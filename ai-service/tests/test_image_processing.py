"""Input validation independent of model downloads and external services."""
from io import BytesIO

import pytest
from PIL import Image

from app.core.config import Settings
from app.core.errors import (
    ImageNotFoundError, ImagePathForbiddenError, ImageTooLargeError,
    InvalidImageError, MissingImageError, UnsupportedMediaError,
)
from app.utils.image_processing import decode_image, read_bytes_from_path


def image_bytes(size=(80, 60), fmt='PNG'):
    stream = BytesIO()
    Image.new('RGB', size, 'white').save(stream, format=fmt)
    return stream.getvalue()


@pytest.mark.parametrize('fmt', ['PNG', 'JPEG', 'BMP', 'WEBP'])
def test_supported_formats(fmt):
    image = decode_image(image_bytes(fmt=fmt), Settings())
    assert (image.original_width, image.original_height) == (80, 60)


@pytest.mark.parametrize(('data', 'error'), [
    (None, MissingImageError), (b'', MissingImageError),
    (b'not an image', InvalidImageError),
])
def test_missing_and_invalid(data, error):
    with pytest.raises(error):
        decode_image(data, Settings())


def test_format_size_and_pixel_limits():
    cases = [
        (image_bytes(fmt='PNG'), Settings(allowed_formats=('JPEG',)), UnsupportedMediaError),
        (image_bytes((10, 10)), Settings(), InvalidImageError),
        (image_bytes(), Settings(max_image_pixels=100), ImageTooLargeError),
        (image_bytes(), Settings(max_image_mb=0.00001), ImageTooLargeError),
    ]
    for data, settings, error in cases:
        with pytest.raises(error):
            decode_image(data, settings)


def test_resize_preserves_original_coordinates():
    image = decode_image(image_bytes((800, 400)), Settings(process_max_side=400))
    assert image.bgr.shape[:2] == (200, 400)
    assert image.to_original_box(10, 20, 400, 200) == (20, 40, 800, 400)


def test_path_access_is_confined(tmp_path):
    settings = Settings(image_base_dir=tmp_path)
    data = image_bytes()
    (tmp_path / 'valid.png').write_bytes(data)
    assert read_bytes_from_path('valid.png', settings) == data
    with pytest.raises(ImagePathForbiddenError):
        read_bytes_from_path('../outside.png', settings)
    with pytest.raises(ImageNotFoundError):
        read_bytes_from_path('missing.png', settings)
    with pytest.raises(ImagePathForbiddenError):
        read_bytes_from_path('valid.png', Settings(image_base_dir=None))
