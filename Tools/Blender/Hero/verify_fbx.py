"""Re-import the exported hero FBX files into an empty scene and check them.

    "D:/Games/Steam/steamapps/common/Blender/blender.exe" -b --factory-startup --python Tools/Blender/Hero/verify_fbx.py
Prints VERIFY lines; VERIFY_OK at the end when every check passes.
"""
import os

import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Characters", "Hero")
EXPECTED = {
    "Hero_ClimbUp": 24, "Hero_HangIdle": 60, "Hero_ShiftLeft": 8, "Hero_ShiftRight": 8, "Hero_Hit": 12,
    "Hero_Fall": 20, "Hero_Carried": 30, "Hero_Win": 45, "Hero_Lose": 36,
}
ok = True


def check(cond, msg):
    global ok
    print("VERIFY", "PASS" if cond else "FAIL", msg)
    ok = ok and cond


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "Hero.fbx"))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
arms = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
check(len(meshes) == 1 and len(arms) == 1, f"Hero.fbx: {len(meshes)} mesh, {len(arms)} armature")
m = meshes[0]
tris = sum(len(p.vertices) - 2 for p in m.data.polygons)
check(tris <= 8000, f"triangles {tris}")
check(len(m.data.materials) == 1, f"materials {[mt.name for mt in m.data.materials]}")
dg = bpy.context.evaluated_depsgraph_get()
zs = [(m.matrix_world @ v.co).z for v in m.data.vertices]
check(abs(max(zs) - 1.8) < 0.01 and abs(min(zs)) < 0.01, f"height {min(zs):.3f}..{max(zs):.3f} m")
bones = [b.name for b in arms[0].data.bones]
check(len(bones) == 21, f"bones {len(bones)}: {', '.join(bones)}")
# Toes, Spine and Shoulders carry no geometry, so 16 groups are weighted.
check(len(m.vertex_groups) >= 16, f"vertex groups {len(m.vertex_groups)}")
check(len(bpy.data.actions) == 0, f"Hero.fbx has no takes ({len(bpy.data.actions)})")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "Hero_Animations.fbx"))
found = {}
for a in bpy.data.actions:
    name = a.name.split("|")[-1]
    found[name] = a.frame_range
for clip, end in EXPECTED.items():
    fr = found.get(clip)
    # Blender's importer offsets takes to the scene start frame (1); the length is what matters.
    check(fr is not None and abs((fr[1] - fr[0]) - end) < 0.01,
          f"take {clip}: {tuple(round(x, 2) for x in fr) if fr else 'missing'} ({end} frames expected)")
print("VERIFY actions:", sorted(a.name for a in bpy.data.actions))
print("VERIFY_OK" if ok else "VERIFY_FAILED")
