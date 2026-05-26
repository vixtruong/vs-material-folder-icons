from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
HIGH_RES_CLOSED_DIR = ROOT / "obj" / "OverviewImagesPng" / "folders"
DEFAULT_CLOSED_DIR = ROOT / "GeneratedImagesPng" / "folders"
OUTPUT = ROOT / "docs" / "images" / "folder-icons-overview.png"

WIDTH = 1920
HEIGHT = 1080
COLUMNS = 29
HEADER_HEIGHT = 88
ICON_SIZE = 40
GRID = "#dde4ee"
BACKGROUND = "#f8fafc"
PANEL = "#ffffff"
TEXT = "#122033"
MUTED = "#58687d"


def load_font(name: str, size: int) -> ImageFont.FreeTypeFont:
    font_path = Path("C:/Windows/Fonts") / name
    return ImageFont.truetype(str(font_path), size=size)


TITLE_FONT = load_font("segoeuib.ttf", 32)
SUBTITLE_FONT = load_font("segoeui.ttf", 16)
NAME_FONT = load_font("segoeuib.ttf", 10)


def resize_icon(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA").resize(
        (ICON_SIZE, ICON_SIZE),
        Image.Resampling.LANCZOS,
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
    closed_dir = HIGH_RES_CLOSED_DIR if HIGH_RES_CLOSED_DIR.exists() else DEFAULT_CLOSED_DIR
    names = sorted(path.stem for path in closed_dir.glob("*.png"))
    rows = (len(names) + COLUMNS - 1) // COLUMNS
    tile_width = WIDTH // COLUMNS
    tile_height = (HEIGHT - HEADER_HEIGHT) // rows

    image = Image.new("RGB", (WIDTH, HEIGHT), BACKGROUND)
    draw = ImageDraw.Draw(image)

    draw.rectangle((0, 0, WIDTH, HEADER_HEIGHT), fill=PANEL)
    draw.line((0, HEADER_HEIGHT - 1, WIDTH, HEADER_HEIGHT - 1), fill=GRID, width=2)
    draw.text((32, 22), "Material Folder Icons for Visual Studio", font=TITLE_FONT, fill=TEXT)
    draw.text(
        (32, 58),
        f"{len(names)} bundled closed folder icons. FullHD overview renders folder icons only.",
        font=SUBTITLE_FONT,
        fill=MUTED,
    )

    closed_cache = {}
    for index, name in enumerate(names):
        row = index // COLUMNS
        column = index % COLUMNS
        x = column * tile_width
        y = HEADER_HEIGHT + (row * tile_height)

        draw.rectangle((x, y, x + tile_width, y + tile_height), fill=PANEL, outline=GRID, width=1)

        closed_cache[name] = closed_cache.get(name) or resize_icon(closed_dir / f"{name}.png")
        icon_x = x + ((tile_width - ICON_SIZE) // 2)
        image.paste(closed_cache[name], (icon_x, y + 8), closed_cache[name])

        label = fit_text(draw, name, NAME_FONT, tile_width - 8)
        label_x = x + max(4, int((tile_width - draw.textlength(label, font=NAME_FONT)) / 2))
        draw.text((label_x, y + ICON_SIZE + 11), label, font=NAME_FONT, fill=TEXT)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT, format="PNG", optimize=True)
    print(f"icons={len(names)}")
    print(f"size={WIDTH}x{HEIGHT}")
    print(f"output={OUTPUT}")


if __name__ == "__main__":
    main()
