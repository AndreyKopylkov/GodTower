"""B-07 Axe: double-bladed battle axe. Pivot at the bounding-box centre so it spins cleanly.
Handle along Unity Y, blades extend to ±X, blade flat faces ±Z (spin around Z for a 'buzz-saw' sweep, or around Y).
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_axe.py
"""
import math
import os
import sys

from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402

HEAD_Z = 0.42
BLADE = [(0.05, 0.11), (0.16, 0.15), (0.27, 0.27), (0.33, 0.285), (0.39, 0.24), (0.435, 0.13), (0.45, 0.0),
         (0.435, -0.13), (0.39, -0.24), (0.33, -0.285), (0.27, -0.27), (0.16, -0.15), (0.05, -0.11)]


def taper(x, y):
    return 1.0 if x < 0.22 else max(0.22, 1.0 - (x - 0.22) / 0.23 * 0.78)


def build():
    pl.start()
    P = []
    for side in (1, -1):
        m = (Matrix.Translation((0, 0, HEAD_Z)) @ Matrix.Scale(side, 4, (1, 0, 0))
             @ Matrix.Rotation(math.radians(90), 4, 'X'))
        b = pl.extruded_poly(f"blade{side}", BLADE, 0.07, matrix=m, color="metal", edge_taper=taper, bevel=0.01)
        if side < 0:
            b.data.flip_normals()
        pl.paint_by(b, lambda c, n: "chrome" if abs(c.x) > 0.36 and abs(n.y) < 0.6 else None)
        P.append(b)
        plate = pl.extruded_poly(f"plate{side}", [(0.05, 0.135), (0.17, 0.175), (0.215, 0.0), (0.17, -0.175), (0.05, -0.135)],
                                 0.095, matrix=m, color="metal_dark", bevel=0.012)
        if side < 0:
            plate.data.flip_normals()
        P.append(plate)
        for k, zz in enumerate((0.09, -0.09)):
            P.append(pl.superellipsoid(f"rivet{side}{k}", (0.025, 0.025, 0.025), nu=6, nv=4, color="gold",
                                       matrix=Matrix.Translation((side * 0.12, -0.05, HEAD_Z + zz))))
            P.append(pl.superellipsoid(f"rivetb{side}{k}", (0.025, 0.025, 0.025), nu=6, nv=4, color="gold",
                                       matrix=Matrix.Translation((side * 0.12, 0.05, HEAD_Z + zz))))
    # head socket + top spike
    P.append(pl.cylinder("socket", 0.075, 0.30, sides=10, loc=(0, 0, HEAD_Z), color="metal_dark", bevel=0.015))
    P.append(pl.cylinder("collar_t", 0.085, 0.04, sides=10, loc=(0, 0, HEAD_Z + 0.16), color="gold"))
    P.append(pl.cylinder("collar_b", 0.085, 0.04, sides=10, loc=(0, 0, HEAD_Z - 0.16), color="gold"))
    P.append(pl.cylinder("spike", 0.06, 0.16, sides=10, loc=(0, 0, HEAD_Z + 0.26), color="metal", r2=0.0))
    # handle with grip wraps and pommel
    P.append(pl.cylinder("handle", 0.042, 1.18, sides=10, loc=(0, 0, HEAD_Z - 0.56), color="wood"))
    for k in range(4):
        P.append(pl.cylinder(f"wrap{k}", 0.05, 0.045, sides=10, loc=(0, 0, -0.42 - k * 0.075), color="wood_dark"))
    P.append(pl.superellipsoid("pommel", (0.07, 0.07, 0.06), nu=10, nv=6, color="gold",
                               matrix=Matrix.Translation((0, 0, HEAD_Z - 1.18))))
    o = pl.finalize(P, "Prop_Axe", smooth_angle=40)
    zs = [v.co.z for v in o.data.vertices]
    o.data.transform(Matrix.Translation((0, 0, -(min(zs) + max(zs)) / 2)))
    return o


def main():
    o = build()
    pl.export([o], "Prop_Axe")
    print("TRIS Prop_Axe:", gt.tri_count(o), "dims", tuple(round(x, 3) for x in o.dimensions))
    pl.preview("prop_axe", target=(0, 0, 0), dist=4.2, cam_dir=(0.45, -1.0, 0.3))


if __name__ == "__main__":
    main()
