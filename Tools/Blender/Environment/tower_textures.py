"""Generate the tower stone atlas (albedo + tangent-space normal map, OpenGL/Unity convention: +Y = +V).

Run: uv run --with numpy --with scipy --with pillow python Tools/Blender/Environment/tower_textures.py
Output: Assets/Art/Environment/Tower/Textures/T_TowerStone_{Albedo,Normal}.png (1024^2, 2x2 tiles, one per segment)
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tower_spec as spec  # noqa: E402

OUT = "D:/Unity/Projects/GodTower/Assets/Art/Environment/Tower/Textures"
ATLAS = 1024
TILE = ATLAS // 2          # final tile size
SS = 2                     # supersampling
N = TILE * SS              # working tile resolution
W = spec.TILE_W            # metres across a tile
HM = spec.H                # metres up a tile
DX = W / N
DZ = HM / N

STONE = np.array([176, 187, 162], dtype=np.float32) / 255.0   # pale grey-green (sRGB)

# physical coordinates of pixel centres (row 0 = top = z H)
X = (np.arange(N) + 0.5) * DX
Z = HM - (np.arange(N) + 0.5) * DZ
XX, ZZ = np.meshgrid(X, Z)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def ridge(d, w):
    """Rounded ridge profile from a distance field (0 at centre line)."""
    t = np.clip(1.0 - (np.abs(d) / w) ** 2, 0.0, 1.0)
    return np.sqrt(t)


def wrapdx(x, cx):
    return (x - cx + W / 2) % W - W / 2


def band_mask(z0, z1, pad=0.035):
    return smoothstep(z0 + pad * 0.3, z0 + pad, ZZ) * (1 - smoothstep(z1 - pad, z1 - pad * 0.3, ZZ))


def local_v(z0, z1):
    return (ZZ - z0) / (z1 - z0)


# ------------------------------------------------------------- motif painters (return height in metres)

def p_bosses(z0, z1, count=6, scale=1.0):
    zc = (z0 + z1) / 2
    hb = (z1 - z0)
    rad = min(hb * 0.36, W / count * 0.40) * scale
    h = np.zeros_like(XX)
    for k in range(count):
        cx = (k + 0.5) * W / count
        d = np.sqrt(wrapdx(XX, cx) ** 2 + (ZZ - zc) ** 2)
        outer = ridge(d - rad * 0.82, rad * 0.18) * 0.022
        dome = np.sqrt(np.clip(1 - (d / (rad * 0.48)) ** 2, 0, 1)) * 0.035
        h = np.maximum(h, np.maximum(outer, dome))
    return h


def p_crenels(z0, z1, count=8):
    """Notched collar: raised blocks with rounded edges."""
    period = W / count
    lx = (XX % period) / period
    m = (np.abs(lx - 0.5) < 0.30) & (ZZ > z0 + 0.05) & (ZZ < z1 - 0.05)
    return bevel(m, 0.025) * 0.04


def p_grooves(z0, z1, n=3):
    h = np.zeros_like(XX)
    for i in range(n):
        zc = z0 + (i + 1) * (z1 - z0) / (n + 1)
        h -= ridge(ZZ - zc, 0.03) * 0.02
    return h


def zigzag_line(z0, z1, period, amp_frac, offset_frac, invert=False):
    t = (XX % period) / period
    tri = np.abs(2 * t - 1)
    if invert:
        tri = 1 - tri
    zl = z0 + (offset_frac + tri * amp_frac) * (z1 - z0)
    return ZZ - zl


def p_zigwave(z0, z1):
    zm = z0 + (z1 - z0) * 0.40
    h = np.zeros_like(XX)
    period = W / 4
    # upper: three nested zig-zag ridges
    for k in range(3):
        d = zigzag_line(zm, z1, period, 0.55, 0.12 + k * 0.14)
        h = np.maximum(h, ridge(d, 0.028) * 0.03)
    # triangles filled at top
    t = (XX % period) / period
    tri = np.abs(2 * t - 1)
    top = ZZ > (zm + (0.12 + 3 * 0.14 + tri * 0.55) * (z1 - zm) + 0.02)
    h = np.maximum(h, bevel(top & (ZZ < z1 - 0.04), 0.02) * 0.03)
    # lower: two wavy ridges
    for k in range(2):
        zc = z0 + (0.30 + k * 0.38) * (zm - z0)
        d = ZZ - (zc + 0.05 * np.sin(2 * math.pi * XX / (W / 6)))
        h = np.maximum(h, ridge(d, 0.03) * 0.028)
    return h


def p_chevrons(z0, z1):
    h = np.zeros_like(XX)
    period = W / 8
    for k in range(2):
        d = zigzag_line(z0, z1, period, 0.45, 0.15 + k * 0.25, invert=True)
        h = np.maximum(h, ridge(d, 0.03) * 0.03)
    return h


def p_knurl(z0, z1):
    p = W / 24
    a = np.abs(np.sin(math.pi * (XX + ZZ) / p))
    b = np.abs(np.sin(math.pi * (XX - ZZ) / p))
    return np.minimum(a, b) ** 0.6 * 0.03


def bevel(mask, width):
    """Chunky rounded bevel from a boolean mask using a physical distance transform."""
    if not mask.any():
        return np.zeros(mask.shape, np.float32)
    d = ndimage.distance_transform_edt(mask, sampling=(DZ, DX))
    t = np.clip(d / width, 0, 1)
    return np.sqrt(1 - (1 - t) ** 2)


# ---- original glyph library (local coords: x right, y up, roughly within [-1, 1])

def g_sun(dr):
    dr.circle((0, 0), 0.42)
    for k in range(8):
        a = k * math.pi / 4
        dr.poly([(math.cos(a - 0.17) * 0.55, math.sin(a - 0.17) * 0.55),
                 (math.cos(a) * 0.95, math.sin(a) * 0.95),
                 (math.cos(a + 0.17) * 0.55, math.sin(a + 0.17) * 0.55)])
    dr.hole_circle((0, 0), 0.18)


def g_spiral(dr):
    pts = []
    for i in range(90):
        t = i / 89
        a = t * 4.2 * math.pi
        r = 0.08 + 0.78 * t
        pts.append((math.cos(a) * r, math.sin(a) * r))
    dr.line(pts, 0.17)


def g_fish(dr):
    body = [(math.cos(a) * 0.62 - 0.12, math.sin(a) * 0.36) for a in np.linspace(0, 2 * math.pi, 40)]
    dr.poly(body)
    dr.poly([(0.38, 0.0), (0.92, 0.45), (0.82, 0.0), (0.92, -0.45)])
    dr.hole_circle((-0.45, 0.08), 0.09)
    dr.hole_line([(-0.05, 0.3), (0.05, 0.0), (-0.05, -0.3)], 0.06)


def g_bird(dr):
    dr.poly([(-0.9, 0.35), (-0.3, 0.05), (0.0, 0.25), (0.3, 0.05), (0.9, 0.35), (0.35, -0.15),
             (0.12, -0.2), (0.0, -0.75), (-0.12, -0.2), (-0.35, -0.15)])
    dr.circle((0, 0.32), 0.17)


def g_mask(dr):
    dr.rounded_rect(-0.55, -0.7, 0.55, 0.55, 0.22)
    dr.line([(-0.5, 0.45), (-0.85, 0.7), (-0.8, 0.95)], 0.14)
    dr.line([(0.5, 0.45), (0.85, 0.7), (0.8, 0.95)], 0.14)
    dr.hole_circle((-0.24, 0.12), 0.13)
    dr.hole_circle((0.24, 0.12), 0.13)
    dr.hole_line([(-0.25, -0.35), (0.0, -0.45), (0.25, -0.35)], 0.07)


def g_tree(dr):
    dr.line([(0, -0.9), (0, 0.85)], 0.16)
    for k, y in enumerate((-0.35, 0.05, 0.42)):
        s = 0.75 - k * 0.18
        dr.line([(0, y), (-s, y + 0.35)], 0.13)
        dr.line([(0, y), (s, y + 0.35)], 0.13)


def g_wave_eye(dr):
    dr.poly([(math.cos(a) * 0.85, math.sin(a) * 0.45) for a in np.linspace(0, 2 * math.pi, 40)])
    dr.hole_circle((0, 0), 0.26)
    dr.circle((0, 0), 0.12)


class GlyphCanvas:
    """Draws glyph primitives into a supersampled mask in physical units."""

    def __init__(self):
        self.img = Image.new("L", (N, N), 0)
        self.d = ImageDraw.Draw(self.img)
        self.cx = self.cz = 0.0
        self.s = 1.0

    def at(self, cx, cz, size):
        self.cx, self.cz, self.s = cx, cz, size
        return self

    def _p(self, x, y):
        X_ = self.cx + x * self.s
        Z_ = self.cz + y * self.s
        return (X_ / DX, (HM - Z_) / DZ)

    def _rpx(self, r):
        return (r * self.s / DX, r * self.s / DZ)

    def circle(self, c, r, fill=255):
        px, py = self._p(*c)
        rx, ry = self._rpx(r)
        self.d.ellipse([px - rx, py - ry, px + rx, py + ry], fill=fill)

    def hole_circle(self, c, r):
        self.circle(c, r, fill=0)

    def poly(self, pts, fill=255):
        self.d.polygon([self._p(*p) for p in pts], fill=fill)

    def line(self, pts, w, fill=255):
        P = [self._p(*p) for p in pts]
        wpx = w * self.s / ((DX + DZ) / 2)
        self.d.line(P, fill=fill, width=max(1, int(wpx)), joint="curve")
        for p in pts:
            self.circle(p, w / 2, fill=fill)

    def hole_line(self, pts, w):
        self.line(pts, w, fill=0)

    def rounded_rect(self, x0, y0, x1, y1, r):
        a = self._p(x0, y1)
        b = self._p(x1, y0)
        self.d.rounded_rectangle([a[0], a[1], b[0], b[1]], radius=r * self.s / DX, fill=255)

    def mask(self):
        return np.asarray(self.img) > 127


def p_glyphs(z0, z1, glyphs):
    cv = GlyphCanvas()
    zc = (z0 + z1) / 2
    size = min((z1 - z0) * 0.44, W / len(glyphs) * 0.42)
    for k, g in enumerate(glyphs):
        cx = (k + 0.5) * W / len(glyphs)
        g(cv.at(cx, zc, size))
    return bevel(cv.mask(), 0.03) * 0.04


PAINTERS = {
    "bosses": lambda z0, z1: p_bosses(z0, z1, 6),
    "bosses_small": lambda z0, z1: p_bosses(z0, z1, 8, 0.9),
    "crenels": p_crenels,
    "grooves": p_grooves,
    "zigwave": p_zigwave,
    "chevrons": p_chevrons,
    "knurl": p_knurl,
    "glyphs_a": lambda z0, z1: p_glyphs(z0, z1, [g_sun, g_fish, g_spiral, g_bird]),
    "glyphs_b": lambda z0, z1: p_glyphs(z0, z1, [g_mask, g_tree, g_wave_eye, g_mask, g_tree, g_wave_eye][:4]),
}


# ------------------------------------------------------------- composition

def periodic_noise(sigma_m, seed):
    rng = np.random.default_rng(seed)
    n = rng.standard_normal((N, N)).astype(np.float32)
    n = ndimage.gaussian_filter(n, (sigma_m / DZ, sigma_m / DX), mode="wrap")
    return n / (n.std() + 1e-6)


def build_tile(name, seed):
    profile, bands, flutes, creases = spec.build(name)
    h = np.zeros((N, N), np.float32)
    for z0, z1, kind in bands:
        hb = PAINTERS[kind](z0, z1) * band_mask(z0, z1)
        sel = (ZZ >= z0) & (ZZ <= z1)
        h = np.where(sel, hb, h)
    # normal map
    gz, gx = np.gradient(h, DZ, DX)          # gz along rows (downwards = -Z)
    dhdx = gx
    dhdz = -gz
    nx, ny, nz = -dhdx, -dhdz, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / ln, ny / ln, nz / ln], -1)
    # cavity (ambient-occlusion-like) darkening from height
    blur = ndimage.gaussian_filter(h, (0.04 / DZ, 0.04 / DX), mode="wrap")
    cav = np.clip((blur - h) / 0.012, 0, 1)
    top = np.clip((h - blur) / 0.012, 0, 1)
    shade = 1.0 - 0.38 * cav + 0.06 * top
    # creases at ring bases / recess edges
    for zc in creases:
        shade -= 0.22 * np.exp(-((ZZ - zc) / 0.012) ** 2)
    # flute region: subtle vertical stripes so flutes read even on low-mip
    for z0, z1, ribs in flutes:
        sel = (ZZ > z0) & (ZZ < z1)
        per = W / (ribs / spec.SECTORS)
        stripe = 0.5 + 0.5 * np.cos(2 * math.pi * XX / per)
        shade = np.where(sel, shade - 0.0 * stripe, shade)
    noise = periodic_noise(0.4, seed) * 0.012 + periodic_noise(0.04, seed + 1) * 0.008
    col = STONE[None, None, :] * (shade + noise)[..., None]
    col = np.clip(col, 0, 1)
    return col, nrm


def down(a):
    return a.reshape(TILE, SS, TILE, SS, -1).mean(axis=(1, 3))


def main():
    os.makedirs(OUT, exist_ok=True)
    alb = np.zeros((ATLAS, ATLAS, 3), np.float32)
    nrm = np.zeros((ATLAS, ATLAS, 3), np.float32)
    for i, (name, (u0, v0)) in enumerate(spec.ATLAS_TILES.items()):
        c, n = build_tile(name, 100 + i)
        c, n = down(c), down(n)
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        x0 = int(u0 * ATLAS)
        y0 = int((1.0 - v0 - 0.5) * ATLAS)
        alb[y0:y0 + TILE, x0:x0 + TILE] = c
        nrm[y0:y0 + TILE, x0:x0 + TILE] = n * 0.5 + 0.5
        print("tile", name, "done")
    Image.fromarray((alb * 255 + 0.5).astype(np.uint8)).save(f"{OUT}/T_TowerStone_Albedo.png")
    Image.fromarray((np.clip(nrm, 0, 1) * 255 + 0.5).astype(np.uint8)).save(f"{OUT}/T_TowerStone_Normal.png")
    print("written", OUT)


if __name__ == "__main__":
    main()
