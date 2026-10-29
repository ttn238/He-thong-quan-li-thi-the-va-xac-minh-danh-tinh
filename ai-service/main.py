"""Diem khoi chay AI Service (FastAPI).

Chay:  uvicorn main:app --host 0.0.0.0 --port 8000
"""
import asyncio
import logging
import time
import uuid
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

from app.api import image_routes, ocr_routes
from app.api.deps import get_detector, get_ocr_engine
from app.core.config import get_settings
from app.core.errors import AppError
from app.core.logging_setup import setup_logging
from app.schemas.prediction_schema import ApiResponse, ErrorBody

logger = logging.getLogger("ai-service")


def _error_response(request: Request, status: int, code: str, message: str) -> JSONResponse:
    started = getattr(request.state, "started", time.perf_counter())
    body = ApiResponse(
        success=False,
        request_id=getattr(request.state, "request_id", "-"),
        processing_ms=int((time.perf_counter() - started) * 1000),
        error=ErrorBody(code=code, message=message),
    )
    return JSONResponse(status_code=status, content=body.model_dump())


@asynccontextmanager
async def lifespan(app: FastAPI):
    settings = get_settings()
    setup_logging(settings.log_level)
    app.state.settings = settings
    app.state.semaphore = asyncio.Semaphore(settings.max_concurrent_jobs)
    if settings.preload_models:
        try:
            await asyncio.to_thread(get_ocr_engine().load)
            await asyncio.to_thread(get_detector().load)
            logger.info("models preloaded")
        except Exception:
            # Khong chan khoi dong: endpoint se bao loi ro rang neu thieu mo hinh
            logger.exception("model preload failed")
    yield


def create_app() -> FastAPI:
    settings = get_settings()
    app = FastAPI(
        title="AI Service - Nhan dang va doi chieu ho so",
        version=settings.app_version,
        lifespan=lifespan,
    )

    @app.middleware("http")
    async def request_context(request: Request, call_next):
        request.state.request_id = uuid.uuid4().hex[:16]
        request.state.started = time.perf_counter()
        response = await call_next(request)
        response.headers["X-Request-ID"] = request.state.request_id
        # Chi log phuong thuc, duong dan co dinh, ma tra ve, thoi gian
        logger.info(
            "%s %s -> %d (%d ms) rid=%s",
            request.method,
            request.url.path,
            response.status_code,
            (time.perf_counter() - request.state.started) * 1000,
            request.state.request_id,
        )
        return response

    @app.exception_handler(AppError)
    async def app_error_handler(request: Request, exc: AppError):
        return _error_response(request, exc.status_code, exc.code, exc.message)

    @app.exception_handler(RequestValidationError)
    async def validation_handler(request: Request, exc: RequestValidationError):
        return _error_response(request, 422, "VALIDATION_ERROR", "Du lieu yeu cau khong hop le")

    @app.exception_handler(StarletteHTTPException)
    async def http_handler(request: Request, exc: StarletteHTTPException):
        return _error_response(request, exc.status_code, "HTTP_ERROR", str(exc.detail))

    @app.exception_handler(Exception)
    async def unhandled_handler(request: Request, exc: Exception):
        # Khong tra chi tiet noi bo; stack trace chi ghi vao log (khong co du lieu anh)
        logger.exception("unhandled error rid=%s", getattr(request.state, "request_id", "-"))
        return _error_response(request, 500, "INTERNAL_ERROR", "Loi he thong")

    @app.get("/health", tags=["System"])
    async def health():
        return {"status": "ok", "service": settings.app_name, "version": settings.app_version}

    app.include_router(ocr_routes.router)
    app.include_router(image_routes.router)
    return app

app = create_app()


if __name__ == "__main__":
    import uvicorn


    uvicorn.run("main:app", host="127.0.0.1", port=8000)
