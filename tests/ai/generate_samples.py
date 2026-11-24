"""Generate the labeled synthetic sample images used by the real-model suite."""
import argparse
import json
from pathlib import Path

from test_real_models import picture


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path, help='Directory for PNG samples and their answer key')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    samples = [
        ('plate-clear', ['29A-123.45'], False, {'plates': ['29A12345']}),
        ('plate-blurred', ['29A-123.45'], True, {'plates': ['29A12345']}),
        ('plate-multiple', ['29A-123.45', '51B-678.90'], False, {'plates': ['29A12345', '51B67890']}),
        ('plate-two-lines', ['29A', '123.45'], False, {'plates': ['29A12345']}),
        ('blank', [], False, {'plates': [], 'objects': [], 'document_valid': False}),
        ('document', ['CAN CUOC CONG DAN', 'So: 001099012345', 'Ho va ten: TEST'], False,
         {'document_type': 'cccd', 'id_number': '001099012345', 'full_name': 'TEST'}),
    ]
    manifest = {}
    for name, lines, blur, expected in samples:
        filename = name + '.png'
        (args.output / filename).write_bytes(picture(lines, blur))
        manifest[filename] = expected
    (args.output / 'answers.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
