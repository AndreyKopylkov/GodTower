"""Shared constants for the God Tower hero: palette, atlas layout and body dimensions.

Imported by make_atlas.py (Pillow, run via uv) and build_hero.py (Blender Python).
All lengths are metres, Blender axes (Z up, character faces -Y, character's left = +X).
"""

ATLAS_SIZE = 1024

# Flat-colour swatches: 8 columns x 2 rows of 128 px at the top of the atlas.
SWATCH = 128
PALETTE = {
    "orange":      (244, 122, 28),   # gi top + trousers
    "orange_dark": (196, 84, 16),    # gi lapel trim
    "navy":        (34, 62, 140),    # undershirt, belt, wristbands
    "navy_dark":   (26, 36, 82),     # boots
    "sole":        (236, 226, 204),  # boot soles
    "skin":        (247, 200, 156),
    "hair":        (30, 30, 38),
    "white":       (250, 250, 250),
    "black":       (22, 22, 28),
    "mouth":       (150, 62, 48),
    "gold":        (250, 196, 54),   # back emblem tower
    "blush":       (246, 160, 140),
}
SWATCH_ORDER = list(PALETTE.keys())


def swatch_rect(name):
    """Pixel rect (x0, y0, x1, y1), y down, of a flat-colour swatch."""
    i = SWATCH_ORDER.index(name)
    x0 = (i % 8) * SWATCH
    y0 = (i // 8) * SWATCH
    return x0, y0, x0 + SWATCH, y0 + SWATCH


# Painted regions (pixel rects, y down). Planar projections map into them.
REGIONS = {
    "face":        (0, 256, 384, 640),
    "torso_front": (384, 256, 768, 640),
    "torso_back":  (0, 640, 384, 1024),
}


def px_to_uv(x, y):
    return x / ATLAS_SIZE, 1.0 - y / ATLAS_SIZE


# ---------------------------------------------------------------- body dimensions
# Projection boxes (x0, x1, z0, z1) for painted regions, in metres (pre-scale model space).
HEAD = dict(cx=0.0, cy=-0.005, cz=1.61, w=0.44, d=0.40, h=0.38)
# The face is painted in this "design" head space; the planar UV projection is normalised,
# so features keep their relative placement on the (bigger) real head.
FACE_DESIGN_HEAD = dict(cx=0.0, cy=-0.005, cz=1.585, w=0.38, d=0.34, h=0.33)
_D = FACE_DESIGN_HEAD
FACE_DESIGN_PROJ = (_D["cx"] - _D["w"] / 2, _D["cx"] + _D["w"] / 2, _D["cz"] - _D["h"] / 2, _D["cz"] + _D["h"] / 2)
TORSO = dict(x0=-0.28, x1=0.28, y0=-0.15, y1=0.15, z0=0.92, z1=1.39)

FACE_PROJ = (HEAD["cx"] - HEAD["w"] / 2, HEAD["cx"] + HEAD["w"] / 2,
             HEAD["cz"] - HEAD["h"] / 2, HEAD["cz"] + HEAD["h"] / 2)
TORSO_PROJ = (TORSO["x0"], TORSO["x1"], TORSO["z0"], TORSO["z1"])
