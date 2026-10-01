"""B-08 Jetpack: two tanks + nozzles on a back plate.
Pivot = back contact point (centre of the plate's front face). The pack extends behind the wearer along Unity -Z
(wearer faces +Z), up = +Y, nozzles point down (-Y). Nozzle exits at y = -0.47, x = ±0.135, z = -0.17 (attach flame VFX there).
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_jetpack.py
"""
import math
import os
import sys

from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402

TY = 0.17   # tank axis distance behind the contact point (Blender +Y)


def build():
    pl.start()
    P = []
    P.append(pl.box("plate", (0.44, 0.07, 0.52), loc=(0, 0.035, 0.0), bevel=0.025, segments=2, color="blue_dark"))
    for sx in (-1, 1):
        x = sx * 0.135
        tank = pl.superellipsoid(f"tank{sx}", (0.115, 0.115, 0.30), nu=14, nv=12, e1=0.7, e2=1.0,
                                 matrix=Matrix.Translation((x, TY, 0.02)), color="white")
        pl.paint_by(tank, lambda c, n: "red" if c.z > 0.22 or -0.06 < c.z < 0.0 else None)
        P.append(tank)
        # nozzle: flared cone + dark exit
        P.append(pl.cylinder(f"neck{sx}", 0.06, 0.08, sides=12, loc=(x, TY, -0.31), color="metal_dark"))
        P.append(pl.cylinder(f"noz{sx}", 0.055, 0.12, sides=12, loc=(x, TY, -0.40), r2=0.085, color="metal",
                             rot=(math.radians(180), 0, 0)))
        P.append(pl.cylinder(f"exit{sx}", 0.07, 0.01, sides=12, loc=(x, TY, -0.465), color="dark"))
        # little fin on the outer side
        fin = [(0.0, 0.0), (0.0, 0.22), (0.12, 0.05), (0.12, -0.06)]
        m = Matrix.Translation((x + sx * 0.1, TY, -0.18)) @ Matrix.Scale(sx, 4, (1, 0, 0)) @ Matrix.Rotation(math.radians(90), 4, 'X')
        f = pl.extruded_poly(f"fin{sx}", fin, 0.03, matrix=m, color="red", bevel=0.006)
        if sx < 0:
            f.data.flip_normals()
        P.append(f)
    for z in (0.16, -0.16):
        P.append(pl.box(f"bracket{z}", (0.2, 0.1, 0.07), loc=(0, TY - 0.02, z), bevel=0.015, color="grey"))
    # top gauge light + shoulder strap stubs
    P.append(pl.cylinder("gauge", 0.045, 0.03, sides=10, loc=(0, 0.075, 0.18), rot=(math.radians(90), 0, 0), color="yellow"))
    for sx in (-1, 1):
        P.append(pl.box(f"strap{sx}", (0.07, 0.16, 0.035), loc=(sx * 0.14, -0.03, 0.27), bevel=0.01, color="dark"))
    return pl.finalize(P, "Prop_Jetpack", smooth_angle=45)


def main():
    o = build()
    pl.export([o], "Prop_Jetpack")
    print("TRIS Prop_Jetpack:", gt.tri_count(o), "dims", tuple(round(x, 3) for x in o.dimensions))
    pl.preview("prop_jetpack", target=(0, 0.15, -0.05), dist=2.4, cam_dir=(0.8, 1.0, 0.35))


if __name__ == "__main__":
    main()
