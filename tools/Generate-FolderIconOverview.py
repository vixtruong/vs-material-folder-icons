from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
CLOSED_DIR = ROOT / "GeneratedImagesPng" / "folders"
OPEN_DIR = ROOT / "GeneratedImagesPng" / "foldersOpen"
OUTPUT = ROOT / "docs" / "images" / "folder-icons-overview.png"

WIDTH = 3840
COLUMNS = 12
HEADER_HEIGHT = 184
TILE_WIDTH = WIDTH // COLUMNS
TILE_HEIGHT = 98
ICON_SIZE = 48
GRID = "#dde4ee"
BACKGROUND = "#f8fafc"
PANEL = "#ffffff"
TEXT = "#122033"
MUTED = "#58687d"
META = "#6b7b91"


def load_font(name: str, size: int) -> ImageFont.FreeTypeFont:
    font_path = Path("C:/Windows/Fonts") / name
    return ImageFont.truetype(str(font_path), size=size)


TITLE_FONT = load_font("segoeuib.ttf", 54)
SUBTITLE_FONT = load_font("segoeui.ttf", 27)
NAME_FONT = load_font("segoeuib.ttf", 21)
META_FONT = load_font("segoeui.ttf", 15)


def resize_icon(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA").resize(
        (ICON_SIZE, ICON_SIZE),
        Image.Resampling.NEAREST,
    )


def fit_text(draw: ImageDraw.ImageDraw, value: str, font: ImageFont.FreeTypeFont, max_width: int) -> str:
    if draw.textlength(value, font=font) <= max_width:
        return value

    suffix = "..."
    available = max_width - int(draw.textlength(suffix, font=font))
    if available <= 0:
        return suffix

    fitted = value
    while fitted and draw.textlength(fitted, font=font) > available:
        fitted = fitted[:-1]

    return fitted.rstrip() + suffix


def main() -> None:
    names = sorted(path.stem for path in CLOSED_DIR.glob("*.png"))
    rows = (len(names) + COLUMNS - 1) // COLUMNS
    height = HEADER_HEIGHT + (rows * TILE_HEIGHT)

    image = Image.new("RGB", (WIDTH, height), BACKGROUND)
    draw = ImageDraw.Draw(image)

    draw.rectangle((0, 0, WIDTH, HEADER_HEIGHT), fill=PANEL)
    draw.line((0, HEADER_HEIGHT - 1, WIDTH, HEADER_HEIGHT - 1), fill=GRID, width=2)
    draw.text((72, 54), "Material Folder Icons for Visual Studio", font=TITLE_FONT, fill=TEXT)
    draw.text(
        (72, 116),
        f"{len(names)} bundled closed folder icons. Matching open-folder variants are included in the VSIX.",
        font=SUBTITLE_FONT,
        fill=MUTED,
    )

    closed_cache = {}
    open_cache = {}
    for index, name in enumerate(names):
        row = index // COLUMNS
        column = index % COLUMNS
        x = column * TILE_WIDTH
        y = HEADER_HEIGHT + (row * TILE_HEIGHT)

        draw.rectangle((x, y, x + TILE_WIDTH, y + TILE_HEIGHT), fill=PANEL, outline=GRID, width=2)

        closed_cache[name] = closed_cache.get(name) or resize_icon(CLOSED_DIR / f"{name}.png")
        image.paste(closed_cache[name], (x + 24, y + 25), closed_cache[name])

        open_path = OPEN_DIR / f"{name}.png"
        has_open = open_path.exists()
        if has_open:
            open_cache[name] = open_cache.get(name) or resize_icon(open_path)
            image.paste(open_cache[name], (x + 78, y + 25), open_cache[name])

        label = fit_text(draw, name, NAME_FONT, TILE_WIDTH - 154)
        draw.text((x + 140, y + 30), label, font=NAME_FONT, fill=TEXT)
        draw.text((x + 140, y + 62), "closed + open" if has_open else "closed only", font=META_FONT, fill=META)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT, format="PNG", optimize=True)
    print(f"icons={len(names)}")
    print(f"size={WIDTH}x{height}")
    print(f"output={OUTPUT}")


if __name__ == "__main__":
    main()
