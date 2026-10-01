"""B-10 Trophy: gold cup on a dark plinth. Pivot = bottom centre; front (star emblem + plaque) faces Unity +Z.
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_trophy.py
"""
import math
import os
import sys

from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402


def build():
    pl.start()
    P = []
    P.append(pl.box("plinth", (0.40, 0.40, 0.14), loc=(0, 0, 0.07), bevel=0.02, segments=2, color="wood_dark"))
    P.append(pl.box("plinth2", (0.30, 0.30, 0.06), loc=(0, 0, 0.17), bevel=0.012, color="wood_dark"))
    P.append(pl.box("plaque", (0.22, 0.012, 0.07), loc=(0, -0.203, 0.07), bevel=0.004, color="gold"))
    prof = [(0.20, 0.0), (0.20, 0.13), (0.215, 0.135), (0.24, 0.10), (0.28, 0.055), (0.36, 0.045),
            (0.38, 0.072), (0.40, 0.072), (0.42, 0.045), (0.48, 0.05), (0.51, 0.09), (0.56, 0.17),
            (0.64, 0.235), (0.74, 0.27), (0.84, 0.285), (0.875, 0.30), (0.895, 0.295), (0.89, 0.27),
            (0.80, 0.25), (0.68, 0.20), (0.60, 0.10), (0.58, 0.0)]
    cup = pl.lathe_part("cup", prof, sides=20, color="gold")
    pl.paint_by(cup, lambda c, n: "gold_dark" if 0.36 < c.z < 0.42 or c.z > 0.865 else None)
    P.append(cup)
    for sx in (-1, 1):
        pts = [Vector((sx * x, 0, z)) for x, z in
               ((0.24, 0.80), (0.33, 0.83), (0.41, 0.78), (0.43, 0.68), (0.38, 0.58), (0.27, 0.535), (0.15, 0.555))]
        P.append(pl.tube(f"handle{sx}", pts, 0.03, sides=10, color="gold", up=(0, 1, 0)))
    # star emblem on the front of the cup
    star = []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5
        r = 0.085 if k % 2 == 0 else 0.038
        star.append((math.cos(a) * r, math.sin(a) * r))
    m = Matrix.Translation((0, -0.262, 0.71)) @ Matrix.Rotation(math.radians(-8), 4, 'X') @ Matrix.Rotation(math.radians(90), 4, 'X')
    P.append(pl.extruded_poly("star", star, 0.03, matrix=m, color="red", bevel=0.005))
    return pl.finalize(P, "Prop_Trophy", smooth_angle=45)


def main():
    o = build()
    pl.export([o], "Prop_Trophy")
    print("TRIS Prop_Trophy:", gt.tri_count(o), "dims", tuple(round(x, 3) for x in o.dimensions))
    pl.preview("prop_trophy", target=(0, 0, 0.45), dist=2.6, cam_dir=(0.45, -1.0, 0.35))


if __name__ == "__main__":
    main()
