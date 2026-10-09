"""Prepare project-ready derivatives from this project's generated xianxia artwork."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/_WenXinJie/Art'
GENERATED = ROOT / 'AssetSources/Art/Generated'


def fit(source, target):
    image = Image.open(source).convert('RGB')
    ratio = max(1920 / image.width, 1080 / image.height)
    size = (round(image.width * ratio), round(image.height * ratio))
    image = image.resize(size, Image.Resampling.LANCZOS)
    left = (image.width - 1920) // 2
    top = (image.height - 1080) // 2
    image.crop((left, top, left + 1920, top + 1080)).save(target, quality=94)


backgrounds = ART / 'Backgrounds'
backgrounds.mkdir(parents=True, exist_ok=True)
fit(GENERATED / 'CloudGate_Premium.png', backgrounds / 'CloudGate_Premium_1080.jpg')
fit(GENERATED / 'PineRidge_Premium.png', backgrounds / 'PineRidge_Premium_1080.jpg')
fit(GENERATED / 'SteleCorridor_Premium.png', backgrounds / 'SteleCorridor_Premium_1080.jpg')
fit(GENERATED / 'DiscipleQuartersNight_Premium.png', backgrounds / 'DiscipleQuartersNight_Premium_1080.jpg')
fit(GENERATED / 'MountainHeartGate_Premium.png', backgrounds / 'MountainHeartGate_Premium_1080.jpg')
fit(GENERATED / 'MedicineHouse_Premium_v1.png', backgrounds / 'MedicineHouse_Premium_1080.jpg')
fit(GENERATED / 'NangouVillage_Premium_v1.png', backgrounds / 'NangouVillage_Premium_1080.jpg')
fit(GENERATED / 'CouncilHall_Premium_v1.png', backgrounds / 'CouncilHall_Premium_1080.jpg')
fit(GENERATED / 'FurnaceChamber_Premium_v1.png', backgrounds / 'FurnaceChamber_Premium_1080.jpg')
fit(GENERATED / 'DiversionValley_Premium_v1.png', backgrounds / 'DiversionValley_Premium_1080.jpg')
fit(GENERATED / 'DiversionValley_Aftermath_v1.png', backgrounds / 'DiversionValley_Aftermath_1080.jpg')
for location in ('CraftsmanHome', 'VillagerHome', 'XuFamilyHome', 'OldBellTower', 'RegistryOffice',
                 'Storehouse', 'TransferDesk', 'ExaminationCourt'):
    fit(GENERATED / f'{location}_Premium_v1.png', backgrounds / f'{location}_Premium_1080.jpg')

valley_night = Image.open(backgrounds / 'DiversionValley_Premium_1080.jpg').convert('RGB')
valley_night = ImageEnhance.Brightness(valley_night).enhance(0.40)
valley_night = ImageEnhance.Color(valley_night).enhance(0.40)
valley_night = Image.blend(valley_night, Image.new('RGB', valley_night.size, '#10263d'), 0.32)
valley_night.save(backgrounds / 'DiversionValley_Premium_Night_1080.jpg', quality=94)

for character in ('GuHeng', 'AChe', 'XuCheng', 'ShenYan'):
    # These API outputs already contain alpha; retain it without keying or cropping.
    shutil.copyfile(GENERATED / f'{character}_Premium_v1.png',
                    ART / 'Generated' / f'{character}_Premium_Cutout.png')

village_night = Image.open(backgrounds / 'NangouVillage_Premium_1080.jpg').convert('RGB')
village_night = ImageEnhance.Brightness(village_night).enhance(0.40)
village_night = ImageEnhance.Color(village_night).enhance(0.40)
village_night = Image.blend(village_night, Image.new('RGB', village_night.size, '#10263d'), 0.32)
village_night.save(backgrounds / 'NangouVillage_Premium_Night_1080.jpg', quality=94)

night = Image.open(backgrounds / 'PineRidge_Premium_1080.jpg').convert('RGB')
night = ImageEnhance.Brightness(night).enhance(0.58)
night = ImageEnhance.Contrast(night).enhance(1.12)
blue = Image.new('RGB', night.size, '#162c43')
night = Image.blend(night, blue, 0.28)
vignette = Image.new('L', night.size, 0)
vignette_draw = ImageDraw.Draw(vignette)
vignette_draw.ellipse((-360, -260, 2280, 1500), fill=235)
vignette = vignette.filter(ImageFilter.GaussianBlur(180))
dark = Image.new('RGB', night.size, '#07131f')
night = Image.composite(night, dark, vignette)
night.save(backgrounds / 'PineRidge_Premium_Night_1080.jpg', quality=94)


def panel(size, output, alpha_top, alpha_bottom, corners=True):
    width, height = size
    image = Image.new('RGBA', size)
    pixels = image.load()
    for y in range(height):
        factor = y / max(1, height - 1)
        color = (
            round(21 + 15 * factor),
            round(37 + 20 * factor),
            round(48 + 19 * factor),
            round(alpha_top + (alpha_bottom - alpha_top) * factor))
        for x in range(width):
            pixels[x, y] = color

    draw = ImageDraw.Draw(image)
    gold = (204, 177, 112, 235)
    pale = (224, 232, 222, 90)
    draw.rounded_rectangle((2, 2, width - 3, height - 3), radius=8, outline=gold, width=2)
    draw.rounded_rectangle((10, 10, width - 11, height - 11), radius=5, outline=pale, width=1)
    if corners:
        draw.line((34, 16, width - 34, 16), fill=(197, 218, 208, 75), width=1)
        for x in (18, width - 18):
            for y in (18, height - 18):
                draw.polygon(((x, y - 7), (x + 7, y), (x, y + 7), (x - 7, y)), outline=gold)
    image.save(output)


ui = ART / 'UI'
ui.mkdir(parents=True, exist_ok=True)
panel((1728, 260), ui / 'ImmortalGlassPanel.png', 226, 207)
panel((760, 84), ui / 'ImmortalGlassChoice.png', 235, 216, corners=False)

prompts = {
    'generator': 'OpenAI built-in image generation tool',
    'generated_on': '2026-09-22 through 2026-09-24',
    'style_boundary': 'Original contemporary xianxia direction; no existing characters, logos, UI, or copied composition.',
    'assets': [
        {
            'path': 'AssetSources/Art/Generated/CloudGate_Premium.png',
            'role': 'original mountain gate background source',
            'prompt_summary': 'Rainy immortal-sect gate, white stone stairs, cloud sea, jade/cyan/ivory/gold palette.'
        },
        {
            'path': 'AssetSources/Art/Generated/PineRidge_Premium.png',
            'role': 'original pine ridge background source',
            'prompt_summary': 'Blue-hour pine ridge, broken stele, rain shelter, distant sect lights.'
        },
        {
            'path': 'AssetSources/Art/Generated/LinYue_Premium.png',
            'role': 'original Lin Yue portrait source before background removal',
            'prompt_summary': 'Young investigator cultivator, white/cyan/charcoal hanfu, jade token and folded umbrella.'
        },
        {
            'path': 'Assets/_WenXinJie/Art/Generated/LinYue_Premium_Cutout.png',
            'role': 'original Lin Yue standing portrait with transparent alpha',
            'prompt_summary': 'Background-removed derivative of the generated Lin Yue portrait source.'
        },
        {
            'path': 'AssetSources/Art/Generated/SteleCorridor_Premium.png',
            'role': 'original old stele corridor background source',
            'prompt_summary': 'Post-rain archive gallery, memorial steles, sealed casket, cloud mountains and restrained lantern light.'
        },
        {
            'path': 'AssetSources/Art/Generated/DiscipleQuartersNight_Premium.png',
            'role': 'original outer-disciple residence night background source',
            'prompt_summary': 'Searched disciple room, moonlit covered walkway, distant patrol lanterns and wet mountain architecture.'
        },
        {
            'path': 'AssetSources/Art/Generated/MountainHeartGate_Premium.png',
            'role': 'original mountain-heart sealed gate background source',
            'prompt_summary': 'Ancient seamless stone gate, jade-gold channels, mountain mist and restrained furnace glow.'
        }
    ]
}

for record in prompts['assets']:
    path = ROOT / record['path']
    record['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()

(ROOT / 'Docs/AssetResearch/generated-assets.json').write_text(
    json.dumps(prompts, ensure_ascii=False, indent=2), encoding='utf8')

campaign_asset_paths = [
    'Assets/_WenXinJie/Art/Backgrounds/CloudGate_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/PineRidge_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/PineRidge_Premium_Night_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/SteleCorridor_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/DiscipleQuartersNight_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/MountainHeartGate_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/MedicineHouse_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/NangouVillage_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/NangouVillage_Premium_Night_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/CouncilHall_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/FurnaceChamber_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/DiversionValley_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/DiversionValley_Premium_Night_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/DiversionValley_Aftermath_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/CraftsmanHome_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/VillagerHome_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/XuFamilyHome_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/OldBellTower_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/RegistryOffice_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/Storehouse_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/TransferDesk_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Backgrounds/ExaminationCourt_Premium_1080.jpg',
    'Assets/_WenXinJie/Art/Generated/GuHeng_Premium_Cutout.png',
    'Assets/_WenXinJie/Art/Generated/AChe_Premium_Cutout.png',
    'Assets/_WenXinJie/Art/Generated/XuCheng_Premium_Cutout.png',
    'Assets/_WenXinJie/Art/Generated/ShenYan_Premium_Cutout.png',
    'Assets/_WenXinJie/Art/Generated/LinYue_Premium_Cutout.png',
    'Assets/_WenXinJie/Art/UI/ImmortalGlassPanel.png',
    'Assets/_WenXinJie/Art/UI/ImmortalGlassChoice.png',
    'Assets/_WenXinJie/Audio/BGM/AsianOriental2_Tozan.ogg',
    'Assets/_WenXinJie/Audio/Ambience/Rain_Window_Ylmir.ogg',
    'Assets/_WenXinJie/Fonts/NotoSerifSC-Regular.ttf',
    'Assets/_WenXinJie/Fonts/OFL.txt',
    'Assets/_WenXinJie/Fonts/ZhiMangXing-Regular.ttf',
    'Assets/_WenXinJie/Fonts/ZhiMangXing-OFL.txt',
    'Assets/_WenXinJie/Fonts/ZCOOLXiaoWei-Regular.ttf',
    'Assets/_WenXinJie/Fonts/ZCOOLXiaoWei-OFL.txt'
]
campaign_assets = []
for relative_path in campaign_asset_paths:
    path = ROOT / relative_path
    campaign_assets.append({
        'path': relative_path,
        'bytes': path.stat().st_size,
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest()
    })

(ROOT / 'Docs/AssetResearch/campaign-assets.json').write_text(
    json.dumps(campaign_assets, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
print('Prepared premium art, provenance, and the campaign asset checksum inventory.')
