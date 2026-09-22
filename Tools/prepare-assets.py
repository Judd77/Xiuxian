"""Reproducible edits to CC0 sources; no image generation or external services."""
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

from PIL import Image, ImageEnhance, ImageOps

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/_WenXinJie/Art/Backgrounds'
AUDIO = ROOT / 'Assets/_WenXinJie/Audio'
ART.mkdir(parents=True, exist_ok=True)
(AUDIO / 'BGM').mkdir(parents=True, exist_ok=True)
(AUDIO / 'Ambience').mkdir(parents=True, exist_ok=True)

source = Image.open(ROOT / 'AssetSources/Art/kuncan-spring-landscape-1966.367.jpg').convert('RGB')
# Retain the mountain composition, remove the mounting edge, and cool the aged paper.
source = source.crop((140, 30, 3290, 1570))
source = ImageOps.fit(source, (1920, 1080), Image.Resampling.LANCZOS, centering=(0.78, 0.5))
source = ImageEnhance.Color(source).enhance(0.28)
source = ImageEnhance.Contrast(source).enhance(1.12)
red, green, blue = source.split()
source = Image.merge('RGB', (red.point(lambda v: v * 0.88), green.point(lambda v: v * 0.99), blue.point(lambda v: min(255, v * 1.04))))
background = ART / 'Landscape_Cool_1080.jpg'
source.save(background, quality=94, subsampling=0)

music = AUDIO / 'BGM/AsianOriental2_Tozan.ogg'
shutil.copyfile(ROOT / 'AssetSources/Audio/asianoriental2.ogg', music)
rain = AUDIO / 'Ambience/Rain_Window_Ylmir.ogg'
with zipfile.ZipFile(ROOT / 'AssetSources/Audio/rain-ylmir.zip') as archive:
    rain.write_bytes(archive.read('1.ogg'))

manifest = []
for asset in (background, music, rain):
    manifest.append({'path': asset.relative_to(ROOT).as_posix(), 'bytes': asset.stat().st_size,
                     'sha256': hashlib.sha256(asset.read_bytes()).hexdigest()})
(ROOT / 'Docs/AssetResearch/imported-assets.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf8')
print(json.dumps(manifest, indent=2))
