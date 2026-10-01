"""B-05 Missile: cartoon red/white rocket with fins. Pivot at its centre, nose points/flies along Unity +Z (Blender -Y).
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_missile.py
"""
import math
import os
import sys

from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402

L = 1.30          # total length
C = L * 0.5       # pivot at the middle


def build():
    pl.start()
    prof = [(0.0, 0.0), (0.0, 0.085), (0.035, 0.10), (0.09, 0.125), (0.20, 0.175), (0.24, 0.185),
            (0.30, 0.185), (0.31, 0.19), (0.34, 0.19), (0.35, 0.185),
            (0.62, 0.185), (0.70, 0.185), (0.72, 0.185), (0.75, 0.185),
            (0.86, 0.185), (0.95, 0.178), (1.05, 0.155), (1.14, 0.115), (1.21, 0.065), (1.25, 0.025), (1.26, 0.0)]
    # lathe axis Z -> -Y (nose forward), shifted so the pivot is the centre
    m = Matrix.Translation((0, C, 0)) @ Matrix.Rotation(math.radians(90), 4, 'X')
    body = pl.lathe_part("body", prof, sides=16, color="white", matrix=m)

    def col(c, n):
        a = C - c.y            # distance from the tail
        if a < 0.04:
            return "dark"
        if a < 0.20:
            return "metal_dark"
        if a < 0.36:
            return "red"
        if 0.62 < a < 0.70:
            return "red"
        if 0.72 < a < 0.75:
            return "yellow"
        if a > 0.92:
            return "red"
        return None
    pl.paint_by(body, col)
    parts = [body]
    # four swept fins at the tail (+ four small canards near the nose)
    fin = [(0.0, 0.0), (0.30, 0.0), (0.16, 0.24), (0.02, 0.26)]
    for k in range(4):
        rot = Matrix.Rotation(math.radians(45 + 90 * k), 4, 'Y')
        # polygon plane: x = along axis (towards nose), y = radial
        place = (rot @ Matrix.Translation((0, C - 0.02, 0)) @ Matrix.Rotation(math.radians(-90), 4, 'Z')
                 @ Matrix.Translation((0, 0.13, 0)))
        f = pl.extruded_poly(f"fin{k}", fin, 0.035, matrix=place, color="red", bevel=0.008)
        parts.append(f)
        can = [(0.0, 0.0), (0.12, 0.0), (0.05, 0.09)]
        place2 = (rot @ Matrix.Translation((0, C - 0.84, 0)) @ Matrix.Rotation(math.radians(-90), 4, 'Z')
                  @ Matrix.Translation((0, 0.17, 0)))
        parts.append(pl.extruded_poly(f"can{k}", can, 0.025, matrix=place2, color="red", bevel=0.006))
    return pl.finalize(parts, "Prop_Missile", smooth_angle=45)


def main():
    o = build()
    pl.export([o], "Prop_Missile")
    print("TRIS Prop_Missile:", gt.tri_count(o), "dims", tuple(round(x, 3) for x in o.dimensions))
    pl.preview("prop_missile", target=(0, 0, 0), dist=3.2, cam_dir=(1.0, -0.6, 0.45))


if __name__ == "__main__":
    main()
