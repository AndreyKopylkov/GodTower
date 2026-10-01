"""Deterministic generator for all God Tower 2D art.

Run from repo root:
  uv run --with pillow --with numpy --with scipy python Tools/Images/generate_all.py
"""
import os
import sys
import math
import random

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
UI = os.path.join(ROOT, "Assets", "Art", "UI")
SKY = os.path.join(ROOT, "Assets", "Art", "Environment", "Sky")

from PIL import Image, ImageDraw, ImageFilter, ImageChops, ImageFont
import numpy as np
from common import *
from icons import ICONS, render_icon, Canvas, glove, group
import preview

MANIFEST = []  # (relative path, size, nine-slice)


def out(img, sub, name, nine=None):
    p = os.path.join(UI if sub != "SKY" else SKY, *( [sub] if sub not in ("", "SKY") else [] ), name)
    save(img, p)
    MANIFEST.append((os.path.relpath(p, ROOT).replace("\\", "/"), img.size, nine))
    return p


# ---------------------------------------------------------------- clouds (I-01)
def cloud(circles, flat, seed, size=512):
    ss = 2
    N = size * ss
    m = Image.new("L", (N, N), 0)
    d = ImageDraw.Draw(m)
    for cx, cy, r in circles:
        d.ellipse([(cx - r) * ss, (cy - r) * ss, (cx + r) * ss, (cy + r) * ss], fill=255)
    x0, x1, y0, y1 = flat
    d.rounded_rectangle([x0 * ss, y0 * ss, x1 * ss, y1 * ss], radius=(y1 - y0) * ss / 2, fill=255)
    bb = m.getbbox()
    # centre
    dx = (N - (bb[0] + bb[2])) // 2
    dy = (N - (bb[1] + bb[3])) // 2
    m = ImageChops.offset(m, dx, dy)
    soft = m.filter(ImageFilter.GaussianBlur(1.2 * ss))
    lit = ImageChops.offset(m, 0, -int(70 * ss)).filter(ImageFilter.GaussianBlur(34 * ss))
    lit2 = ImageChops.offset(m, int(30 * ss), -int(24 * ss)).filter(ImageFilter.GaussianBlur(14 * ss))
    a = np.array(lit, np.float32) / 255
    b = np.array(lit2, np.float32) / 255
    t = np.clip(a * 0.75 + b * 0.35, 0, 1)[..., None]
    shade = np.array([176, 202, 236], np.float32)
    mid = np.array([228, 240, 255], np.float32)
    white = np.array([255, 255, 255], np.float32)
    rgb = np.where(t < 0.6, shade + (mid - shade) * (t / 0.6), mid + (white - mid) * ((t - 0.6) / 0.4))
    img = np.concatenate([rgb, np.array(soft, np.float32)[..., None]], 2).astype(np.uint8)
    return Image.fromarray(img, "RGBA").resize((size, size), Image.LANCZOS)


def gen_clouds():
    specs = [
        ([(150, 300, 70), (230, 250, 95), (320, 275, 80), (390, 310, 55), (270, 300, 62)], (120, 410, 290, 355)),
        ([(110, 280, 50), (190, 260, 62), (290, 250, 70), (370, 265, 58), (430, 292, 38)], (70, 460, 270, 330)),
        ([(190, 285, 50), (260, 250, 66), (330, 285, 48)], (150, 370, 280, 335)),
        ([(120, 300, 55), (190, 255, 70), (250, 295, 52), (310, 270, 62), (380, 300, 55), (255, 320, 40)], (90, 430, 300, 360)),
    ]
    for i, (c, f) in enumerate(specs, 1):
        out(cloud(c, f, i), "SKY", f"cloud_{i:02d}.png")


# ---------------------------------------------------------------- kit (I-02)
NAVY2 = (22, 30, 70)


def panel():
    g = Glossy(256, 256)
    m = lambda i: g.rrect((6, 6, 250, 244), 40, i)
    img = g.render(m, (255, 252, 240), (222, 236, 250), NAVY2, 6, lip=0, shadow=(0, 6, 4, 0.35), gloss=0.0, rim=0.0)
    # inner decorative line
    ss = 4
    ring = Glossy(256, 256)
    a = ImageChops.subtract(ring.rrect((6, 6, 250, 244), 40, 14), ring.rrect((6, 6, 250, 244), 40, 17))
    ring_img = fill(scale_alpha(a, 0.9), (160, 195, 235)).resize((256, 256), Image.LANCZOS)
    return over(img, ring_img)


def button(top, bot, lipc, outline, state="normal"):
    g = Glossy(256, 112)
    if state == "pressed":
        box, lip = (6, 10, 250, 100), 2
        shadow = (0, 2, 2, 0.3)
    else:
        box, lip = (6, 4, 250, 100), 9
        shadow = (0, 4, 3, 0.35)
    m = lambda i: g.rrect(box, 40, i)
    return g.render(m, top, bot, outline, 5, lip=lip, lip_col=lipc, shadow=shadow, gloss=0.55 if state != "disabled" else 0.25)


def round_button(top, bot, lipc, pressed=False):
    g = Glossy(128, 128)
    box = (6, 12, 122, 120) if pressed else (6, 4, 122, 112)
    lip = 2 if pressed else 8
    m = lambda i: g.ellipse(box, i)
    return g.render(m, top, bot, NAVY2, 5, lip=lip, lip_col=lipc, shadow=(0, 4, 3, 0.35), gloss=0.55)


def heightbar():
    g = Glossy(64, 512)
    m = lambda i: g.rrect((4, 4, 60, 506), 28, i)
    frame = g.render(m, (52, 60, 80), (36, 42, 60), NAVY2, 4, shadow=(0, 3, 2, 0.35), gloss=0.12, gloss_h=0.1, rim=0.15)
    g2 = Glossy(40, 256)
    m2 = lambda i: g2.rrect((0, 0, 40, 256), 20, i)
    fillimg = g2.render(m2, (255, 224, 70), (255, 170, 20), (190, 105, 10), 2, shadow=None, gloss=0.5, gloss_h=0.4)
    return frame, fillimg


def gen_kit():
    out(panel(), "Kit", "panel.png", (64, 64, 64, 64))
    green = ((150, 238, 90), (58, 186, 52), (30, 120, 40), NAVY2)
    out(button(*green), "Kit", "button_normal.png", (48, 48, 48, 48))
    out(button((120, 205, 72), (46, 150, 44), (28, 100, 36), NAVY2, "pressed"), "Kit", "button_pressed.png", (48, 48, 48, 48))
    out(button((200, 206, 218), (150, 158, 176), (118, 124, 142), (60, 66, 90), "disabled"), "Kit", "button_disabled.png", (48, 48, 48, 48))
    out(button((120, 205, 255), (40, 125, 235), (24, 80, 175), NAVY2), "Kit", "button_blue.png", (48, 48, 48, 48))
    out(round_button((120, 205, 255), (40, 125, 235), (24, 80, 175)), "Kit", "round_button_normal.png")
    out(round_button((95, 170, 230), (32, 100, 195), (24, 80, 175), True), "Kit", "round_button_pressed.png")
    f, fl = heightbar()
    out(f, "Kit", "heightbar_frame.png", (28, 28, 28, 28))
    out(fl, "Kit", "heightbar_fill.png", (20, 20, 20, 20))


# ---------------------------------------------------------------- banners (I-03)
def banner(top, bot, lipc, hero=True):
    W, H = 384, 112
    g = Glossy(W, H)
    m = lambda i: g.rrect((6, 6, 378, 102), 48, i)
    img = g.render(m, top, bot, NAVY2, 6, lip=7, lip_col=lipc, shadow=(0, 4, 3, 0.35), gloss=0.5, gloss_h=0.45)
    # badge circle on the leading side (fixed area inside the 9-slice border)
    cx = 54 if hero else W - 54
    b = Glossy(W, H)
    bm = lambda i: b.ellipse((cx - 36, 14, cx + 36, 86), i)
    badge = b.render(bm, (255, 255, 255), lighten(bot, 0.55), NAVY2, 4, shadow=(0, 2, 2, 0.3), gloss=0.0, rim=0.0)
    img = over(img, badge)
    # decorative chevrons on the trailing side
    ov = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    for j in range(3):
        x = (W - 62 + j * 14) if hero else (62 - j * 14)
        t = 1 if hero else -1
        d.polygon([(x, 32), (x + 12 * t, 52), (x, 72), (x - 7 * t, 72), (x + 5 * t, 52), (x - 7 * t, 32)], fill=(255, 255, 255, 120))
    img = over(img, ov)
    return img


def gen_banners():
    out(banner((120, 208, 255), (36, 112, 232), (20, 66, 170), True), "Banners", "banner_hero_blue.png", (64, 64, 48, 48))
    out(banner((255, 170, 90), (225, 55, 40), (150, 30, 30), False), "Banners", "banner_villain_red.png", (64, 64, 48, 48))


# ---------------------------------------------------------------- icons (I-04)
def gen_icons():
    for n in ICONS:
        out(render_icon(n), "Icons", f"icon_{n}.png")


# ---------------------------------------------------------------- logo + app icon (I-05)
def text_layer(txt, font_path, size, N, center, rot=0):
    f = ImageFont.truetype(font_path, size)
    m = Image.new("L", N, 0)
    d = ImageDraw.Draw(m)
    bb = d.textbbox((0, 0), txt, font=f)
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    d.text((center[0] - w / 2 - bb[0], center[1] - h / 2 - bb[1]), txt, font=f, fill=255)
    if rot:
        m = m.rotate(rot, resample=Image.BICUBIC, center=center)
    return m


def gen_logo():
    W, H, ss = 1024, 512, 4
    N = (W * ss, H * ss)
    font = "C:/Windows/Fonts/ariblk.ttf"
    layers = [
        ("GOD", 330 * ss, (W / 2 * ss, 158 * ss), ((255, 240, 90), (255, 150, 20))),
        ("TOWER", 250 * ss, (W / 2 * ss, 380 * ss), ((140, 225, 255), (40, 120, 235))),
    ]
    res = Image.new("RGBA", N, (0, 0, 0, 0))
    allmask = Image.new("L", N, 0)
    masks = []
    for txt, sz, c, cols in layers:
        # shrink to fit width
        while True:
            m = text_layer(txt, font, sz, N, c, rot=0)
            bb = m.getbbox()
            if bb[2] - bb[0] < 940 * ss:
                break
            sz = int(sz * 0.97)
        masks.append((m, cols))
        allmask = ImageChops.lighter(allmask, m)
    # thick dark outer outline, white middle outline
    dark = dilate(allmask, 26 * ss * 0.5)
    white = dilate(allmask, 14 * ss * 0.5)
    shadow = shadowed(dark, N, 0, 14 * ss * 0.5, 8 * ss * 0.5, 0.4)
    res = over(res, shadow)
    res = over(res, fill(dark, NAVY2))
    res = over(res, fill(white, (255, 255, 255)))
    for m, cols in masks:
        bb = m.getbbox()
        grad = vgrad(N[0], N[1], cols[0], cols[1], bb[1], bb[3])
        res = over(res, fill(m, grad))
        # lower dark-edge band for depth
        band = ImageChops.subtract(m, ImageChops.offset(m, 0, -8 * ss))
        res = over(res, fill(scale_alpha(band, 0.35), darken(cols[1], 0.5)))
        # top gloss
        gl = Image.new("L", N, 0)
        ImageDraw.Draw(gl).rectangle([0, bb[1], N[0], bb[1] + (bb[3] - bb[1]) * 0.36], fill=255)
        gl = ImageChops.multiply(gl, erode(m, 6 * ss * 0.5))
        res = over(res, fill(scale_alpha(gl, 0.35), (255, 255, 255)))
    img = res.resize((W, H), Image.LANCZOS)
    # sparkles
    d = ImageDraw.Draw(img)
    out(img, "Logo", "logo_god_tower.png")


def tower_shape(ss, N, cx, w, ytop, ybot):
    pass


def gen_app_icon():
    S = 1024
    ss = 2
    N = S * ss
    # sky gradient
    img = vgrad(N, N, (70, 160, 245), (190, 232, 255)).convert("RGBA")
    # sun burst rays
    rays = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(rays)
    cx, cy = N * 0.5, N * 0.40
    for i in range(14):
        a0 = i * 2 * math.pi / 14
        a1 = a0 + math.pi / 14
        R = N * 3.0
        d.polygon([(cx, cy), (cx + R * math.cos(a0), cy + R * math.sin(a0)), (cx + R * math.cos(a1), cy + R * math.sin(a1))], fill=(255, 255, 255, 38))
    img = over(img, rays)
    # clouds
    for (cxx, cyy, sc) in ((190, 800, 0.9), (840, 860, 1.1), (760, 250, 0.6), (150, 330, 0.5)):
        cl = Image.open(os.path.join(SKY, "cloud_01.png")).convert("RGBA")
        cl = cl.resize((int(N * 0.5 * sc), int(N * 0.5 * sc)), Image.LANCZOS)
        img.alpha_composite(cl, (int(cxx * ss - cl.width / 2), int(cyy * ss - cl.height / 2)))
    # tower: stacked rings
    tw = 230 * ss
    x0, x1 = N // 2 - tw // 2, N // 2 + tw // 2
    top, bot = 250 * ss, N
    tmask = Image.new("L", (N, N), 0)
    ImageDraw.Draw(tmask).rectangle([x0, top, x1, bot], fill=255)
    # ring bands
    rings = Image.new("L", (N, N), 0)
    rd = ImageDraw.Draw(rings)
    ringys = list(range(top + 60 * ss, N, 150 * ss))
    for y in ringys:
        rd.rounded_rectangle([x0 - 16 * ss, y, x1 + 16 * ss, y + 44 * ss], radius=18 * ss, fill=255)
    body = tmask
    outline_all = dilate(ImageChops.lighter(tmask, rings), 12 * ss)
    img = over(img, shadowed(outline_all, (N, N), 14 * ss, 12 * ss, 14 * ss, 0.30))
    img = over(img, fill(outline_all, NAVY2))
    # body shading horizontal gradient
    arr = np.zeros((N, N, 3), np.float32)
    xs = np.linspace(0, 1, N)[None, :, None]
    t = np.clip((xs * N - x0) / (x1 - x0), 0, 1)
    c0 = np.array([236, 244, 255], np.float32)
    c1 = np.array([140, 170, 215], np.float32)
    arr[:] = c0 + (c1 - c0) * (t ** 1.4)
    body_img = Image.fromarray(arr.astype(np.uint8), "RGB").convert("RGBA")
    img = over(img, fill(tmask, body_img))
    rarr = np.zeros((N, N, 3), np.float32)
    r0 = np.array([255, 255, 255], np.float32)
    r1 = np.array([170, 195, 235], np.float32)
    t2 = np.clip((xs * N - (x0 - 16 * ss)) / (x1 - x0 + 32 * ss), 0, 1)
    rarr[:] = r0 + (r1 - r0) * (t2 ** 1.2)
    img = over(img, fill(rings, Image.fromarray(rarr.astype(np.uint8), "RGB").convert("RGBA")))
    # windows
    dd = ImageDraw.Draw(img)
    for y in ringys:
        for dy in (70 * ss,):
            wy = y + dy
            for wx in (N // 2 - 62 * ss, N // 2 + 22 * ss):
                if 90 * ss < wy < N - 40 * ss:
                    dd.rounded_rectangle([wx, wy, wx + 40 * ss, wy + 56 * ss], radius=8 * ss, fill=(255, 215, 90), outline=NAVY2, width=5 * ss)
    # golden top: flag / star
    star = render_icon("star", out=300 * ss, ss=2)
    img.alpha_composite(star, (N // 2 - star.width // 2, top - 215 * ss))
    # boxing glove punching
    gl = Canvas(520 * ss // 2, 2)
    glove(gl)
    gl = gl.finish(ow=7)
    gl = gl.resize((560 * ss, 560 * ss), Image.LANCZOS)
    gl = gl.transpose(Image.FLIP_LEFT_RIGHT)
    img.alpha_composite(gl, (N // 2 - 80 * ss, 470 * ss))
    final = img.convert("RGB").resize((S, S), Image.LANCZOS)
    p = os.path.join(UI, "Logo", "app_icon_1024.png")
    save(final, p)
    MANIFEST.append((os.path.relpath(p, ROOT).replace("\\", "/"), final.size, None))


def contact_sheets():
    pv = os.path.join(ROOT, "Art", "Source", "UI", "Previews")
    import glob
    preview.sheet(sorted(glob.glob(os.path.join(UI, "Icons", "*.png"))), os.path.join(pv, "icons.png"), 256, 5)
    preview.sheet(sorted(glob.glob(os.path.join(SKY, "*.png"))), os.path.join(pv, "clouds.png"), 512, 2)
    preview.sheet(sorted(glob.glob(os.path.join(UI, "Kit", "*.png"))), os.path.join(pv, "kit.png"), 300, 4, bg=(120, 190, 240))
    preview.sheet(sorted(glob.glob(os.path.join(UI, "Banners", "*.png"))), os.path.join(pv, "banners.png"), 400, 2)
    preview.sheet(sorted(glob.glob(os.path.join(UI, "Logo", "*.png"))), os.path.join(pv, "logo.png"), 700, 2)


if __name__ == "__main__":
    only = sys.argv[1:] or ["clouds", "kit", "banners", "icons", "logo", "app"]
    if "clouds" in only: gen_clouds()
    if "kit" in only: gen_kit()
    if "banners" in only: gen_banners()
    if "icons" in only: gen_icons()
    if "logo" in only: gen_logo()
    if "app" in only: gen_app_icon()
    for r in MANIFEST:
        print(r)
    contact_sheets()
