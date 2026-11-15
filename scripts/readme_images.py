from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs" / "images" / "source"
OUT = ROOT / "docs" / "images"
FONTS = Path("C:/Windows/Fonts")

CARD = (18, 18, 22)
BORDER = (48, 48, 56)
TEXT = (244, 244, 247)
MUTED = (150, 150, 165)
SUPERSAMPLE = 4

LED_COLORS = [(46, 204, 64)] * 4 + [(255, 196, 0)] * 4 + [(255, 46, 46)] * 4

GRID_COLUMNS, GRID_ROWS = 2, 5
TILE_GAP, CARD_PADDING = 12, 20
TILE_RADIUS, CARD_RADIUS = 12, 24

SCREENSHOT_HEIGHT = 490
SCREENSHOT_RADIUS = 16

INSTALLER_TITLE_BAR = 31


def rounded_mask(size, radius):
    w, h = size
    big = Image.new("L", (w * SUPERSAMPLE, h * SUPERSAMPLE), 0)
    ImageDraw.Draw(big).rounded_rectangle(
        (0, 0, w * SUPERSAMPLE - 1, h * SUPERSAMPLE - 1), radius * SUPERSAMPLE, fill=255
    )
    return big.resize(size, Image.LANCZOS)


def rounded_card(size, radius):
    outer = rounded_mask(size, radius)
    inner = Image.new("L", size, 0)
    inner.paste(rounded_mask((size[0] - 2, size[1] - 2), radius - 1), (1, 1))
    card = Image.new("RGBA", size, BORDER + (0,))
    card.putalpha(outer)
    body = Image.new("RGBA", size, CARD + (255,))
    card.paste(body, (0, 0), inner)
    return card


def font(name, size):
    return ImageFont.truetype(str(FONTS / name), size)


def led_row(width, height):
    leds = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    highlights = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    led_draw, highlight_draw = ImageDraw.Draw(leds), ImageDraw.Draw(highlights)
    radius, step = 22, width / len(LED_COLORS)
    for i, color in enumerate(LED_COLORS):
        t = (i + 0.5) / len(LED_COLORS) * 2 - 1
        x, y = step * (i + 0.5), height / 2 + 18 * t * t
        led_draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=color + (255,))
        highlight_draw.ellipse((x - 9, y - 12, x + 3, y - 4), fill=(255, 255, 255, 110))
    glow = leds.filter(ImageFilter.GaussianBlur(18))
    out = Image.alpha_composite(glow, glow)
    out = Image.alpha_composite(out, leds)
    return Image.alpha_composite(out, highlights)


def banner():
    w, h = 1320, 840
    card = rounded_card((w, h), 40)
    leds = led_row(1100, 140)
    card.alpha_composite(leds, ((w - leds.width) // 2, 150))
    draw = ImageDraw.Draw(card)
    draw.text((w // 2, 420), "Polsimer F74LED", font=font("segoeuib.ttf", 128), fill=TEXT, anchor="mm")
    draw.text((w // 2, 545), "SimHub LED plugin", font=font("segoeuib.ttf", 60), fill=TEXT, anchor="mm")
    draw.line((110, 650, w - 110, 650), fill=BORDER, width=2)
    draw.text((w // 2, 730), "Your SimHub LED effects, on the wheel.", font=font("segoeui.ttf", 44), fill=MUTED, anchor="mm")
    card.save(OUT / "banner.png", optimize=True)


def led_effects():
    src = Image.open(SOURCE / "led_effects.gif")
    tile_w, tile_h = src.width // GRID_COLUMNS, src.height // GRID_ROWS
    w = CARD_PADDING * 2 + GRID_COLUMNS * tile_w + (GRID_COLUMNS - 1) * TILE_GAP
    h = CARD_PADDING * 2 + GRID_ROWS * tile_h + (GRID_ROWS - 1) * TILE_GAP
    card = rounded_card((w, h), CARD_RADIUS)
    tile_mask = rounded_mask((tile_w, tile_h), TILE_RADIUS)
    transparent = ImageChops.invert(card.getchannel("A").point(lambda a: 255 if a >= 128 else 0))

    frames, durations = [], []
    for i in range(src.n_frames):
        src.seek(i)
        frame = src.convert("RGB")
        canvas = card.copy()
        for row in range(GRID_ROWS):
            for col in range(GRID_COLUMNS):
                tile = frame.crop((col * tile_w, row * tile_h, (col + 1) * tile_w, (row + 1) * tile_h))
                x = CARD_PADDING + col * (tile_w + TILE_GAP)
                y = CARD_PADDING + row * (tile_h + TILE_GAP)
                canvas.paste(tile, (x, y), tile_mask)
        rgb = canvas.convert("RGB").quantize(255, method=Image.Quantize.MEDIANCUT)
        rgb.paste(255, (0, 0), transparent)
        frames.append(rgb)
        durations.append(src.info.get("duration", 100))

    frames[0].save(
        OUT / "led_effects.gif",
        save_all=True,
        append_images=frames[1:],
        duration=durations,
        loop=0,
        transparency=255,
        disposal=2,
    )


def framed(shot, name):
    border = 1
    card = rounded_card((shot.width + border * 2, shot.height + border * 2), SCREENSHOT_RADIUS)
    card.paste(shot, (border, border), rounded_mask(shot.size, SCREENSHOT_RADIUS - border))
    card.save(OUT / name, optimize=True)


def settings():
    src = Image.open(SOURCE / "settings_preview.png").convert("RGB")
    framed(src.crop((0, 0, src.width, SCREENSHOT_HEIGHT)), "settings_preview.png")


def installer():
    src = Image.open(SOURCE / "installer.png").convert("RGB")
    framed(src.crop((0, INSTALLER_TITLE_BAR, src.width, src.height)), "installer.png")


if __name__ == "__main__":
    banner()
    led_effects()
    settings()
    installer()
