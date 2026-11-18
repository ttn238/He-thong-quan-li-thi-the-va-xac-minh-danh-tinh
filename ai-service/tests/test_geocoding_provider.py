"""Kiem thu NominatimProvider voi `fetch`, dong ho va sleep gia: KHONG goi mang that.

Luu y: cac test nay chung minh logic (tham so, gioi han toc do, cache, cooldown, xu ly loi),
khong chung minh Nominatim that tra dung dia chi.
"""
import json
import unittest
from urllib.parse import parse_qs, urlparse

from app.core.errors import GeocodingUnavailableError
from app.services.geocoding_provider import NominatimProvider

ITEM = {
    "place_id": 1, "osm_type": "way", "osm_id": 123, "lat": "10.7769", "lon": "106.7009",
    "category": "building", "type": "yes", "addresstype": "building", "importance": 0.4,
    "display_name": "12, Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh, Việt Nam",
    "address": {"house_number": "12", "road": "Nguyễn Huệ", "country_code": "vn"},
}


class FakeTime:
    def __init__(self):
        self.now = 1000.0
        self.sleeps = []

    def clock(self):
        return self.now

    def sleep(self, seconds):
        self.sleeps.append(seconds)
        self.now += seconds


class FakeFetch:
    def __init__(self, *responses):
        self.responses = list(responses)
        self.calls = []

    def __call__(self, url, headers, timeout):
        self.calls.append((url, headers, timeout))
        r = self.responses.pop(0) if len(self.responses) > 1 else self.responses[0]
        if isinstance(r, Exception):
            raise r
        return r


def ok(*items):
    return 200, json.dumps(list(items)).encode()


def make(fetch, **kw):
    t = FakeTime()
    p = NominatimProvider(fetch=fetch, clock=t.clock, sleep=t.sleep, **kw)
    return p, t


class RequestTests(unittest.TestCase):
    def test_url_params_and_user_agent(self):
        fetch = FakeFetch(ok(ITEM))
        p, _ = make(fetch, user_agent="Demo/1.0 (a@b.c)", timeout_s=3)
        places = p.search("12 Nguyễn Huệ, Quận 1", 4)
        url, headers, timeout = fetch.calls[0]
        qs = parse_qs(urlparse(url).query)
        self.assertEqual(urlparse(url).path, "/search")
        self.assertEqual(qs["q"], ["12 Nguyễn Huệ, Quận 1"])
        self.assertEqual(qs["format"], ["jsonv2"])
        self.assertEqual(qs["countrycodes"], ["vn"])
        self.assertEqual(qs["limit"], ["4"])
        self.assertEqual(qs["addressdetails"], ["1"])
        self.assertEqual(headers["User-Agent"], "Demo/1.0 (a@b.c)")
        self.assertEqual(timeout, 3)
        self.assertEqual(places[0].lat, 10.7769)
        self.assertEqual(places[0].osm_id, 123)

    def test_rejects_non_http_scheme_and_blank_user_agent(self):
        with self.assertRaises(ValueError):
            NominatimProvider(base_url="file:///etc/passwd")
        with self.assertRaises(ValueError):
            NominatimProvider(base_url="ftp://example.com")
        with self.assertRaises(ValueError):
            NominatimProvider(user_agent="   ")


class RateLimitAndCacheTests(unittest.TestCase):
    def test_min_interval_between_outgoing_requests(self):
        fetch = FakeFetch(ok(ITEM))
        p, t = make(fetch, min_interval_s=1.0)
        p.search("a", 5)
        p.search("b", 5)
        p.search("c", 5)
        self.assertEqual(len(fetch.calls), 3)
        self.assertEqual(len(t.sleeps), 2)
        self.assertTrue(all(abs(s - 1.0) < 1e-9 for s in t.sleeps))

    def test_cache_hit_ignores_case_and_diacritics_and_skips_network(self):
        fetch = FakeFetch(ok(ITEM))
        p, t = make(fetch)
        p.search("Nguyễn Huệ, Quận 1", 5)
        again = p.search("nguyen hue quan 1", 5)
        self.assertEqual(len(fetch.calls), 1)
        self.assertEqual(len(again), 1)
        self.assertEqual(t.sleeps, [])

    def test_limit_is_part_of_cache_key(self):
        fetch = FakeFetch(ok(ITEM))
        p, _ = make(fetch)
        p.search("x", 5)
        p.search("x", 3)
        self.assertEqual(len(fetch.calls), 2)

    def test_cache_expires(self):
        fetch = FakeFetch(ok(ITEM))
        p, t = make(fetch, cache_ttl_s=10, min_interval_s=0)
        p.search("x", 5)
        t.now += 11
        p.search("x", 5)
        self.assertEqual(len(fetch.calls), 2)

    def test_negative_result_cached_with_its_own_ttl(self):
        fetch = FakeFetch(ok())
        p, t = make(fetch, negative_cache_ttl_s=100, min_interval_s=0)
        self.assertEqual(p.search("zzz", 5), [])
        self.assertEqual(p.search("zzz", 5), [])
        self.assertEqual(len(fetch.calls), 1)
        t.now += 101
        p.search("zzz", 5)
        self.assertEqual(len(fetch.calls), 2)

    def test_cache_disabled_when_ttl_zero(self):
        fetch = FakeFetch(ok(ITEM))
        p, _ = make(fetch, cache_ttl_s=0, min_interval_s=0)
        p.search("x", 5)
        p.search("x", 5)
        self.assertEqual(len(fetch.calls), 2)

    def test_lru_eviction(self):
        fetch = FakeFetch(ok(ITEM))
        p, _ = make(fetch, cache_max_entries=2, min_interval_s=0)
        for q in ("a", "b", "c"):
            p.search(q, 5)
        p.search("c", 5)  # con trong cache
        p.search("a", 5)  # da bi day ra
        self.assertEqual(len(fetch.calls), 4)


class ErrorHandlingTests(unittest.TestCase):
    def test_network_error_message_does_not_leak_query(self):
        fetch = FakeFetch(OSError("timed out connecting for q=SECRET ADDRESS"))
        p, _ = make(fetch)
        with self.assertRaises(GeocodingUnavailableError) as cm:
            p.search("SECRET ADDRESS", 5)
        self.assertNotIn("SECRET", cm.exception.message)
        self.assertEqual(cm.exception.code, "GEOCODING_UNAVAILABLE")

    def test_rate_limited_enters_cooldown_then_recovers(self):
        fetch = FakeFetch((429, b""), ok(ITEM))
        p, t = make(fetch, cooldown_s=60, min_interval_s=0)
        with self.assertRaises(GeocodingUnavailableError):
            p.search("a", 5)
        with self.assertRaises(GeocodingUnavailableError):  # trong cooldown: khong goi them
            p.search("b", 5)
        self.assertEqual(len(fetch.calls), 1)
        t.now += 61
        self.assertEqual(len(p.search("b", 5)), 1)
        self.assertEqual(len(fetch.calls), 2)

    def test_cached_result_still_served_during_cooldown(self):
        fetch = FakeFetch(ok(ITEM), (503, b""))
        p, _ = make(fetch, min_interval_s=0)
        p.search("known", 5)
        with self.assertRaises(GeocodingUnavailableError):
            p.search("other", 5)
        self.assertEqual(len(p.search("known", 5)), 1)

    def test_http_errors(self):
        for status in (403, 500, 502, 400, 404):
            p, _ = make(FakeFetch((status, b"")))
            with self.assertRaises(GeocodingUnavailableError, msg=str(status)):
                p.search("a", 5)

    def test_invalid_payloads(self):
        for body in (b"<html>blocked</html>", b'{"error": "x"}', b"\xff\xfe", b""):
            p, _ = make(FakeFetch((200, body)))
            with self.assertRaises(GeocodingUnavailableError, msg=repr(body)):
                p.search("a", 5)

    def test_malformed_items_are_skipped(self):
        bad = [
            "not a dict",
            {"lat": "x", "lon": "1", "display_name": "bad lat"},
            {"lat": "95", "lon": "1", "display_name": "out of range"},
            {"lat": "1", "lon": "1"},
            {"lat": "1", "lon": "1", "display_name": "  "},
        ]
        p, _ = make(FakeFetch(ok(*bad, ITEM)))
        places = p.search("a", 5)
        self.assertEqual([x.osm_id for x in places], [123])

    def test_errors_are_not_cached(self):
        fetch = FakeFetch((500, b""), ok(ITEM))
        p, t = make(fetch, cooldown_s=0, min_interval_s=0)
        with self.assertRaises(GeocodingUnavailableError):
            p.search("a", 5)
        self.assertEqual(len(p.search("a", 5)), 1)


if __name__ == "__main__":
    unittest.main()
