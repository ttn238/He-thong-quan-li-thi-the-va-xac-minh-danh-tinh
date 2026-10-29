"""Loi nghiep vu thong nhat. Moi loi duoc tra ve theo cung mot dinh dang JSON."""


class AppError(Exception):
    status_code = 400
    code = "BAD_REQUEST"

    def __init__(self, message: str):
        super().__init__(message)
        self.message = message


class InvalidImageError(AppError):
    status_code = 400
    code = "INVALID_IMAGE"


class MissingImageError(AppError):
    status_code = 400
    code = "MISSING_IMAGE"


class UnsupportedMediaError(AppError):
    status_code = 415
    code = "UNSUPPORTED_MEDIA_TYPE"


class ImageTooLargeError(AppError):
    status_code = 413
    code = "IMAGE_TOO_LARGE"


class ImagePathForbiddenError(AppError):
    status_code = 403
    code = "IMAGE_PATH_FORBIDDEN"


class ImageNotFoundError(AppError):
    status_code = 404
    code = "IMAGE_NOT_FOUND"


class ProcessingTimeoutError(AppError):
    status_code = 504
    code = "PROCESSING_TIMEOUT"


class ModelUnavailableError(AppError):
    status_code = 503
    code = "MODEL_UNAVAILABLE"
