# Task 5.13 AI Service

Run deterministic unit and HTTP integration tests from the repository root:

```powershell
$env:PYTHONDONTWRITEBYTECODE='1'
& ./ai-service/.venv/Scripts/python.exe -m pytest -c ai-service/pytest.ini ai-service/tests -q -p no:cacheprovider
```

Run real image-to-JSON OCR and YOLO tests separately:

```powershell
$env:AI_RUN_REAL_MODELS='1'
& ./ai-service/.venv/Scripts/python.exe -m pytest -c tests/ai/pytest.ini tests/ai -v -p no:cacheprovider --junitxml=$env:TEMP/qltt-ai-real.xml
```

Requirements are in `ai-service/requirements.txt`. Model weights must be installed. The real suite uses RapidOCR and bundled YOLO weights. Images are generated in memory, with a known answer, using Arial on Windows. The positive YOLO sample is `ultralytics/assets/bus.jpg` from the installed package. No personal identity documents are used. The CCCD fixture has deliberately fictional ASCII text and measures a smoke case, not accuracy on Vietnamese ID photographs.

To export six reusable PNG samples and `answers.json` outside the repository:

```powershell
& ./ai-service/.venv/Scripts/python.exe tests/ai/generate_samples.py "$env:TEMP/qltt-ai-samples"
```

Fixtures cover clear, mildly blurred, two separate plates, two-line plates, blank images and a synthetic identity document. Every positive assertion records the expected fields. Latency and request IDs are stored in JUnit properties. The real-model suite allows 120 seconds per request for CPU diagnostics, including cold loading. Set `AI_TEST_MAX_SECONDS` for a stricter acceptance budget. Passing this budget does not establish compliance with the service's default 20-second timeout. No aggregate accuracy percentage or production latency percentile is claimed from this small sample.

`ai-service/tests/test_api_resilience.py` covers all five upload routes, malformed/oversized/missing input, location search HTTP contracts, provider outage, missing model, timeout and recovery. Existing tests cover result fetch, image download, review, address extraction, ranking, cache and provider failures. External maps are simulated to keep runs repeatable and avoid sending document addresses to third parties. Live map accuracy and VietOCR/PaddleOCR model accuracy require separate labeled datasets and an explicitly configured provider.

`test_face_matching.py` is an existing placeholder. Face matching routes are not implemented and are outside the four modules in task 5.13.

## Verified run on 19 November 2026

Python 3.12 on Windows CPU, project based on merge `1b33f43`.

- Deterministic suite: 131 passed, including 100 existing cases, in 2.18 seconds on the final run.
- Real-model suite: 10 passed in 69.15 seconds. Each HTTP request was measured once. Figures include cold loading where applicable and are not p95/p99 measurements.
- Plate clear 14.261 s, blurred 8.788 s, two plates 10.950 s, two lines 5.225 s. All expected plate values were present.
- Synthetic CCCD 5.606 s. Type, number and name matched the answer key.
- Blank document 4.130 s, blank plate 3.219 s, blank YOLO 14.316 s. All returned no positive identification/detection as expected.
- Bus/person sample 0.093 s after detector loading. Combined upload/read/image roundtrip analysis request 0.602 s.
- A Starlette/httpx deprecation warning remains. This test task makes no production dependency changes.

These results do not replace the earlier observed 504 for a different two-plate image. Image layout, model warm-up and CPU load differ. Keep timeout regression checks and expand the labeled dataset before making general accuracy claims.

An additional real-model test passed separately on 19 November 2026 using the official blank 2024 identity card illustration in `samples/`. The source and manually reviewed answer key are documented there. The extractor correctly returned no identity number or full name and marked the document invalid. This adds one tested negative image case to the ten real-model cases above. A populated Vietnamese card image remains a dataset gap.
