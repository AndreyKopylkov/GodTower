"""B-04 Boxing glove (right hand). Pivot at the wrist (cuff opening), punches along Unity +Z (Blender -Y).
Back of the hand = up (+Y Unity). Thumb on the glove's inner side (Unity -X... see status). Mirror X for a left glove.
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_glove.py
"""
import math
import os
import sys

from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402

FIST_Y = -0.45


def fist_deform(v):
    t = (v.y - FIST_Y) / 0.26          # -1 at the knuckle tip, +1 toward the wrist
    if t > 0:
        k = 1 - 0.20 * t * t
        v.x *= k
        v.z = v.z * k + 0.01 * t
    if v.z < 0:
        v.z *= 1 + 0.18 * max(0.0, -t)  # fingers curl down/in at the front
    if v.z > 0:
        v.z *= 1 + 0.06 * max(0.0, -t)  # chunky knuckle mound
    return v


def build():
    pl.start()
    rotZtoX = Matrix.Rotation(math.radians(90), 4, 'Y')   # local poles (Z) -> X axis (sides)
    # main body: local radii (x->height, y->length, z->width after rotation)
    fist = pl.superellipsoid("fist", (0.20, 0.26, 0.225), nu=24, nv=14, e1=0.72, e2=0.8,
                             matrix=Matrix.Translation((0, FIST_Y, 0.02)) @ rotZtoX,
                             deform=fist_deform, color="red")
    # curled finger roll across the front/bottom -> classic boxing-glove silhouette + crease
    roll = pl.superellipsoid("roll", (0.13, 0.12, 0.195), nu=18, nv=8, e1=0.85, e2=0.9,
                             matrix=Matrix.Translation((0, -0.565, -0.085)) @ rotZtoX
                             @ Matrix.Rotation(math.radians(-25), 4, 'Z'), color="red")
    # thumb on +X side, front end tucked down/in
    thumb_m = (Matrix.Translation((0.185, -0.44, -0.035)) @ Matrix.Rotation(math.radians(-16), 4, 'Z')
               @ Matrix.Rotation(math.radians(-14), 4, 'X') @ Matrix.Rotation(math.radians(90), 4, 'X'))
    thumb = pl.superellipsoid("thumb", (0.075, 0.07, 0.165), nu=12, nv=8, e1=0.9, e2=0.9,
                              matrix=thumb_m, color="red")
    # cuff: lathe along -Y (elliptical), stripe + piping painted by position
    prof = [(0.03, 0.0), (0.0, 0.122), (-0.012, 0.157), (0.006, 0.18), (0.03, 0.184),
            (0.05, 0.184), (0.075, 0.184), (0.16, 0.178), (0.172, 0.193), (0.198, 0.193), (0.21, 0.175), (0.30, 0.12)]
    cuff_m = Matrix.Scale(0.9, 4, (0, 0, 1)) @ Matrix.Rotation(math.radians(90), 4, 'X')
    cuff = pl.lathe_part("cuff", prof, sides=20, color="white", matrix=cuff_m)
    pl.paint_faces(cuff, "dark", lambda c, n: math.hypot(c.x, c.z) < 0.115 and c.y > -0.04)
    pl.paint_faces(cuff, "red", lambda c, n: -0.076 < c.y < -0.049)
    pl.paint_faces(cuff, "red_dark", lambda c, n: -0.212 < c.y < -0.17)
    # lacing patch on the palm side (-Z)
    pl.paint_faces(cuff, "dark", lambda c, n: c.z < -0.11 and -0.16 < c.y < -0.085 and abs(c.x) < 0.07)
    glove = pl.finalize([fist, roll, thumb, cuff], "Prop_BoxingGlove", smooth_angle=60)
    return glove


def main():
    g = build()
    pl.export([g], "Prop_BoxingGlove")
    print("TRIS Prop_BoxingGlove:", gt.tri_count(g), "dims", tuple(round(x, 3) for x in g.dimensions))
    pl.preview("prop_glove_front", target=(0.02, -0.35, 0.0), dist=2.2, cam_dir=(0.55, -1.0, 0.5))
    pl.preview("prop_glove_side", target=(0.0, -0.35, 0.0), dist=2.2, cam_dir=(1.0, 0.25, 0.25))
    pl.preview("prop_glove_back", target=(0.0, -0.35, 0.0), dist=2.2, cam_dir=(-0.4, 1.0, -0.3))


if __name__ == "__main__":
    main()
