"""Tra cuu dia diem tu chuoi van ban (chuoi OCR hoac dia chi nhap tay) -> danh sach GOI Y.

Luong: chuan hoa -> thu dia chi day du -> neu khong co ket qua thi bo dan phan cu the nhat (so nha, duong...)
-> quy ve schema, loai trung, xep hang. Ket qua KHONG bao gio duoc coi la da xac minh (verified=False).

Khong goi mang truc tiep: moi truy van di qua `GeocodingProvider` nen thay the/gia lap duoc khi test.
Log chi ghi so luong, KHONG ghi chuoi dia chi (cung nguyen tac voi OCR bien so).
"""
import logging
import time
from typing import Callable, Optional

from app.core.errors import InvalidAddressError
from app.schemas.location_schema import (
    AddressComponents,
    LocationCandidate,
    LocationSearchResult,
    MatchLevel,
)
from app.services.address_normalizer import fold, normalize_address
from app.services.geocoding_provider import GeocodingProvider, RawPlace

logger = logging.getLogger(__name__)

NOTICE = "Chỉ là gợi ý từ dữ liệu bản đồ mở, chưa được xác minh. Cán bộ cần đối chiếu thực tế trước khi sử dụng."

AMBIGUOUS_GAP = 0.1  # hai goi y dau chenh nhau it hon nguong nay => coi la nhieu phuong an ngang nhau
LOW_SIMILARITY = 0.5

# Tu chi cap hanh chinh/loai duong: bo khi tinh do giong de so, ten rieng quyet dinh ket qua
_STOP_TOKENS = {
    "phuong", "quan", "huyen", "xa", "thi", "tran", "thanh", "pho", "tinh", "duong", "khu",
    "viet", "nam", "so", "hem", "ngo", "ngach", "quoc", "lo",
}

_ADDRESSTYPE_LEVEL: dict[str, MatchLevel] = {
    "house": "house", "building": "house", "house_number": "house",
    "road": "street", "street": "street",
    "quarter": "ward", "suburb": "ward", "neighbourhood": "ward", "hamlet": "ward", "village": "ward",
    "city_block": "ward", "commune": "ward",
    "city_district": "district", "district": "district", "county": "district", "borough": "district",
    "municipality": "district", "town": "district",
    "city": "city",
    "state": "province", "province": "province", "region": "province",
    "country": "country",
}


def _first(address: dict, *keys: str) -> Optional[str]:
    for key in keys:
        value = address.get(key)
        if value:
            return value
    return None


def _match_level(place: RawPlace) -> MatchLevel:
    if place.addresstype in _ADDRESSTYPE_LEVEL:
        return _ADDRESSTYPE_LEVEL[place.addresstype]
    if place.category == "highway":
        return "street"
    if place.category == "building" or place.type in ("house", "residential"):
        return "house"
    return "other"


def _tokens(text: str) -> set[str]:
    return set(fold(text).split())


def _similarity(query_tokens: set[str], place: RawPlace) -> float:
    significant = query_tokens - _STOP_TOKENS or query_tokens
    if not significant:
        return 0.0
    return round(len(significant & _tokens(place.display_name)) / len(significant), 3)


def _candidate(place: RawPlace, query_tokens: set[str], source: str) -> LocationCandidate:
    addr = place.address
    osm_url = (
        f"https://www.openstreetmap.org/{place.osm_type}/{place.osm_id}"
        if place.osm_type in ("node", "way", "relation") and place.osm_id is not None
        else None
    )
    return LocationCandidate(
        display_name=place.display_name,
        lat=place.lat,
        lon=place.lon,
        match_level=_match_level(place),
        similarity=_similarity(query_tokens, place),
        importance=place.importance,
        components=AddressComponents(
            house_number=addr.get("house_number"),
            road=addr.get("road"),
            ward=_first(addr, "quarter", "suburb", "neighbourhood", "hamlet", "village"),
            district=_first(addr, "city_district", "district", "county", "borough", "municipality"),
            city=_first(addr, "city", "town"),
            province=_first(addr, "state", "province", "region"),
            postcode=addr.get("postcode"),
            country_code=addr.get("country_code"),
        ),
        source=source,
        osm_url=osm_url,
    )


def _dedupe(places: list[RawPlace]) -> list[RawPlace]:
    """Loai ket qua trung: cung toa do (lam tron ~10 m) hoac cung ten day du."""
    seen_coords, seen_names, out = set(), set(), []
    for p in places:
        coord, name = (round(p.lat, 4), round(p.lon, 4)), fold(p.display_name)
        if coord in seen_coords or name in seen_names:
            continue
        seen_coords.add(coord)
        seen_names.add(name)
        out.append(p)
    return out


def search_location(
    text: str,
    provider: GeocodingProvider,
    *,
    limit: int = 5,
    max_attempts: int = 3,
    budget_s: float = 12.0,
    clock: Callable[[], float] = time.monotonic,
) -> LocationSearchResult:
    """Tra cuu dia diem. Nem InvalidAddressError neu chuoi rong sau chuan hoa; GeocodingUnavailableError
    neu nguon tra cuu loi (khi do KHONG the ket luan "khong tim thay")."""
    address = normalize_address(text)
    if address.is_empty:
        raise InvalidAddressError("Chuỗi địa điểm rỗng hoặc không chứa nội dung hợp lệ")

    queries = address.fallback_queries(max_attempts)
    query_tokens = _tokens(address.text)
    warnings = list(address.notes)
    started = clock()

    raw: list[RawPlace] = []
    used = None
    attempts = 0
    for index, fallback in enumerate(queries):
        if index > 0 and clock() - started > budget_s:
            warnings.append("Đã dừng thử các phiên bản rút gọn của địa chỉ vì quá thời gian cho phép.")
            break
        attempts += 1
        # Loi nguon (GeocodingUnavailableError) de lan ra ngoai: khong duoc bao "khong tim thay" khi chua tra cuu duoc
        raw = provider.search(fallback.query, limit)
        if raw:
            used = fallback
            break

    base = dict(
        query=address.raw,
        normalized_query=address.text,
        attribution=provider.attribution,
        notice=NOTICE,
    )

    if not raw or used is None:
        warnings.append(
            "Không tìm thấy địa điểm khớp. Hãy kiểm tra chính tả và bổ sung tên đường, phường/xã, tỉnh/thành phố."
        )
        logger.info("location_search found=false attempts=%d", attempts)
        return LocationSearchResult(found=False, warnings=warnings, **base)

    candidates = [_candidate(p, query_tokens, provider.name) for p in _dedupe(raw)]
    candidates.sort(key=lambda c: (-c.similarity, -(c.importance or 0.0)))
    candidates = candidates[:limit]

    partial = bool(used.dropped)
    if partial:
        warnings.append(
            "Không tìm thấy địa chỉ đầy đủ; kết quả chỉ khớp phần còn lại sau khi bỏ: " + ", ".join(used.dropped) + "."
        )
    ambiguous = len(candidates) > 1 and candidates[0].similarity - candidates[1].similarity < AMBIGUOUS_GAP
    if ambiguous:
        warnings.append("Có nhiều địa điểm tương tự nhau, cần chọn và đối chiếu thủ công.")
    if candidates[0].similarity < LOW_SIMILARITY:
        warnings.append("Độ giống giữa chuỗi nhập và kết quả thấp, cần kiểm tra kỹ trước khi dùng.")

    logger.info("location_search found=true candidates=%d attempts=%d partial=%s", len(candidates), attempts, partial)
    return LocationSearchResult(
        found=True,
        match_scope="partial" if partial else "full",
        used_query=used.query,
        dropped_parts=list(used.dropped),
        ambiguous=ambiguous,
        candidates=candidates,
        warnings=warnings,
        **base,
    )
