"""Build the 4 tileable tower segments (B-02) and export FBX + previews.

Run (after tower_textures.py):
  blender.exe -b --factory-startup --python Tools/Blender/Environment/build_tower.py
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "Common"))
import gtlib as gt  # noqa: E402
import tower_spec as spec  # noqa: E402

OUT = gt.PROJECT + "/Assets/Art/Environment/Tower"
TEX = OUT + "/Textures"


def uv_for(name):
    u0, v0 = spec.ATLAS_TILES[name]

    def make(profile):
        def fn(frac, i):
            return (u0 + 0.5 * frac, v0 + 0.5 * (profile[i][0] / spec.H))
        return fn
    return make


def flute_tube(name, z0, z1, ribs, uvmake):
    """Vertical flutes: ring of `ribs` rounded ribs as a separate open tube inside the recess."""
    per_rib = 4
    sides = ribs * per_rib
    shape = [spec.FLUTE_VALLEY, spec.FLUTE_TOP - 0.012, spec.FLUTE_TOP, spec.FLUTE_TOP - 0.012]
    profile = [(z0, 1.0), (z1, 1.0)]
    uv = uvmake(profile)
    obj = gt.lathe(name, [(z0, 0.0, 1.0), (z1, 0.0, 1.0)], sides, uv, sectors=spec.SECTORS,
                   rib=lambda g: shape[g % per_rib], phase=math.pi / sides)
    return obj


def build_segment(name, mat):
    profile, bands, flutes, creases = spec.build(name)
    make = uv_for(name)
    body = gt.lathe("Tower_Segment_" + name, profile, spec.SIDES, make(profile), sectors=spec.SECTORS)
    parts = [body]
    for i, (z0, z1, ribs) in enumerate(flutes):
        parts.append(flute_tube(f"fl{i}", z0, z1, ribs, make))
    obj = gt.join(parts, "Tower_Segment_" + name) if len(parts) > 1 else body
    gt.smooth_by_angle(obj, 40)
    gt.assign(obj, mat)
    return obj


def main():
    gt.reset_scene()
    mat = gt.material("M_TowerStone", albedo=TEX + "/T_TowerStone_Albedo.png",
                      normal=TEX + "/T_TowerStone_Normal.png", roughness=0.85)
    objs = []
    for name in spec.SEGMENTS:
        o = build_segment(name, mat)
        objs.append(o)
        gt.export_fbx(f"{OUT}/Tower_Segment_{name}.fbx", [o])
        print(f"TRIS {o.name}: {gt.tri_count(o)}")
    # preview: stack all four segments like a real tower
    for i, o in enumerate(objs):
        o.location.z = i * spec.H
    gt.preview(gt.PREVIEWS + "/tower_segments_stack.png", target=(0, 0, 6), cam_dir=(0.15, -1, 0.05),
               dist=40, lens=50, res=(600, 1200))
    gt.preview(gt.PREVIEWS + "/tower_segments_close.png", target=(0, 0, 4.5), cam_dir=(0.35, -1, 0.15),
               dist=12, lens=50, res=(900, 1000))


if __name__ == "__main__":
    main()
