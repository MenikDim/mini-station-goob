#!/usr/bin/env python3
"""Mars lobby — static scene, animated rocket exhaust + twinkling stars."""

from __future__ import annotations

import json
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

FRAME_W = 320
FRAME_H = 180
FRAME_COUNT = 64
FRAME_DELAY = 0.40
SUPERSAMPLE = 4
SEED = 0x4D415253

ROOT = Path(__file__).resolve().parents[1]
RSI_DIR = ROOT / "Resources" / "Textures" / "_Mini" / "Lobby" / "mars.rsi"
OUT_DIR = RSI_DIR

SW = FRAME_W * SUPERSAMPLE
SH = FRAME_H * SUPERSAMPLE


def tri_wave(frame: int, period: int) -> float:
    p = frame % period
    h = period / 2
    return p / h if p < h else 2.0 - p / h


def load_source() -> np.ndarray:
    src = RSI_DIR / "source.png"
    if not src.exists():
        alt = ROOT / "Resources" / "Textures" / "_Mini" / "Lobby" / "1.png"
        if not alt.exists():
            raise FileNotFoundError(f"Put source.png in {RSI_DIR}")
        src = alt
    img = Image.open(src).convert("RGB")
    if img.size != (FRAME_W, FRAME_H):
        img = img.resize((FRAME_W, FRAME_H), Image.LANCZOS)
    return np.array(img.resize((SW, SH), Image.LANCZOS), dtype=np.float32)


def build_planet_mask(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy = w * 0.28, h * 0.32
    dist = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    planet = (dist < w * 0.36) & (r > 55)
    m = Image.fromarray((planet.astype(np.uint8) * 255), "L")
    m = m.filter(ImageFilter.GaussianBlur(radius=4))
    return np.array(m, dtype=np.float32) / 255.0


def build_exhaust_masks(rgb: np.ndarray, planet_mask: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Маски: весь шлейф, ядро сопла, горячая точка."""
    r = rgb[:, :, 0]
    g = rgb[:, :, 1]
    b = rgb[:, :, 2]
    lum = r + g + b
    h, w = rgb.shape[:2]
    yy = np.linspace(0, 1, h, dtype=np.float32)[:, None]
    not_planet = 1.0 - planet_mask

    plume = (
        (lum > 280)
        & (g > 85)
        & (r > 120)
        & (yy > 0.14)
    ).astype(np.float32) * not_planet

    core = (
        (lum > 360)
        & (g > 110)
        & (yy > 0.16)
    ).astype(np.float32) * not_planet

    nozzle = (
        (lum > 400)
        & (g > 125)
        & (r > 160)
    ).astype(np.float32) * not_planet

    def soften(m: np.ndarray, r_blur: float, dilate: int = 0) -> np.ndarray:
        img = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8), "L")
        if dilate:
            img = img.filter(ImageFilter.MaxFilter(dilate * 2 + 1))
        img = img.filter(ImageFilter.GaussianBlur(radius=r_blur))
        return np.array(img, dtype=np.float32) / 255.0

    return soften(plume, 2.0, 1), soften(core, 1.0, 0), soften(nozzle, 0.6, 0)


def find_nozzle_seeds(exhaust_core: np.ndarray, lum: np.ndarray) -> list[tuple[float, float, int]]:
    """Центры сопел для индивидуального мерцания."""
    h, w = lum.shape
    seeds: list[tuple[float, float, int]] = []
    step = 28
    for y in range(step, h - step, step):
        for x in range(step, w - step, step):
            patch_m = exhaust_core[y - 8:y + 8, x - 8:x + 8]
            if patch_m.max() < 0.35:
                continue
            patch_l = lum[y - 8:y + 8, x - 8:x + 8]
            ly, lx = np.unravel_index(patch_l.argmax(), patch_l.shape)
            cx, cy = x - 8 + lx, y - 8 + ly
            if any(abs(cx - s[0]) < 20 and abs(cy - s[1]) < 20 for s in seeds):
                continue
            seeds.append((float(cx), float(cy), len(seeds)))
    return seeds[:6]


def shift_rgb(img: np.ndarray, dx: float, dy: float) -> np.ndarray:
    """Сдвиг изображения (для дрожания шлейфа)."""
    h, w = img.shape[:2]
    ix, iy = int(round(dx)), int(round(dy))
    out = np.zeros_like(img)
    xs0 = max(0, ix)
    xs1 = min(w, w + ix)
    ys0 = max(0, iy)
    ys1 = min(h, h + iy)
    xd0 = max(0, -ix)
    xd1 = xd0 + (xs1 - xs0)
    yd0 = max(0, -iy)
    yd1 = yd0 + (ys1 - ys0)
    if xs1 > xs0 and ys1 > ys0:
        out[ys0:ys1, xs0:xs1] = img[yd0:yd1, xd0:xd1]
    return out


def animate_exhaust(
    base: np.ndarray,
    plume_mask: np.ndarray,
    core_mask: np.ndarray,
    nozzle_mask: np.ndarray,
    nozzle_seeds: list[tuple[float, float, int]],
    frame: int,
) -> np.ndarray:
    """Шлейф слегка дрожит и мерцает — как настоящее сопло."""
    t = frame / FRAME_COUNT * math.tau
    h, w = base.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)

    # общее покачивание шлейфа вдоль выхлопа
    dx = math.sin(t) * 2.5 + math.sin(t * 2.05 + 0.7) * 1.2
    dy = math.cos(t * 0.9) * 2.0 + math.sin(t * 1.55 + 1.1) * 1.0

    flicker = (
        0.90
        + 0.10 * tri_wave(frame, FRAME_COUNT)
        + 0.06 * tri_wave(frame, 32)
        + 0.04 * tri_wave(frame, 11)
    )

    plume_shifted = shift_rgb(base, dx, dy)
    m3 = plume_mask[..., None]
    arr = base * (1 - m3) + plume_shifted * m3 * flicker

    # бегущие волны яркости вдоль шлейфа
    along = xx * 0.7 - yy * 0.95
    wave = (np.sin(along * 0.08 - t * 0.9) + 1) * 0.5
    arr[:, :, 0] = np.clip(arr[:, :, 0] + plume_mask * wave * 12, 0, 255)
    arr[:, :, 1] = np.clip(arr[:, :, 1] + plume_mask * wave * 10, 0, 255)
    arr[:, :, 2] = np.clip(arr[:, :, 2] + plume_mask * (1 - wave) * 8, 0, 255)

    # каждое сопло — своё дрожание + мерцание ядра
    for nx, ny, idx in nozzle_seeds:
        ndx = dx + math.sin(t + idx * 1.9) * 1.8
        ndy = dy + math.cos(t * 0.85 + idx * 2.3) * 1.4
        local = shift_rgb(base, ndx, ndy)

        pulse = 0.82 + 0.18 * tri_wave(frame + idx * 7, 32)
        pulse_fast = 0.88 + 0.12 * tri_wave(frame + idx * 3, 8)

        # локальная зона вокруг сопла
        d2 = (xx - nx) ** 2 + (yy - ny) ** 2
        zone = np.clip(1 - d2 / (55 * 55), 0, 1) ** 1.5
        z3 = zone[..., None]

        blended = base * (1 - z3) + local * z3 * pulse
        arr = arr * (1 - z3) + blended * z3

        # горячая точка
        core_r = 12 + pulse_fast * 6
        core_d2 = (xx - nx) ** 2 + (yy - ny) ** 2
        core_f = np.clip(1 - core_d2 / (core_r * core_r), 0, 1) ** 2
        arr[:, :, 0] = np.clip(arr[:, :, 0] + core_f * pulse_fast * 35, 0, 255)
        arr[:, :, 1] = np.clip(arr[:, :, 1] + core_f * pulse_fast * 30, 0, 255)
        arr[:, :, 2] = np.clip(arr[:, :, 2] + core_f * (1 - pulse_fast) * 40 + core_f * pulse_fast * 15, 0, 255)

    # ядро шлейфа
    core_flicker = 0.86 + 0.14 * tri_wave(frame, 24)
    cm = core_mask[..., None]
    core_shift = shift_rgb(base, dx * 0.6, dy * 0.6)
    arr = arr * (1 - cm) + core_shift * cm * core_flicker

    # ультра-яркие точки сопла
    nm = nozzle_mask * (0.85 + 0.15 * tri_wave(frame, 16))
    arr[:, :, 0] = np.clip(arr[:, :, 0] + nm * 18, 0, 255)
    arr[:, :, 1] = np.clip(arr[:, :, 1] + nm * 16, 0, 255)
    arr[:, :, 2] = np.clip(arr[:, :, 2] + nm * 22, 0, 255)

    return arr


STAR_PALETTE = [
    (255, 255, 255),
    (255, 250, 220),
    (255, 220, 180),
    (255, 200, 200),
    (255, 180, 220),
    (200, 220, 255),
    (180, 200, 255),
    (160, 255, 240),
    (200, 255, 200),
    (255, 230, 150),
    (230, 200, 255),
]


def make_stars(rng: random.Random, rgb: np.ndarray, planet_mask: np.ndarray) -> list[dict]:
    stars: list[dict] = []
    h, w = rgb.shape[:2]
    lum = rgb.mean(axis=2)

    for _ in range(420):
        x = rng.uniform(0, w)
        y = rng.uniform(0, h * 0.88)
        ix, iy = int(x), int(y)
        if ix >= w or iy >= h:
            continue
        if planet_mask[iy, ix] > 0.15:
            continue
        if lum[iy, ix] > 78:
            continue
        stars.append({
            "x": x, "y": y,
            "phase": rng.randint(0, FRAME_COUNT - 1),
            "period": rng.choice([4, 6, 8, 12, 16, 24, 48]),
            "size": rng.choice([1, 1, 2, 2, 3]),
            "color": rng.choice(STAR_PALETTE),
            "glow": rng.uniform(0.7, 1.0),
        })

    # усилить уже нарисованные звёзды на арте
    for y in range(3, h - 3, 2):
        for x in range(3, w - 3, 2):
            if planet_mask[y, x] > 0.2:
                continue
            if not (150 < lum[y, x] < 245):
                continue
            patch = lum[y - 2:y + 3, x - 2:x + 3]
            if lum[y, x] < patch.max() - 1:
                continue
            stars.append({
                "x": float(x), "y": float(y),
                "phase": rng.randint(0, FRAME_COUNT - 1),
                "period": rng.choice([6, 8, 12, 16, 24]),
                "size": 3 if lum[y, x] > 200 else 2,
                "color": rng.choice(STAR_PALETTE),
                "glow": 1.0,
            })
    return stars


def draw_stars(arr: np.ndarray, stars: list[dict], frame: int) -> None:
    """Звёзды — additive поверх, исходные цвета не трогаем."""
    h, w = arr.shape[:2]

    for s in stars:
        tw = tri_wave(frame + s["phase"], s["period"]) ** 1.6
        if tw < 0.05:
            continue
        x, y = int(s["x"]), int(s["y"])
        if not (0 <= x < w and 0 <= y < h):
            continue
        c = np.array(s["color"], dtype=np.float32) * s.get("glow", 1.0)
        sz = s["size"]

        # мягкое свечение
        gr = sz + 3
        for dy in range(-gr, gr + 1):
            for dx in range(-gr, gr + 1):
                d2 = dx * dx + dy * dy
                if d2 > gr * gr:
                    continue
                sx, sy = x + dx, y + dy
                if 0 <= sx < w and 0 <= sy < h:
                    fall = (1 - math.sqrt(d2) / (gr + 0.01)) ** 2
                    arr[sy, sx] = np.clip(arr[sy, sx] + c * fall * tw * 0.45, 0, 255)

        # ядро
        for dy in range(-sz, sz + 1):
            for dx in range(-sz, sz + 1):
                if dx * dx + dy * dy <= sz * sz + 0.2:
                    sx, sy = x + dx, y + dy
                    if 0 <= sx < w and 0 <= sy < h:
                        arr[sy, sx] = np.clip(arr[sy, sx] + c * tw * 0.75, 0, 255)

        # лучи
        if sz >= 2 and tw > 0.5:
            ray = c * tw * 0.35
            for dx in range(-4, 5):
                sx = x + dx
                if 0 <= sx < w:
                    arr[y, sx] = np.clip(arr[y, sx] + ray, 0, 255)
            for dy in range(-4, 5):
                sy = y + dy
                if 0 <= sy < h:
                    arr[sy, x] = np.clip(arr[sy, x] + ray, 0, 255)


def render_frame(
    base: np.ndarray,
    plume_mask: np.ndarray,
    core_mask: np.ndarray,
    nozzle_mask: np.ndarray,
    nozzle_seeds: list[tuple[float, float, int]],
    stars: list[dict],
    frame: int,
) -> Image.Image:
    arr = animate_exhaust(base, plume_mask, core_mask, nozzle_mask, nozzle_seeds, frame)
    draw_stars(arr, stars, frame)
    return Image.fromarray(arr.astype(np.uint8), "RGB").resize((FRAME_W, FRAME_H), Image.LANCZOS)


def build_strip() -> Image.Image:
    rng = random.Random(SEED)
    base = load_source()
    planet = build_planet_mask(base)
    plume, core, nozzle = build_exhaust_masks(base, planet)
    lum = base[:, :, 0] + base[:, :, 1] + base[:, :, 2]
    nozzle_seeds = find_nozzle_seeds(core, lum)
    stars = make_stars(rng, base, planet)

    strip = Image.new("RGB", (FRAME_W * FRAME_COUNT, FRAME_H))
    for i in range(FRAME_COUNT):
        strip.paste(
            render_frame(base, plume, core, nozzle, nozzle_seeds, stars, i),
            (i * FRAME_W, 0),
        )
        print(f"  frame {i + 1}/{FRAME_COUNT}", end="\r", flush=True)
    print()
    return strip


def write_meta() -> None:
    meta = {
        "version": 1,
        "license": "CC-BY-SA-3.0",
        "copyright": "Mini Station lobby — Mars scene animation",
        "size": {"x": FRAME_W, "y": FRAME_H},
        "states": [{"name": "1", "delays": [[FRAME_DELAY] * FRAME_COUNT]}],
    }
    (OUT_DIR / "meta.json").write_text(json.dumps(meta, indent=4) + "\n", encoding="utf-8")


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    strip = build_strip()
    strip.save(OUT_DIR / "1.png", optimize=True)
    write_meta()
    print(f"Wrote {OUT_DIR / '1.png'} ({strip.size[0]}x{strip.size[1]}, static + exhaust + stars)")


if __name__ == "__main__":
    main()
