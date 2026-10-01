"""Top platform texture (albedo + normal, 1024^2).

Layout (UV space):
  deck   : square u[0,0.70] x v[0.30,1.0]   planar top-down projection, radius DECK_UV_R metres -> square half size
  sides  : strip  u[0.72,1.0] x v[0,1]       plain stone (lathe sides, arc-length v)
Run: uv run --with numpy --with scipy --with pillow python Tools/Blender/Environment/platform_textures.py
"""
import math
import os

import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tower_spec as spec  # noqa: E402

OUT = "D:/Unity/Projects/GodTower/Assets/Art/Environment/Tower/Textures"
S = 1024
SS = 2
N = S * SS
DECK_UV_R = 6.6   # metres from centre to the square edge

STONE = np.array([176, 187, 162], np.float32) / 255
STONE_D = np.array([150, 163, 140], np.float32) / 255
SAND = np.array([206, 196, 160], np.float32) / 255
TEAL = np.array([128, 168, 158], np.float32) / 255


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def groove(d, w):
    return -np.sqrt(np.clip(1 - (d / w) ** 2, 0, 1))


def main():
    os.makedirs(OUT, exist_ok=True)
    px = 1.0 / N
    u = (np.arange(N) + 0.5) * px
    v = 1.0 - (np.arange(N) + 0.5) * px
    UU, VV = np.meshgrid(u, v)
    col = np.broadcast_to(STONE, (N, N, 3)).copy()
    h = np.zeros((N, N), np.float32)

    # ---- deck (planar)
    du0, du1, dv0, dv1 = 0.0, 0.70, 0.30, 1.0
    cx, cy = (du0 + du1) / 2, (dv0 + dv1) / 2
    half = (du1 - du0) / 2
    x = (UU - cx) / half * DECK_UV_R      # metres
    y = (VV - cy) / half * DECK_UV_R
    r = np.sqrt(x * x + y * y)
    a = np.arctan2(y, x)
    deck = (UU <= du1) & (VV >= dv0)

    m_w = 0.035  # groove half width (m)
    hd = np.zeros_like(h)
    cd = np.broadcast_to(STONE, (N, N, 3)).copy()
    # concentric grooves
    for rr in (0.55, 1.6, 1.75, 3.6, 3.75, 5.35):
        hd = np.minimum(hd, groove(r - rr, m_w) * 0.02)
    # radial wedge tiles between 1.75 and 3.6 (16), alternating tint
    wedge = np.floor((a + math.pi) / (2 * math.pi) * 16).astype(int)
    ring1 = (r > 1.75) & (r < 3.6)
    alt = (wedge % 2 == 0) & ring1
    cd[alt] = STONE_D
    wa = ((a + math.pi) / (2 * math.pi) * 16) % 1.0
    dwa = np.minimum(wa, 1 - wa) * (2 * math.pi / 16) * r
    hd = np.where(ring1, np.minimum(hd, groove(dwa, m_w) * 0.02), hd)
    # zig-zag mosaic ring between 3.75 and 5.35 (32 teeth), sand vs stone
    t = ((a + math.pi) / (2 * math.pi) * 32) % 1.0
    tri = np.abs(2 * t - 1)
    ring2 = (r > 3.75) & (r < 5.35)
    zz = 3.95 + tri * 1.2
    cd[ring2 & (r < zz)] = SAND
    hd = np.where(ring2, np.minimum(hd, groove(r - zz, m_w) * 0.02), hd)
    # inner ring 0.55..1.6: 8-point star emblem (original) in teal on sand
    inner = (r > 0.55) & (r < 1.6)
    cd[inner] = SAND
    star_r = 0.75 + 0.75 * (np.abs(np.cos(4 * a)) ** 3)
    star = inner & (r < star_r)
    cd[star] = TEAL
    sd = ndimage.distance_transform_edt(star, sampling=(DECK_UV_R / half * px,) * 2)
    hd = np.where(star, np.sqrt(1 - (1 - np.clip(sd / 0.06, 0, 1)) ** 2) * 0.03, hd)
    # centre disc: raised boss
    centre = r < 0.55
    cd[centre] = STONE
    hd = np.where(centre, np.sqrt(np.clip(1 - (r / 0.5) ** 2, 0, 1)) * 0.04, hd)
    # outer border beyond 5.35: plain stone with a darker band near the rim wall
    cd[(r > 5.35) & (r < 5.95)] = STONE_D
    col = np.where(deck[..., None], cd, col)
    h = np.where(deck, hd, h)

    # ---- noise + cavity shading
    rng = np.random.default_rng(7)
    n = ndimage.gaussian_filter(rng.standard_normal((N, N)).astype(np.float32), 18, mode="wrap")
    n /= n.std()
    n2 = ndimage.gaussian_filter(rng.standard_normal((N, N)).astype(np.float32), 3, mode="wrap")
    n2 /= n2.std()
    blur = ndimage.gaussian_filter(h, 10)
    cav = np.clip((blur - h) / 0.01, 0, 1)
    shade = 1 - 0.35 * cav + np.where(deck, 0.010 * n + 0.006 * n2, 0.0)
    # sides strip: one flute per UV island -> darken valleys (strip edges) on ribbed spans, crease lines elsewhere
    side = UU >= 0.72
    frac = np.clip((UU - 0.72) / 0.28, 0, 1)
    edge = np.minimum(frac, 1 - frac)
    flute = -0.30 * np.exp(-(edge / 0.10) ** 2) + 0.06 * np.exp(-((frac - 0.5) / 0.2) ** 2)
    ranges, _acc = spec.top_rib_vranges()
    ribbed = np.zeros_like(UU, dtype=bool)
    for v0, v1 in ranges:
        ribbed |= (VV >= v0) & (VV <= v1)
    shade = np.where(side & ribbed, shade + flute, shade)
    col = np.clip(col * shade[..., None], 0, 1)

    # ---- normal map (metres per pixel in deck space)
    mpp = DECK_UV_R / half * px
    gy, gx = np.gradient(h, mpp, mpp)
    nx, ny = -gx, gy          # rows go down = -v
    nz = np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / ln, ny / ln, nz / ln], -1)

    def down(img):
        return img.reshape(S, SS, S, SS, -1).mean(axis=(1, 3))

    col = down(col)
    nrm = down(nrm)
    nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
    Image.fromarray((col * 255 + 0.5).astype(np.uint8)).save(f"{OUT}/T_TowerTop_Albedo.png")
    Image.fromarray(((nrm * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)).save(f"{OUT}/T_TowerTop_Normal.png")
    print("written")


if __name__ == "__main__":
    main()
