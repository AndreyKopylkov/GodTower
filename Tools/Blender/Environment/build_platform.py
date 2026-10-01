"""Top platform (B-03): fluted neck -> bell flare -> wide saucer with a flat walkable deck.

Joint at z=0 matches the tower segments exactly (radius 1.5, plain 6 cm, 40-side column is a subset of 80 sides).
Run: blender.exe -b --factory-startup --python Tools/Blender/Environment/build_platform.py
"""
import math
import os
import sys

import bmesh

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "Common"))
import gtlib as gt  # noqa: E402
import tower_spec as spec  # noqa: E402

OUT = gt.PROJECT + "/Assets/Art/Environment/Tower"
TEX = OUT + "/Textures"
SIDES = spec.TOP_SIDES
SECTORS = spec.TOP_SIDES // 4   # one UV island per flute so the texture can darken flute valleys
DECK_Z = spec.TOP_DECK_Z
DECK_UV_R = 6.6


def main():
    gt.reset_scene()
    P = spec.top_profile()
    # side UVs: arc length -> v in strip u[0.72,1.0]
    acc = [0.0]
    for i in range(1, len(P)):
        acc.append(acc[-1] + math.hypot(P[i][0] - P[i - 1][0], P[i][1] - P[i - 1][1]))
    total = acc[-1]

    def uv(frac, i):
        return (0.72 + 0.28 * frac, acc[i] / total)

    rib_shape = [0.0, 0.7, 1.0, 0.7]   # rounded flutes, 20 ribs around
    obj = gt.lathe("Tower_Top", P, SIDES, uv, sectors=SECTORS, rib=lambda g: rib_shape[g % 4], phase=0.0)
    # planar UVs for upward-facing deck + rim top faces
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv.active
    cx, cy, half = 0.35, 0.65, 0.35
    for f in bm.faces:
        c = f.calc_center_median()
        if f.normal.z > 0.9 and c.z > DECK_Z - 0.01:
            for lp in f.loops:
                co = lp.vert.co
                lp[uvl].uv = (cx + co.x / DECK_UV_R * half, cy + co.y / DECK_UV_R * half)
    bm.to_mesh(me)
    bm.free()
    gt.smooth_by_angle(obj, 40)
    mat = gt.material("M_TowerTop", albedo=TEX + "/T_TowerTop_Albedo.png",
                      normal=TEX + "/T_TowerTop_Normal.png", roughness=0.85)
    gt.assign(obj, mat)
    gt.export_fbx(OUT + "/Tower_Top.fbx", [obj])
    print("TRIS Tower_Top:", gt.tri_count(obj))

    # previews: underside like ref 91 s (with a column segment below) and top view of the deck
    import build_tower as bt
    smat = gt.material("M_TowerStone", albedo=TEX + "/T_TowerStone_Albedo.png",
                       normal=TEX + "/T_TowerStone_Normal.png", roughness=0.85)
    for k, nm in enumerate(("Ribbed", "Relief")):
        seg = bt.build_segment(nm, smat)
        seg.location.z = -3.0 * (k + 1)
    gt.preview(gt.PREVIEWS + "/tower_top_under.png", target=(0, 0, 1.6), cam_dir=(0.1, -1, -0.12),
               dist=22, lens=50, res=(900, 900))
    gt.preview(gt.PREVIEWS + "/tower_top_deck.png", target=(0, 0, 3.4), cam_dir=(0.2, -0.8, 0.9),
               dist=20, lens=50, res=(900, 900))


main()
