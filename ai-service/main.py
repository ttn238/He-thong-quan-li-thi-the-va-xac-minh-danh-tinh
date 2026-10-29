from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.ocr_routes import router as ocr_router
from app.services.ocr_service import _engine_instance


@asynccontextmanager
async def lifespan(_: FastAPI):
    _engine_instance()      # nạp mô hình OCR sẵn để yêu cầu đầu tiên không bị chậm
    yield


app = FastAPI(title="DVI AI Service", lifespan=lifespan)

# Chỉ cần khi gọi thẳng cổng 8000 từ trình duyệt; qua proxy của Vite thì không cần CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_methods=["GET", "POST"],
    allow_headers=["*"],
)

app.include_router(ocr_router)


@app.get("/health")
def health():
    return {"status": "ok"}