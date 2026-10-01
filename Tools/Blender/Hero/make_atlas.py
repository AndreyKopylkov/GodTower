"""Paint the hero's 1024x1024 albedo atlas (flat colour swatches + face / gi front / gi back regions).

Run from the project root:
    uv run --with pillow python Tools/Blender/Hero/make_atlas.py
Output: Art/Source/Character/Hero_Albedo.png (copied to Assets by build_hero.py).
No lighting is baked in: everything is flat colour.
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hero_layout as L  # noqa: E402

SS = 4  # supersampling factor
OUT = "Art/Source/Character/Hero_Albedo.png"

img = Image.new("RGB", (L.ATLAS_SIZE * SS, L.ATLAS_SIZE * SS), L.PALETTE["skin"])
d = ImageDraw.Draw(img)
P = L.PALETTE

# Flat swatches
for name in L.SWATCH_ORDER:
    x0, y0, x1, y1 = L.swatch_rect(name)
    d.rectangle([x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1], fill=P[name])


class Region:
    """Draw in metric units of a planar projection (u: x0..x1 left->right, v: z1 (top) .. z0 (bottom))."""

    def __init__(self, rect, proj, mirror_u=False):
        # Each region is painted on its own canvas and pasted, so strokes are clipped to the region.
        self.ox, self.oy = rect[0] * SS, rect[1] * SS
        self.px0, self.py0 = 0, 0
        self.px1, self.py1 = (rect[2] - rect[0]) * SS, (rect[3] - rect[1]) * SS
        self.canvas = Image.new("RGB", (self.px1, self.py1))
        self.d = ImageDraw.Draw(self.canvas)
        self.x0, self.x1, self.z0, self.z1 = proj
        self.mirror_u = mirror_u

    def paste(self):
        img.paste(self.canvas, (self.ox, self.oy))

    def p(self, x, z):
        fu = (x - self.x0) / (self.x1 - self.x0)
        if self.mirror_u:
            fu = 1.0 - fu
        fv = (self.z1 - z) / (self.z1 - self.z0)
        return (self.px0 + fu * (self.px1 - self.px0), self.py0 + fv * (self.py1 - self.py0))

    def ellipse(self, cx, cz, rx, rz, **kw):
        a = self.p(cx - rx, cz + rz)
        b = self.p(cx + rx, cz - rz)
        self.d.ellipse([min(a[0], b[0]), min(a[1], b[1]), max(a[0], b[0]), max(a[1], b[1])], **kw)

    def poly(self, pts, **kw):
        self.d.polygon([self.p(x, z) for x, z in pts], **kw)

    def line(self, pts, width_m, **kw):
        w = int(width_m / (self.x1 - self.x0) * (self.px1 - self.px0))
        self.d.line([self.p(x, z) for x, z in pts], width=w, joint="curve", **kw)

    def fill(self, col):
        self.d.rectangle([self.px0, self.py0, self.px1 - 1, self.py1 - 1], fill=col)


# ------------------------------------------------------------------ face
# Looking at the character from the front: character's right (-X) is on the image left.
face = Region(L.REGIONS["face"], L.FACE_DESIGN_PROJ)
face.fill(P["skin"])
H = L.FACE_DESIGN_HEAD
cz = H["cz"]
for side in (-1, 1):
    ex = side * 0.068
    ez = cz + 0.005
    # blush
    face.ellipse(side * 0.105, cz - 0.055, 0.03, 0.016, fill=P["blush"])
    # eye: tall rounded oval with two highlights
    face.ellipse(ex, ez, 0.026, 0.040, fill=P["black"])
    face.ellipse(ex - 0.008, ez + 0.016, 0.0095, 0.0115, fill=P["white"])
    face.ellipse(ex + 0.010, ez - 0.018, 0.0045, 0.0050, fill=P["white"])
    # brow: thick, angled down toward the centre (determined look)
    face.poly([(side * 0.035, cz + 0.058), (side * 0.105, cz + 0.082),
               (side * 0.108, cz + 0.066), (side * 0.040, cz + 0.044)], fill=P["hair"])
# mouth: small confident grin
face.poly([(-0.032, cz - 0.062), (0.032, cz - 0.062), (0.022, cz - 0.080), (0.0, cz - 0.086),
           (-0.022, cz - 0.080)], fill=P["mouth"])
face.poly([(-0.026, cz - 0.0625), (0.026, cz - 0.0625), (0.020, cz - 0.070), (-0.020, cz - 0.070)],
          fill=P["white"])

# ------------------------------------------------------------------ gi front (V neck + lapels)
T = L.TORSO
front = Region(L.REGIONS["torso_front"], L.TORSO_PROJ)
front.fill(P["orange"])
top = T["z1"]
v_bottom = top - 0.20
# dark-blue undershirt V
front.poly([(-0.115, top + 0.02), (0.115, top + 0.02), (0.0, v_bottom)], fill=P["navy"])
# lapel trim bands along the V; left panel overlaps down to the belt
front.line([(-0.125, top + 0.02), (0.0, v_bottom), (0.085, T["z0"] - 0.02)], 0.026, fill=P["orange_dark"])
front.line([(0.125, top + 0.02), (0.012, v_bottom + 0.03)], 0.026, fill=P["orange_dark"])

# ------------------------------------------------------------------ gi back (original emblem)
# Looking from behind: character's left (+X) is on the image left -> mirror u.
back = Region(L.REGIONS["torso_back"], L.TORSO_PROJ, mirror_u=True)
back.fill(P["orange"])
# undershirt collar peeking out at the back neckline
back.poly([(-0.10, top + 0.02), (0.10, top + 0.02), (0.075, top - 0.025), (-0.075, top - 0.025)], fill=P["navy"])
ecz = top - 0.15
back.ellipse(0.0, ecz, 0.092, 0.092, fill=P["navy"])
back.ellipse(0.0, ecz, 0.078, 0.078, fill=P["white"])
# stylised tower: dish top, column with rings, base
back.poly([(-0.045, ecz + 0.045), (0.045, ecz + 0.045), (0.022, ecz + 0.028), (-0.022, ecz + 0.028)], fill=P["gold"])
back.poly([(-0.014, ecz + 0.030), (0.014, ecz + 0.030), (0.014, ecz - 0.050), (-0.014, ecz - 0.050)], fill=P["gold"])
for rz in (0.012, -0.012, -0.036):
    back.poly([(-0.022, ecz + rz + 0.005), (0.022, ecz + rz + 0.005), (0.022, ecz + rz - 0.005),
               (-0.022, ecz + rz - 0.005)], fill=P["gold"])
back.poly([(-0.035, ecz - 0.048), (0.035, ecz - 0.048), (0.035, ecz - 0.060), (-0.035, ecz - 0.060)], fill=P["gold"])

face.paste()
front.paste()
back.paste()
img = img.resize((L.ATLAS_SIZE, L.ATLAS_SIZE), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print("wrote", OUT)
