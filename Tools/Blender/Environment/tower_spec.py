"""Tower segment specification shared by the texture generator (uv python) and the Blender builder.

Scale contract: column radius R = 1.5 m, every segment exactly 3.0 m tall. Each segment starts and ends with a
plain 6 cm cylinder at radius R, so any segment stacks on any other with identical vertices/normals at the joint.

Element list (bottom -> top):
  ("plain", h)                     cylinder at R
  ("rings", count, h_each, dr)     rounded bead rings sticking out by dr
  ("ledge", h, r_out)              thin wide disc plate
  ("band", h, kind, recess)        cylinder band at R - recess, carved via the texture `kind`
  ("flutes", h, ribs)              vertical flutes (real geometry, separate tube inside a recess)
"""
import math


R = 1.5
H = 3.0
SIDES = 40          # radial resolution of the column
SECTORS = 4         # texture repeats around the column (UV islands)
TILE_W = 2.0 * 3.141592653589793 * R / SECTORS   # physical width of one texture repeat (2.356 m)

# Atlas: 2x2 tiles in one 1024^2 texture, one tile per segment. (u0, v0) = bottom-left of tile in UV space.
ATLAS_TILES = {
    "Rings":  (0.0, 0.5),
    "Zigzag": (0.5, 0.5),
    "Relief": (0.0, 0.0),
    "Ribbed": (0.5, 0.0),
}

SEGMENTS = {
    "Rings": [
        ("plain", 0.06),
        ("rings", 3, 0.10, 0.085),
        ("band", 0.56, "bosses", 0.0),
        ("rings", 2, 0.10, 0.075),
        ("ledge", 0.12, 1.80),
        ("band", 0.36, "crenels", 0.0),
        ("rings", 1, 0.12, 0.095),
        ("band", 0.50, "bosses_small", 0.03),
        ("rings", 3, 0.10, 0.085),
        ("band", 0.42, "grooves", 0.0),
        ("plain", 0.06),
    ],
    "Zigzag": [
        ("plain", 0.06),
        ("rings", 1, 0.12, 0.095),
        ("band", 1.00, "zigwave", 0.03),
        ("rings", 2, 0.10, 0.075),
        ("ledge", 0.12, 1.80),
        ("rings", 1, 0.10, 0.075),
        ("band", 0.50, "chevrons", 0.0),
        ("rings", 3, 0.10, 0.085),
        ("band", 0.54, "bosses", 0.0),
        ("plain", 0.06),
    ],
    "Relief": [
        ("plain", 0.06),
        ("rings", 2, 0.10, 0.075),
        ("band", 0.84, "glyphs_a", 0.04),
        ("rings", 1, 0.12, 0.095),
        ("ledge", 0.12, 1.80),
        ("rings", 2, 0.10, 0.075),
        ("band", 0.70, "glyphs_b", 0.04),
        ("rings", 3, 0.10, 0.085),
        ("band", 0.40, "chevrons", 0.0),
        ("plain", 0.06),
    ],
    "Ribbed": [
        ("plain", 0.06),
        ("rings", 2, 0.10, 0.075),
        ("flutes", 0.90, 40),
        ("rings", 1, 0.12, 0.095),
        ("band", 0.50, "knurl", 0.02),
        ("rings", 2, 0.10, 0.075),
        ("ledge", 0.12, 1.80),
        ("band", 0.34, "crenels", 0.0),
        ("rings", 1, 0.10, 0.075),
        ("band", 0.40, "bosses_small", 0.0),
        ("plain", 0.06),
    ],
}

FLUTE_INNER = 1.40   # recess radius behind flutes
FLUTE_VALLEY = 1.43
FLUTE_TOP = 1.50


def build(name):
    """Return (profile, bands, flutes, creases).

    profile: list of (z, r) bottom->top, collinear points removed.
    bands:   list of (z0, z1, kind) texture regions.
    flutes:  list of (z0, z1, ribs).
    creases: list of z values of concave ring/band junctions (for baked darkening).
    """
    pts = [(0.0, R)]
    bands, flutes, creases = [], [], []
    z = 0.0

    def add(p):
        if abs(p[0] - pts[-1][0]) > 1e-6 or abs(p[1] - pts[-1][1]) > 1e-6:
            pts.append((round(p[0], 5), round(p[1], 5)))

    for el in SEGMENTS[name]:
        kind = el[0]
        if kind == "plain":
            z += el[1]
            add((z, R))
        elif kind == "rings":
            _, count, h, dr = el
            for _i in range(count):
                creases.append(z)
                add((z, R))
                add((z + h * 0.5, R + dr))
                z += h
                add((z, R))
            creases.append(z)
        elif kind == "ledge":
            _, h, ro = el
            add((z, R))
            add((z, ro - 0.025))
            add((z + 0.025, ro))
            add((z + h - 0.025, ro))
            add((z + h, ro - 0.025))
            z += h
            add((z, R))
        elif kind == "band":
            _, h, tex, rec = el
            add((z, R))
            if rec > 0:
                add((z, R - rec))
                add((z + h, R - rec))
                creases.append(z + 0.015)
                creases.append(z + h - 0.015)
            bands.append((z, z + h, tex))
            z += h
            add((z, R))
        elif kind == "flutes":
            _, h, ribs = el
            add((z, R))
            add((z, FLUTE_INNER))
            add((z + h, FLUTE_INNER))
            flutes.append((z, z + h, ribs))
            creases.append(z + 0.01)
            creases.append(z + h - 0.01)
            z += h
            add((z, R))
    assert abs(z - H) < 1e-6, f"segment {name} height {z} != {H}"
    # drop collinear points on vertical runs at the same radius
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        a, b, c = out[-1], pts[i], pts[i + 1]
        if abs(a[1] - b[1]) < 1e-6 and abs(b[1] - c[1]) < 1e-6:
            continue
        out.append(b)
    out.append(pts[-1])
    return out, bands, flutes, creases


# ---------------------------------------------------------------- top platform (B-03)

TOP_SIDES = 80
TOP_DECK_Z = 3.40
TOP_DECK_R = 6.0


def top_profile():
    """Top platform lathe profile: list of (z, r, rib_amplitude). Joint at z=0 equals segment ends."""
    P = []

    def add(z, r, rib=0.0):
        P.append((z, r, rib))
    add(0.0, R)
    add(0.06, R)
    # two bead rings
    add(0.11, R + 0.075); add(0.16, R)
    add(0.21, R + 0.075); add(0.26, R)
    # fluted collar (ribs push outwards)
    add(0.26, R - 0.08, 0.08)
    add(0.95, R - 0.08, 0.08)
    add(0.95, R)
    # big ring
    add(1.03, R + 0.12); add(1.11, R)
    # ledge plate
    add(1.11, 1.92); add(1.14, 1.97); add(1.22, 1.97); add(1.25, 1.92); add(1.25, 1.62)
    # bell flare (fluted)
    for i, t in enumerate((0.0, 0.18, 0.38, 0.6, 0.82, 1.0)):
        a = t * math.pi / 2
        z = 1.25 + 1.05 * (1 - math.cos(a))
        r = 1.62 + 2.1 * math.sin(a)
        add(z, r, 0.14 if 0 < i < 5 else 0.0)
    # step to saucer
    add(2.32, 3.95)
    # saucer underside (shallow, ribbed)
    add(2.45, 4.6, 0.10)
    add(2.62, 5.3, 0.08)
    add(2.82, 5.95, 0.04)
    add(2.96, 6.35)
    # rim
    add(3.10, 6.55)
    add(3.45, 6.55)
    add(3.62, 6.45)
    add(3.66, 6.25)
    add(3.62, 6.08)
    add(TOP_DECK_Z, 6.0)
    # deck (flat) to centre
    add(TOP_DECK_Z, 4.0)
    add(TOP_DECK_Z, 2.0)
    add(TOP_DECK_Z, 0.0)
    return P


def top_rib_vranges():
    """Arc-length v ranges (0..1) of ribbed spans in the top profile (for texture flute shading)."""
    P = top_profile()
    acc = [0.0]
    for i in range(1, len(P)):
        acc.append(acc[-1] + math.hypot(P[i][0] - P[i - 1][0], P[i][1] - P[i - 1][1]))
    out = []
    for i in range(len(P) - 1):
        if P[i][2] > 0 and P[i + 1][2] > 0:
            out.append((acc[i] / acc[-1], acc[i + 1] / acc[-1]))
    return out, acc
