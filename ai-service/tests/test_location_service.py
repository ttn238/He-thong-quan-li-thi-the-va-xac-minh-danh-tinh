"""Kiem thu search_location voi provider gia. KHONG goi mang that."""
import unittest

from pydantic import ValidationError

from app.core.errors import GeocodingUnavailableError, InvalidAddressError
from app.schemas.location_schema import LocationSearchRequest, LocationSearchResult
from app.services.geocoding_provider import RawPlace
from app.services.location_service import NOTICE, search_location

FULL = "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh"


def place(name, lat=10.77, lon=106.70, addresstype="building", importance=0.3, **address):
    return RawPlace(display_name=name, lat=lat, lon=lon, addresstype=addresstype, importance=importance,
                    address=address, osm_type="way", osm_id=int(abs(lat * 1000) + abs(lon * 1000)))


class FakeProvider:
    name = "fake"
    attribution = "© test"

    def __init__(self, *answers):
        self.answers = list(answers)  # moi phan tu: list[RawPlace] hoac Exception
        self.queries = []

    def search(self, query, limit):
        self.queries.append((query, limit))
        a = self.answers.pop(0) if len(self.answers) > 1 else self.answers[0]
        if isinstance(a, Exception):
            raise a
        return list(a)


class FoundTests(unittest.TestCase):
    def test_full_address_found(self):
        hit = place("12, Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh, Việt Nam",
                    house_number="12", road="Nguyễn Huệ", suburb="Phường Bến Nghé", city_district="Quận 1",
                    city="Thành phố Hồ Chí Minh", country_code="vn")
        r = search_location("12 Nguyễn Huệ, P. Bến Nghé, Q.1, TPHCM", FakeProvider([hit]))
        self.assertTrue(r.found)
        self.assertIs(r.verified, False)
        self.assertEqual(r.match_scope, "full")
        self.assertEqual(r.normalized_query, FULL)
        self.assertEqual(r.dropped_parts, [])
        self.assertEqual(r.notice, NOTICE)
        self.assertEqual(r.attribution, "© test")
        c = r.candidates[0]
        self.assertEqual(c.match_level, "house")
        self.assertEqual(c.similarity, 1.0)
        self.assertEqual(c.components.ward, "Phường Bến Nghé")
        self.assertEqual(c.components.district, "Quận 1")
        self.assertTrue(c.osm_url.startswith("https://www.openstreetmap.org/way/"))
        self.assertFalse(r.ambiguous)

    def test_verified_cannot_be_true(self):
        r = search_location(FULL, FakeProvider([place("12 Nguyễn Huệ Phường Bến Nghé Quận 1")]))
        self.assertIs(r.model_dump()["verified"], False)
        with self.assertRaises(ValidationError):
            LocationSearchResult(**{**r.model_dump(), "verified": True})

    def test_limit_is_forwarded_and_enforced(self):
        many = [place(f"Nguyễn Huệ {i}", lat=10 + i / 100, lon=106 + i / 100) for i in range(8)]
        p = FakeProvider(many)
        r = search_location("Nguyễn Huệ", p, limit=3)
        self.assertEqual(p.queries[0][1], 3)
        self.assertEqual(len(r.candidates), 3)

    def test_ranked_by_similarity(self):
        a = place("Phố Lê Lợi, Hà Nội", lat=21.0, lon=105.8, addresstype="road", importance=0.9)
        b = place("12, Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh", lat=10.77, lon=106.70)
        r = search_location(FULL, FakeProvider([a, b]))
        self.assertEqual(r.candidates[0].display_name, b.display_name)
        self.assertGreater(r.candidates[0].similarity, r.candidates[1].similarity)

    def test_match_levels(self):
        cases = {"road": "street", "suburb": "ward", "city_district": "district", "city": "city", "state": "province",
                 "country": "country", "weird": "other"}
        for addresstype, level in cases.items():
            r = search_location("Nguyễn Huệ", FakeProvider([place("Nguyễn Huệ", addresstype=addresstype)]))
            self.assertEqual(r.candidates[0].match_level, level, addresstype)


class DuplicatesAndAmbiguityTests(unittest.TestCase):
    def test_duplicates_removed(self):
        a = place("12 Nguyễn Huệ Quận 1", lat=10.77, lon=106.70)
        same_coord = place("Toà nhà 12 Nguyễn Huệ", lat=10.77001, lon=106.70001)
        same_name = place("12 nguyen hue quan 1", lat=11.0, lon=107.0)
        r = search_location("12 Nguyễn Huệ, Quận 1", FakeProvider([a, same_coord, same_name]))
        self.assertEqual(len(r.candidates), 1)

    def test_similar_candidates_flagged_ambiguous(self):
        a = place("Nguyễn Huệ, Quận 1, Thành phố Hồ Chí Minh", lat=10.77, lon=106.70, addresstype="road")
        b = place("Nguyễn Huệ, Thành phố Huế", lat=16.46, lon=107.59, addresstype="road")
        r = search_location("Nguyễn Huệ", FakeProvider([a, b]))
        self.assertTrue(r.ambiguous)
        self.assertTrue(any("nhiều địa điểm" in w for w in r.warnings))

    def test_low_similarity_warning(self):
        r = search_location(FULL, FakeProvider([place("Hà Giang, Việt Nam", addresstype="state")]))
        self.assertTrue(r.found)
        self.assertTrue(any("Độ giống" in w for w in r.warnings))


class MissingAddressTests(unittest.TestCase):
    def test_partial_address_relaxes_and_reports_dropped_parts(self):
        ward = place("Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh", addresstype="suburb")
        p = FakeProvider([], [ward])
        r = search_location(FULL, p)
        self.assertTrue(r.found)
        self.assertEqual(r.match_scope, "partial")
        self.assertEqual(r.dropped_parts, ["12 Nguyễn Huệ"])
        self.assertEqual(r.used_query, "Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh")
        self.assertEqual([q for q, _ in p.queries], [FULL, r.used_query])
        self.assertTrue(any("12 Nguyễn Huệ" in w for w in r.warnings))
        self.assertLess(r.candidates[0].similarity, 1.0)  # khong "khop 100%" khi da bo bot phan

    def test_not_found_is_valid_result_not_error(self):
        p = FakeProvider([])
        r = search_location(FULL, p, max_attempts=3)
        self.assertFalse(r.found)
        self.assertIs(r.verified, False)
        self.assertEqual(r.candidates, [])
        self.assertIsNone(r.match_scope)
        self.assertEqual(len(p.queries), 3)
        self.assertTrue(any("Không tìm thấy" in w for w in r.warnings))

    def test_max_attempts_limits_provider_calls(self):
        p = FakeProvider([])
        search_location(FULL, p, max_attempts=1)
        self.assertEqual(len(p.queries), 1)

    def test_budget_stops_relaxed_attempts(self):
        now = [0.0]

        def clock():
            now[0] += 10
            return now[0]

        p = FakeProvider([])
        r = search_location(FULL, p, budget_s=5, clock=clock)
        self.assertEqual(len(p.queries), 1)
        self.assertFalse(r.found)
        self.assertTrue(any("quá thời gian" in w for w in r.warnings))


class FailureTests(unittest.TestCase):
    def test_provider_error_is_not_reported_as_not_found(self):
        with self.assertRaises(GeocodingUnavailableError):
            search_location(FULL, FakeProvider(GeocodingUnavailableError("mạng lỗi")))

    def test_error_on_relaxed_attempt_also_propagates(self):
        with self.assertRaises(GeocodingUnavailableError):
            search_location(FULL, FakeProvider([], GeocodingUnavailableError("mạng lỗi")))

    def test_empty_text_raises_invalid_address(self):
        for text in ("", "   ", "---", None, "Việt Nam"):
            p = FakeProvider([])
            with self.assertRaises(InvalidAddressError, msg=repr(text)):
                search_location(text, p)
            self.assertEqual(p.queries, [])  # khong goi mang cho chuoi vo nghia


class SchemaTests(unittest.TestCase):
    def test_request_validation(self):
        self.assertEqual(LocationSearchRequest(text="abc").limit, 5)
        for bad in ({"text": ""}, {"text": "x" * 301}, {"text": "a", "limit": 0}, {"text": "a", "limit": 11}):
            with self.assertRaises(ValidationError, msg=str(bad)):
                LocationSearchRequest(**bad)


if __name__ == "__main__":
    unittest.main()
