from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
CARD_ART = ROOT / "card_art"
SMALL_DIR = ROOT / "power_icons"
BIG_DIR = ROOT / "power_icons_big"

ICON_SLUGS = [
    "bagua",
    "baiyin",
    "chitu",
    "dawan",
    "dilu",
    "ganjiangmoye",
    "guanshi",
    "guding",
    "hanbing",
    "jueying",
    "kongcheng",
    "mengdexinshu",
    "muniu",
    "qilin",
    "qinggang",
    "renwang",
    "taiping",
    "tengjia",
    "yuxi",
    "zhangba",
    "zhuge",
]


def center_crop_square(image: Image.Image) -> Image.Image:
    width, height = image.size
    side = min(width, height)
    left = (width - side) // 2
    top = int((height - side) * 0.42)
    top = max(0, min(height - side, top))
    return image.crop((left, top, left + side, top + side))


def radial_mask(size: int, inset: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.ellipse((inset, inset, size - inset - 1, size - inset - 1), fill=255)
    return mask


def make_icon(source: Image.Image, size: int) -> Image.Image:
    pad = max(4, size // 18)
    ring_outer = max(2, size // 24)
    ring_inner = max(1, size // 64)

    art = center_crop_square(source.convert("RGBA")).resize((size, size), Image.Resampling.LANCZOS)
    art = ImageEnhance.Color(art).enhance(0.9)
    art = ImageEnhance.Contrast(art).enhance(1.2)
    art = ImageEnhance.Sharpness(art).enhance(1.25)

    # One shared bronze-and-ink treatment keeps painterly and flat source art in the same UI family.
    sepia = Image.new("RGBA", (size, size), (116, 75, 36, 70))
    art = Image.alpha_composite(art, sepia)

    vignette = Image.new("L", (size, size), 0)
    vd = ImageDraw.Draw(vignette)
    vd.ellipse((-size // 5, -size // 5, size + size // 5, size + size // 5), fill=255)
    vignette = vignette.filter(ImageFilter.GaussianBlur(size // 7))
    dark = Image.new("RGBA", (size, size), (8, 5, 3, 120))
    art = Image.composite(art, Image.alpha_composite(art, dark), vignette)

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    shadow = radial_mask(size, pad)
    shadow = shadow.filter(ImageFilter.GaussianBlur(max(1, size // 32)))
    canvas.paste(Image.new("RGBA", (size, size), (0, 0, 0, 130)), (0, 0), shadow)

    art_mask = radial_mask(size, pad + ring_outer)
    canvas.paste(art, (0, 0), art_mask)

    draw = ImageDraw.Draw(canvas)
    outer = (pad, pad, size - pad - 1, size - pad - 1)
    mid = (pad + ring_outer, pad + ring_outer, size - pad - ring_outer - 1, size - pad - ring_outer - 1)
    inner = (
        pad + ring_outer + ring_inner,
        pad + ring_outer + ring_inner,
        size - pad - ring_outer - ring_inner - 1,
        size - pad - ring_outer - ring_inner - 1,
    )

    draw.ellipse(outer, outline=(42, 24, 11, 255), width=max(2, ring_outer))
    draw.ellipse(mid, outline=(224, 160, 74, 255), width=max(1, ring_inner * 2))
    draw.ellipse(inner, outline=(75, 42, 18, 230), width=max(1, ring_inner))

    highlight = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    hd = ImageDraw.Draw(highlight)
    hd.arc((pad + ring_outer, pad + ring_outer, size - pad - ring_outer, size - pad - ring_outer), 205, 320, fill=(255, 224, 148, 160), width=max(1, size // 56))
    hd.arc((pad + ring_outer, pad + ring_outer, size - pad - ring_outer, size - pad - ring_outer), 25, 95, fill=(255, 244, 196, 110), width=max(1, size // 80))
    canvas = Image.alpha_composite(canvas, highlight)

    return canvas


def main() -> None:
    SMALL_DIR.mkdir(parents=True, exist_ok=True)
    BIG_DIR.mkdir(parents=True, exist_ok=True)

    missing: list[str] = []
    for slug in ICON_SLUGS:
        source_path = CARD_ART / f"{slug}.png"
        if not source_path.exists():
            missing.append(slug)
            continue

        source = Image.open(source_path)
        make_icon(source, 64).save(SMALL_DIR / f"{slug}.png")
        make_icon(source, 256).save(BIG_DIR / f"{slug}.png")

    if missing:
        raise SystemExit(f"Missing card art for: {', '.join(missing)}")


if __name__ == "__main__":
    main()
