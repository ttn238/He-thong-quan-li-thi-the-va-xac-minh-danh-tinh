"""Nguon tra cuu dia diem (geocoding). Mac dinh: Nominatim cua OpenStreetMap (mien phi).

Tuan thu chinh sach Nominatim cong cong (operations.osmfoundation.org/policies/nominatim, doc ngay phat trien):
  - toi da 1 yeu cau/giay (gioi han ap dung cho CA ung dung, khong theo tung nguoi dung) -> `_throttle`
  - bat buoc User-Agent dinh danh ung dung (khong dung mac dinh cua thu vien HTTP)      -> `user_agent`
  - bat buoc cache ket qua, khong gui lap lai cung truy van                              -> `_cache_*`
  - khong autocomplete, khong bulk: chi tra cuu khi nguoi dung chu dong gui yeu cau
  - phai co the tat/doi dich vu bat cu luc nao khong can sua ma                          -> `base_url` + AI_GEOCODER_ENABLED
  - phai ghi nguon OpenStreetMap (ODbL)                                                  -> `attribution`
  - khong gui du lieu ca nhan/tai lieu mat: XEM tai lieu API, chi gui chuoi dia diem.
Chinh sach co the thay doi khong bao truoc; doc lai truoc khi trien khai thuc te.

Chi dung thu vien chuan (urllib) nen khong them dependency; ham `fetch` co the thay the khi test.
"""
import http.client
import json
import logging
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
from collections import OrderedDict
from dataclasses import dataclass, field
from typing import Callable, Optional, Protocol

from app.core.errors import GeocodingUnavailableError
from app.services.address_normalizer import fold

logger = logging.getLogger(__name__)

_MAX_BODY_BYTES = 2 * 1024 * 1024
FetchFn = Callable[[str, dict, float], tuple[int, bytes]]


@dataclass
class RawPlace:
    """Mot dia diem tho tu nguon tra cuu (chua quy ve schema API)."""

    display_name: str
    lat: float
    lon: float
    category: str = ""
    type: str = ""
    addresstype: str = ""
    importance: Optional[float] = None
    address: dict = field(default_factory=dict)
    osm_type: str = ""
    osm_id: Optional[int] = None


class GeocodingProvider(Protocol):
    name: str
    attribution: str

    def search(self, query: str, limit: int) -> list[RawPlace]: ...


def urllib_fetch(url: str, headers: dict, timeout: float) -> tuple[int, bytes]:
    """GET bang urllib. Tra (status, body); loi mang/timeout de lan ra ngoai cho provider xu ly."""
    request = urllib.request.Request(url, headers=headers, method="GET")
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:  # noqa: S310 - scheme da kiem tra
            body = response.read(_MAX_BODY_BYTES + 1)
            if len(body) > _MAX_BODY_BYTES:
                raise GeocodingUnavailableError("Phản hồi từ nguồn tra cứu quá lớn")
            return response.status, body
    except urllib.error.HTTPError as exc:
        return exc.code, b""


class NominatimProvider:
    name = "nominatim"
    attribution = "Dữ liệu © OpenStreetMap contributors (ODbL), tra cứu qua Nominatim"

    def __init__(
        self,
        base_url: str = "https://nominatim.openstreetmap.org",
        user_agent: str = "DeathManagement-AI-Service/1.0",
        timeout_s: float = 5.0,
        min_interval_s: float = 1.0,
        country_codes: str = "vn",
        language: str = "vi",
        cache_ttl_s: float = 86400.0,
        negative_cache_ttl_s: float = 300.0,
        cache_max_entries: int = 1000,
        cooldown_s: float = 60.0,
        fetch: Optional[FetchFn] = None,
        clock: Callable[[], float] = time.monotonic,
        sleep: Callable[[float], None] = time.sleep,
    ) -> None:
        parsed = urllib.parse.urlparse(base_url)
        if parsed.scheme not in ("http", "https") or not parsed.netloc:
            # Chan file://, ftp://... (urlopen ho tro nhieu scheme) - cau hinh sai khong duoc doc file cuc bo
            raise ValueError("geocoder_base_url phai la http(s)://host")
        if not user_agent.strip():
            raise ValueError("User-Agent khong duoc de trong (chinh sach Nominatim)")
        self._endpoint = base_url.rstrip("/") + "/search"
        self._headers = {"User-Agent": user_agent.strip(), "Accept": "application/json"}
        self._timeout = timeout_s
        self._min_interval = max(0.0, min_interval_s)
        self._country_codes = country_codes
        self._language = language
        self._cache_ttl = cache_ttl_s
        self._negative_ttl = negative_cache_ttl_s
        self._cache_max = max(1, cache_max_entries)
        self._cooldown = max(0.0, cooldown_s)
        self._fetch = fetch or urllib_fetch
        self._clock = clock
        self._sleep = sleep

        self._cache: "OrderedDict[tuple, tuple[float, tuple[RawPlace, ...]]]" = OrderedDict()
        self._cache_lock = threading.Lock()
        self._rate_lock = threading.Lock()
        self._last_request = float("-inf")
        self._blocked_until = float("-inf")

    # ------------------------------------------------------------------ API
    def search(self, query: str, limit: int) -> list[RawPlace]:
        key = (fold(query), limit)
        cached = self._cache_get(key)
        if cached is not None:
            return list(cached)

        self._check_cooldown()
        self._throttle()
        url = self._build_url(query, limit)
        try:
            status, body = self._fetch(url, self._headers, self._timeout)
        except GeocodingUnavailableError:
            raise
        except (OSError, http.client.HTTPException) as exc:  # URLError, timeout, ket noi bi ngat...
            self._enter_cooldown()
            # Khong dua noi dung loi/URL (co chuoi dia chi) vao thong bao tra ve nguoi dung
            raise GeocodingUnavailableError("Không kết nối được tới nguồn tra cứu địa điểm") from exc

        if status == 429 or status == 403 or status >= 500:
            self._enter_cooldown()
            raise GeocodingUnavailableError(f"Nguồn tra cứu địa điểm từ chối hoặc đang lỗi (HTTP {status})")
        if status != 200:
            raise GeocodingUnavailableError(f"Nguồn tra cứu địa điểm trả về lỗi (HTTP {status})")

        places = _parse_places(body)
        self._cache_put(key, places)
        return list(places)

    # ------------------------------------------------------------------ noi bo
    def _build_url(self, query: str, limit: int) -> str:
        params = {
            "q": query,
            "format": "jsonv2",
            "addressdetails": 1,
            "limit": limit,
            "dedupe": 1,
            "accept-language": self._language,
        }
        if self._country_codes:
            params["countrycodes"] = self._country_codes
        return f"{self._endpoint}?{urllib.parse.urlencode(params)}"

    def _throttle(self) -> None:
        """Giu khoang cach toi thieu giua 2 yeu cau ra ngoai (gioi han tinh tren toan ung dung)."""
        with self._rate_lock:
            wait = self._last_request + self._min_interval - self._clock()
            if wait > 0:
                self._sleep(wait)
            self._last_request = self._clock()

    def _enter_cooldown(self) -> None:
        self._blocked_until = self._clock() + self._cooldown

    def _check_cooldown(self) -> None:
        if self._clock() < self._blocked_until:
            raise GeocodingUnavailableError("Nguồn tra cứu địa điểm tạm thời không khả dụng, vui lòng thử lại sau")

    def _cache_get(self, key: tuple) -> Optional[tuple[RawPlace, ...]]:
        with self._cache_lock:
            entry = self._cache.get(key)
            if entry is None:
                return None
            expires_at, places = entry
            if self._clock() >= expires_at:
                del self._cache[key]
                return None
            self._cache.move_to_end(key)
            return places

    def _cache_put(self, key: tuple, places: list[RawPlace]) -> None:
        ttl = self._cache_ttl if places else self._negative_ttl
        if ttl <= 0:
            return
        with self._cache_lock:
            self._cache[key] = (self._clock() + ttl, tuple(places))
            self._cache.move_to_end(key)
            while len(self._cache) > self._cache_max:
                self._cache.popitem(last=False)


def _parse_places(body: bytes) -> list[RawPlace]:
    try:
        data = json.loads(body)
    except (ValueError, UnicodeDecodeError) as exc:
        raise GeocodingUnavailableError("Phản hồi từ nguồn tra cứu không hợp lệ") from exc
    if not isinstance(data, list):
        raise GeocodingUnavailableError("Phản hồi từ nguồn tra cứu không hợp lệ")

    places: list[RawPlace] = []
    for item in data:
        if not isinstance(item, dict):
            continue
        try:
            lat, lon = float(item["lat"]), float(item["lon"])
        except (KeyError, TypeError, ValueError):
            continue
        name = item.get("display_name")
        if not isinstance(name, str) or not name.strip() or not (-90 <= lat <= 90 and -180 <= lon <= 180):
            continue
        importance = item.get("importance")
        osm_id = item.get("osm_id")
        address = item.get("address")
        places.append(
            RawPlace(
                display_name=name.strip(),
                lat=lat,
                lon=lon,
                category=str(item.get("category") or item.get("class") or ""),
                type=str(item.get("type") or ""),
                addresstype=str(item.get("addresstype") or ""),
                importance=float(importance) if isinstance(importance, (int, float)) else None,
                address={str(k): str(v) for k, v in address.items()} if isinstance(address, dict) else {},
                osm_type=str(item.get("osm_type") or ""),
                osm_id=osm_id if isinstance(osm_id, int) else None,
            )
        )
    return places
