"""Reproduce facial-expression composites without regenerating art or changing story data."""

import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT / 'Docs/AssetResearch/character-expressions-api.json'


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    record = json.loads(RECORD.read_text(encoding='utf-8'))
    for item in record['assets']:
        original = Image.open(ROOT / item['reference']).convert('RGBA')
        generated = Image.open(ROOT / item['raw_path']).convert('RGBA')
        if generated.size != original.size:
            raise ValueError(f"Mismatched expression canvas: {item['name']} {generated.size}")

        region = Image.new('L', original.size, 0)
        ImageDraw.Draw(region).ellipse(item['face_region'], fill=255)
        region = region.filter(ImageFilter.GaussianBlur(7))
        region.save(ROOT / item['blend_path'])
        mask = Image.new('RGBA', original.size, (255, 255, 255, 255))
        mask.putalpha(Image.eval(region, lambda alpha: 255 - alpha))
        mask.save(ROOT / item['mask_path'])

        final = Image.composite(generated.convert('RGB'), original.convert('RGB'), region).convert('RGBA')
        final.putalpha(original.getchannel('A'))
        source = ROOT / item['source_path']
        runtime = ROOT / item['runtime_path']
        runtime.parent.mkdir(parents=True, exist_ok=True)
        final.save(source)
        shutil.copyfile(source, runtime)
        item['returned_size'] = list(generated.size)
        for key in ('reference', 'raw_path', 'mask_path', 'blend_path', 'source_path', 'runtime_path'):
            item[key + '_sha256'] = sha256(ROOT / item[key])
        print(f"Prepared {item['portrait_id']}: {runtime.relative_to(ROOT)}")

    RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
