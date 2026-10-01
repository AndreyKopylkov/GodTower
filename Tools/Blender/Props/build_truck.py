"""B-06 Truck: cartoon cab-over delivery truck, readable from above and from the front.
Pivot at the bounding-box centre (so it can tumble while falling); front faces Unity +Z; roof up (+Y).
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_truck.py
"""
import math
import os
import sys

from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402


def build():
    pl.start()
    P = []
    B = pl.box
    # chassis + cab + cargo box (front = -Y)
    P.append(B("chassis", (1.16, 2.45, 0.22), loc=(0, 0.02, 0.42), color="dark"))
    P.append(B("cab", (1.26, 0.86, 0.98), loc=(0, -0.81, 0.97), bevel=0.09, segments=2, color="blue"))
    P.append(B("cargo", (1.34, 1.72, 1.28), loc=(0, 0.42, 1.12), bevel=0.05, segments=1, color="yellow"))
    # roof marking (readable from above): orange band + white chevron
    P.append(B("roofband", (1.06, 1.46, 0.03), loc=(0, 0.42, 1.765), color="orange"))
    P.append(pl.extruded_poly("chev", [(-0.38, 0.05), (0.0, -0.32), (0.38, 0.05), (0.38, 0.27), (0.0, -0.10), (-0.38, 0.27)],
                              0.03, matrix=Matrix.Translation((0, 0.42, 1.785)), color="white"))
    P.append(B("cabroof", (0.9, 0.5, 0.04), loc=(0, -0.83, 1.47), bevel=0.015, color="white"))
    # side stripes on the cargo box
    for sx in (-1, 1):
        P.append(B(f"stripe{sx}", (0.02, 1.5, 0.16), loc=(sx * 0.675, 0.42, 0.98), color="red"))
        P.append(B(f"stripe2{sx}", (0.02, 1.5, 0.05), loc=(sx * 0.675, 0.42, 1.12), color="white"))
    # glass
    P.append(B("windshield", (1.02, 0.05, 0.40), loc=(0, -1.235, 1.12), bevel=0.02, color="glass"))
    for sx in (-1, 1):
        P.append(B(f"sidewin{sx}", (0.05, 0.46, 0.34), loc=(sx * 0.622, -0.86, 1.12), bevel=0.015, color="glass"))
        P.append(B(f"mirror{sx}", (0.06, 0.06, 0.2), loc=(sx * 0.72, -1.12, 1.08), bevel=0.01, color="dark"))
    # front: bumper, grille, headlights
    P.append(B("bumper", (1.32, 0.16, 0.18), loc=(0, -1.27, 0.52), bevel=0.04, color="grey"))
    P.append(B("grille", (0.52, 0.04, 0.2), loc=(0, -1.245, 0.74), color="dark"))
    for sx in (-1, 1):
        P.append(pl.cylinder(f"lamp{sx}", 0.085, 0.05, sides=10, loc=(sx * 0.42, -1.25, 0.74),
                             rot=(math.radians(90), 0, 0), color="cream"))
    # wheels
    for sx in (-1, 1):
        for wy in (-0.78, 0.78):
            P.append(pl.cylinder(f"tyre{sx}{wy}", 0.31, 0.24, sides=14, loc=(sx * 0.56, wy, 0.31),
                                 rot=(0, math.radians(90), 0), color="rubber", bevel=0.04))
            P.append(pl.cylinder(f"hub{sx}{wy}", 0.14, 0.26, sides=8, loc=(sx * 0.56, wy, 0.31),
                                 rot=(0, math.radians(90), 0), color="metal"))
    o = pl.finalize(P, "Prop_Truck", smooth_angle=40)
    # pivot to bbox centre
    zs = [v.co.z for v in o.data.vertices]
    ys = [v.co.y for v in o.data.vertices]
    o.data.transform(Matrix.Translation((0, -(min(ys) + max(ys)) / 2, -(min(zs) + max(zs)) / 2)))
    return o


def main():
    o = build()
    pl.export([o], "Prop_Truck")
    print("TRIS Prop_Truck:", gt.tri_count(o), "dims", tuple(round(x, 3) for x in o.dimensions))
    pl.preview("prop_truck", target=(0, 0, 0), dist=7, cam_dir=(0.8, -1.0, 0.6))
    pl.preview("prop_truck_top", target=(0, 0, 0), dist=7, cam_dir=(0.05, -0.35, 1.0))


if __name__ == "__main__":
    main()
