"""Build the God Tower hero procedurally: mesh, UVs, material, humanoid rig, 9 animation clips, FBX export.

Run from the project root (Blender 5.2, headless):
    "D:/Games/Steam/steamapps/common/Blender/blender.exe" -b --factory-startup --python Tools/Blender/Hero/build_hero.py

Inputs : Art/Source/Character/Hero_Albedo.png   (make_atlas.py)
Outputs: Art/Source/Character/hero.blend
         Assets/Art/Characters/Hero/Hero.fbx               (mesh + rig, T-pose)
         Assets/Art/Characters/Hero/Hero_Animations.fbx    (rig + 9 takes)
         Assets/Art/Characters/Hero/Textures/Hero_Albedo.png

Conventions while building: metres, Z up, the character faces -Y (Blender front), character's left = +X.
FBX export uses forward -Z / up Y, so in Unity the hero is Y-up and faces +Z.
"""
import math
import os
import shutil
import sys

import bmesh
import bpy
from mathutils import Euler, Quaternion, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import hero_layout as L  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
SRC_DIR = os.path.join(ROOT, "Art", "Source", "Character")
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "Characters", "Hero")
TEX_SRC = os.path.join(SRC_DIR, "Hero_Albedo.png")
TEX_OUT = os.path.join(OUT_DIR, "Textures", "Hero_Albedo.png")
TARGET_HEIGHT = 1.80
FPS = 30

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS

# =============================================================================== mesh
bm = bmesh.new()
uv_layer = bm.loops.layers.uv.verify()
deform = bm.verts.layers.deform.verify()

BONES = [
    # name, head, tail, parent
    ("Hips", (0, 0, 0.83), (0, 0, 0.95), None),
    ("Spine", (0, 0, 0.95), (0, 0, 1.12), "Hips"),
    ("Chest", (0, 0, 1.12), (0, 0, 1.36), "Spine"),
    ("Neck", (0, 0, 1.36), (0, 0, 1.43), "Chest"),
    ("Head", (0, 0, 1.43), (0, 0, 1.80), "Neck"),
]
for side, s in (("Left", 1), ("Right", -1)):
    BONES += [
        (side + "Shoulder", (s * 0.06, 0, 1.30), (s * 0.37, 0, 1.30), "Chest"),
        (side + "UpperArm", (s * 0.37, 0, 1.30), (s * 0.555, 0, 1.30), side + "Shoulder"),
        (side + "LowerArm", (s * 0.555, 0, 1.30), (s * 0.745, 0, 1.30), side + "UpperArm"),
        (side + "Hand", (s * 0.745, 0, 1.30), (s * 0.87, 0, 1.30), side + "LowerArm"),
        (side + "UpperLeg", (s * 0.13, 0, 0.83), (s * 0.13, 0, 0.49), "Hips"),
        (side + "LowerLeg", (s * 0.13, 0, 0.49), (s * 0.13, 0, 0.14), side + "UpperLeg"),
        (side + "Foot", (s * 0.13, 0, 0.14), (s * 0.13, -0.13, 0.04), side + "LowerLeg"),
        (side + "Toes", (s * 0.13, -0.13, 0.04), (s * 0.13, -0.21, 0.04), side + "Foot"),
    ]
BONE_INDEX = {b[0]: i for i, b in enumerate(BONES)}


def swatch_uv(color):
    x0, y0, x1, y1 = L.swatch_rect(color)
    return Vector(L.px_to_uv((x0 + x1) / 2, (y0 + y1) / 2))


def region_uv(region, proj, x, z, mirror=False):
    rx0, ry0, rx1, ry1 = L.REGIONS[region]
    x0, x1, z0, z1 = proj
    fu = (x - x0) / (x1 - x0)
    if mirror:
        fu = 1.0 - fu
    fv = (z1 - z) / (z1 - z0)
    fu = min(max(fu, 0.004), 0.996)
    fv = min(max(fv, 0.004), 0.996)
    return Vector(L.px_to_uv(rx0 + fu * (rx1 - rx0), ry0 + fv * (ry1 - ry0)))


def finish_part(new_faces, bone, uv_rule):
    """Assign UVs (uv_rule(face) -> callable(vert)->uv or Vector) and rigid weight to one bone."""
    gi = BONE_INDEX[bone]
    verts = set()
    for f in new_faces:
        f.normal_update()
        rule = uv_rule(f)
        for loop in f.loops:
            loop[uv_layer].uv = rule(loop.vert.co) if callable(rule) else rule
            verts.add(loop.vert)
    for v in verts:
        v[deform].clear()
        v[deform][gi] = 1.0


def solid(color):
    uv = swatch_uv(color)
    return lambda f: uv


def add_box(bone, center, size, color="orange", bevel=0.03, seg=2, rot=None, uv_rule=None, taper=None):
    before = set(bm.faces)
    res = bmesh.ops.create_cube(bm, size=1.0)
    vs = res["verts"]
    cx, cy, cz = center
    sx, sy, sz = size
    for v in vs:
        x, y, z = v.co.x * sx, v.co.y * sy, v.co.z * sz
        if taper:  # (bottom_scale, top_scale) on X/Y
            t = (v.co.z + 0.5)
            k = taper[0] + (taper[1] - taper[0]) * t
            x *= k
            y *= k
        co = Vector((x, y, z))
        if rot:
            co = Euler([math.radians(a) for a in rot]).to_matrix() @ co
        v.co = co + Vector((cx, cy, cz))
    if bevel > 0:
        edges = list({e for v in vs for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=bevel, offset_type='OFFSET', segments=seg,
                        profile=0.5, affect='EDGES', clamp_overlap=True)
    new = [f for f in bm.faces if f not in before]
    finish_part(new, bone, uv_rule or solid(color))
    return new


def add_spike(bone, base, direction, length, radius, bend=(0, 0, 0), sides=5, rings=3, color="hair"):
    before = set(bm.faces)
    base = Vector(base)
    d = Vector(direction).normalized()
    bend = Vector(bend)

    def point(t):
        return base + d * length * t + bend * (t * t)

    ref = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    ring_verts = []
    for i in range(rings):
        t = i / rings
        p = point(t)
        tan = (point(t + 0.01) - p).normalized()
        a = tan.cross(ref).normalized()
        b = tan.cross(a).normalized()
        r = radius * (1.0 - t) ** 0.85
        ring = []
        for k in range(sides):
            ang = 2 * math.pi * k / sides
            ring.append(bm.verts.new(p + (a * math.cos(ang) + b * math.sin(ang)) * r))
        ring_verts.append(ring)
    tip = bm.verts.new(point(1.0))
    for i in range(rings - 1):
        r0, r1 = ring_verts[i], ring_verts[i + 1]
        for k in range(sides):
            k2 = (k + 1) % sides
            bm.faces.new((r0[k], r0[k2], r1[k2], r1[k]))
    last = ring_verts[-1]
    for k in range(sides):
        bm.faces.new((last[k], last[(k + 1) % sides], tip))
    bm.faces.new(list(reversed(ring_verts[0])))  # cap (hidden inside the hair)
    new = [f for f in bm.faces if f not in before]
    bmesh.ops.recalc_face_normals(bm, faces=new)
    finish_part(new, bone, solid(color))


# ---------------------------------------------------------------- head + face
H = L.HEAD
skin_uv = swatch_uv("skin")


def face_rule(f):
    if f.normal.y < -0.3:
        return lambda co: region_uv("face", L.FACE_PROJ, co.x, co.z)
    return skin_uv


add_box("Head", (H["cx"], H["cy"], H["cz"]), (H["w"], H["d"], H["h"]), bevel=0.10, seg=4, uv_rule=face_rule)
add_box("Neck", (0, 0.0, 1.41), (0.15, 0.14, 0.09), "skin", bevel=0.02, seg=1)

# ---------------------------------------------------------------- hair (original spike layout)
# Hair is authored around the design head (FACE_DESIGN_HEAD) and mapped onto the real head size.
DH = L.FACE_DESIGN_HEAD
HF = Vector((H["w"] / DH["w"], H["d"] / DH["d"], H["h"] / DH["h"]))
HK = (HF.x + HF.y + HF.z) / 3


def hp(p):
    o = Vector((DH["cx"], DH["cy"], DH["cz"]))
    n = Vector((H["cx"], H["cy"], H["cz"]))
    return tuple(n + (Vector(p) - o) * HF)


def hs(sz):
    return tuple(Vector(sz) * HF)


add_box("Head", hp((0, 0.012, 1.738)), hs((0.415, 0.385, 0.11)), "hair", bevel=0.055, seg=3)   # cap
add_box("Head", hp((0, 0.105, 1.605)), hs((0.405, 0.17, 0.29)), "hair", bevel=0.055, seg=3)    # back
SPIKES = [
    # base, dir, length, radius, bend   (swept-back flame: everything leans back, one front quiff)
    ((0.0, 0.04, 1.77), (0, 0.65, 1), 0.30, 0.09, (0, 0.10, -0.03)),         # crown
    ((0.10, 0.02, 1.76), (0.5, 0.5, 1), 0.25, 0.08, (0.04, 0.06, -0.05)),
    ((-0.10, 0.02, 1.76), (-0.5, 0.5, 1), 0.25, 0.08, (-0.04, 0.06, -0.05)),
    ((0.03, -0.11, 1.77), (0.15, -0.45, 1), 0.17, 0.07, (0, 0.10, 0.0)),      # front quiff
    ((0.19, 0.06, 1.69), (1, 0.45, 0.5), 0.20, 0.075, (0, 0.04, 0.06)),       # sides
    ((-0.19, 0.06, 1.69), (-1, 0.45, 0.5), 0.20, 0.075, (0, 0.04, 0.06)),
    ((0.18, 0.12, 1.56), (1, 0.7, -0.1), 0.15, 0.065, (0, 0.04, 0.05)),
    ((-0.18, 0.12, 1.56), (-1, 0.7, -0.1), 0.15, 0.065, (0, 0.04, 0.05)),
    ((0.0, 0.19, 1.71), (0, 1, 0.35), 0.24, 0.085, (0, 0, 0.06)),             # back
    ((0.11, 0.18, 1.61), (0.45, 1, 0.05), 0.19, 0.07, (0, 0, 0.04)),
    ((-0.11, 0.18, 1.61), (-0.45, 1, 0.05), 0.19, 0.07, (0, 0, 0.04)),
    ((0.0, 0.18, 1.50), (0, 1, -0.35), 0.14, 0.06, (0, 0, 0.02)),
    ((0.08, -0.17, 1.70), (0.35, -0.5, -0.75), 0.10, 0.05, (0, 0, 0)),        # fringe (asymmetric)
]
for base, dvec, ln, rad, bend in SPIKES:
    add_spike("Head", hp(base), tuple(Vector(dvec) * HF), ln * HK, rad * HK * 1.15, tuple(Vector(bend) * HK))

# ---------------------------------------------------------------- torso (gi top)
T = L.TORSO


def torso_rule(f):
    if f.normal.y < -0.3:
        return lambda co: region_uv("torso_front", L.TORSO_PROJ, co.x, co.z)
    if f.normal.y > 0.3:
        return lambda co: region_uv("torso_back", L.TORSO_PROJ, co.x, co.z, mirror=True)
    return swatch_uv("orange")


add_box("Chest", (0, 0, (T["z0"] + T["z1"]) / 2), (T["x1"] - T["x0"], T["y1"] - T["y0"], T["z1"] - T["z0"]),
        bevel=0.045, seg=3, uv_rule=torso_rule)

# ---------------------------------------------------------------- pelvis + belt
add_box("Hips", (0, 0, 0.84), (0.54, 0.29, 0.15), "orange", bevel=0.04, seg=2)
add_box("Hips", (0, 0, 0.915), (0.578, 0.318, 0.075), "navy", bevel=0.02, seg=2)
add_box("Hips", (0.06, -0.168, 0.915), (0.075, 0.04, 0.07), "navy", bevel=0.015, seg=1)             # knot
add_box("Hips", (0.045, -0.168, 0.85), (0.036, 0.022, 0.10), "navy", bevel=0.008, seg=1, rot=(0, 12, 0))
add_box("Hips", (0.085, -0.168, 0.845), (0.036, 0.022, 0.11), "navy", bevel=0.008, seg=1, rot=(0, -10, 0))

# ---------------------------------------------------------------- arms
for side, s in (("Left", 1), ("Right", -1)):
    add_box(side + "UpperArm", (s * 0.358, 0, 1.295), (0.17, 0.195, 0.195), "navy", bevel=0.04, seg=2)  # sleeve
    add_box(side + "UpperArm", (s * 0.48, 0, 1.295), (0.17, 0.16, 0.16), "skin", bevel=0.045, seg=2)
    add_box(side + "LowerArm", (s * 0.625, 0, 1.295), (0.19, 0.16, 0.16), "skin", bevel=0.04, seg=2)
    add_box(side + "LowerArm", (s * 0.70, 0, 1.295), (0.085, 0.18, 0.18), "navy", bevel=0.03, seg=2)  # wristband
    add_box(side + "Hand", (s * 0.805, -0.005, 1.29), (0.135, 0.17, 0.17), "skin", bevel=0.055, seg=2)  # fist

# ---------------------------------------------------------------- legs (gi trousers + boots)
for side, s in (("Left", 1), ("Right", -1)):
    add_box(side + "UpperLeg", (s * 0.13, 0, 0.665), (0.245, 0.255, 0.37), "orange", bevel=0.045, seg=2)
    add_box(side + "LowerLeg", (s * 0.132, -0.005, 0.335), (0.25, 0.265, 0.33), "orange", bevel=0.05, seg=2,
            taper=(0.92, 1.04))
    add_box(side + "Foot", (s * 0.132, -0.03, 0.125), (0.225, 0.30, 0.18), "navy_dark", bevel=0.05, seg=2)
    add_box(side + "Foot", (s * 0.132, -0.035, 0.022), (0.235, 0.32, 0.044), "sole", bevel=0.015, seg=1)

# ---------------------------------------------------------------- scale to target height
# Shorten the legs for chunkier, cuter proportions: z below LEG_TOP is compressed by LEG_K,
# everything above moves down accordingly (piecewise linear, continuous).
LEG_TOP, LEG_K = 0.80, 0.84


def remap_z(z):
    return z * LEG_K if z <= LEG_TOP else z - LEG_TOP * (1 - LEG_K)


for v in bm.verts:
    v.co.z = remap_z(v.co.z)
bm.normal_update()
max_z = max(v.co.z for v in bm.verts)
min_z = min(v.co.z for v in bm.verts)
S = TARGET_HEIGHT / (max_z - min_z)
for v in bm.verts:
    v.co = Vector((v.co.x, v.co.y, v.co.z - min_z)) * S

mesh = bpy.data.meshes.new("HeroBody")
bm.to_mesh(mesh)
bm.free()
for p in mesh.polygons:
    p.use_smooth = True
mesh.set_sharp_from_angle(angle=math.radians(40))
body_ob = bpy.data.objects.new("HeroBody", mesh)
scene.collection.objects.link(body_ob)
for name, *_ in BONES:
    body_ob.vertex_groups.new(name=name)

# ---------------------------------------------------------------- material
img = bpy.data.images.load(TEX_SRC)
img.name = "Hero_Albedo.png"
mat = bpy.data.materials.new("M_Hero")
mat.use_nodes = True
nt = mat.node_tree
bsdf = nt.nodes.get("Principled BSDF")
tex = nt.nodes.new("ShaderNodeTexImage")
tex.image = img
tex.location = (-400, 200)
nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.65
mesh.materials.append(mat)

tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
print("HERO_TRIS", tris, "SCALE", round(S, 4), "VERTS", len(mesh.vertices))

# =============================================================================== armature
arm_data = bpy.data.armatures.new("HeroRig")
rig = bpy.data.objects.new("HeroRig", arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')


def sc(p):
    return Vector((p[0], p[1], remap_z(p[2]) - min_z)) * S


for name, head, tail, parent in BONES:
    eb = arm_data.edit_bones.new(name)
    eb.head = sc(head)
    eb.tail = sc(tail)
    eb.roll = 0.0
    if parent:
        eb.parent = arm_data.edit_bones[parent]
        eb.use_connect = False
bpy.ops.object.mode_set(mode='OBJECT')
arm_data.display_type = 'STICK'

body_ob.parent = rig
mod = body_ob.modifiers.new("Armature", 'ARMATURE')
mod.object = rig

for pb in rig.pose.bones:
    pb.rotation_mode = 'QUATERNION'

# =============================================================================== pose solver
ORDER = []


def _walk(b):
    ORDER.append(b)
    for c in b.children:
        _walk(c)


for b in arm_data.bones:
    if b.parent is None:
        _walk(b)

# Hinge bend directions in rest (armature) space: elbows bend forward (-Y), knees backward (+Y).
CHAINS = {
    "LeftUpperArm": ("LeftLowerArm", Vector((0, -1, 0))),
    "RightUpperArm": ("RightLowerArm", Vector((0, -1, 0))),
    "LeftUpperLeg": ("LeftLowerLeg", Vector((0, 1, 0))),
    "RightUpperLeg": ("RightLowerLeg", Vector((0, 1, 0))),
}


def q_euler(deg):
    return Euler([math.radians(a) for a in deg], 'XYZ').to_quaternion()


def solve(pose):
    """pose: {bone: {dir:(x,y,z), rot:(deg xyz), loc:(x,y,z)}}.
    'dir' = bone direction in body space (armature axes rotated by the Hips pose rotation);
    'rot' = extra rotation in body axes. Returns {bone: (basis_quat, loc or None)}."""
    world = {}
    out = {}
    body_q = Quaternion()
    for b in ORDER:
        rest = b.matrix_local.to_quaternion()
        if b.parent:
            m0 = world[b.parent.name] @ b.parent.matrix_local.to_quaternion().inverted() @ rest
        else:
            m0 = rest.copy()
        spec = pose.get(b.name, {})
        rw = Quaternion()
        if "dir" in spec:
            want = body_q @ Vector(spec["dir"]).normalized()
            cur = m0 @ Vector((0, 1, 0))
            rw = cur.rotation_difference(want)
            if b.name in CHAINS:
                child, bend_rest = CHAINS[b.name]
                cspec = pose.get(child, {})
                if "dir" in cspec:
                    du = want
                    dl = body_q @ Vector(cspec["dir"]).normalized()
                    p = dl - du * dl.dot(du)
                    if p.length > 1e-3:
                        local_bend = rest.inverted() @ bend_rest
                        b0 = (rw @ m0) @ local_bend
                        b0 = b0 - du * b0.dot(du)
                        phi = math.atan2(du.dot(b0.cross(p)), b0.dot(p))
                        rw = Quaternion(du, phi) @ rw
        if "rot" in spec:
            rq = body_q @ q_euler(spec["rot"]) @ body_q.inverted()
            if b.parent is None:
                rq = q_euler(spec["rot"])
            rw = rq @ rw
        posed = rw @ m0
        world[b.name] = posed
        basis = m0.inverted() @ posed
        loc = None
        if "loc" in spec:
            loc = rest.inverted() @ (Vector(spec["loc"]) * S)
        out[b.name] = (basis, loc)
        if b.parent is None:
            body_q = rw
    return out


def mirror(pose):
    m = {}
    for name, spec in pose.items():
        n = name.replace("Left", "#").replace("Right", "Left").replace("#", "Right")
        ns = {}
        for k, v in spec.items():
            if k == "dir" or k == "loc":
                ns[k] = (-v[0], v[1], v[2])
            elif k == "rot":
                ns[k] = (v[0], -v[1], -v[2])
        m[n] = ns
    return m


def merge(*poses):
    out = {}
    for p in poses:
        for k, v in p.items():
            out.setdefault(k, {}).update(v)
    return out


def arms(lu, ll, ru, rl, lsh=(1, 0, 0), rsh=(1, 0, 0)):
    """Arm dirs with outward-positive X for both sides."""
    return {
        "LeftShoulder": {"dir": lsh}, "RightShoulder": {"dir": (-rsh[0], rsh[1], rsh[2])},
        "LeftUpperArm": {"dir": lu}, "LeftLowerArm": {"dir": ll},
        "RightUpperArm": {"dir": (-ru[0], ru[1], ru[2])}, "RightLowerArm": {"dir": (-rl[0], rl[1], rl[2])},
    }


def legs(lu, ll, ru, rl, lf=(0, -0.75, -0.6), rf=(0, -0.75, -0.6)):
    """Leg dirs with outward-positive X for both sides."""
    return {
        "LeftUpperLeg": {"dir": lu}, "LeftLowerLeg": {"dir": ll}, "LeftFoot": {"dir": lf},
        "RightUpperLeg": {"dir": (-ru[0], ru[1], ru[2])}, "RightLowerLeg": {"dir": (-rl[0], rl[1], rl[2])},
        "RightFoot": {"dir": (-rf[0], rf[1], rf[2])},
    }


def body(hips_rot=(0, 0, 0), hips_loc=(0, 0, 0), spine=(0, 0, 0), chest=(0, 0, 0), neck=(0, 0, 0), head=(0, 0, 0)):
    return {"Hips": {"rot": hips_rot, "loc": hips_loc}, "Spine": {"rot": spine}, "Chest": {"rot": chest},
            "Neck": {"rot": neck}, "Head": {"rot": head}}


# =============================================================================== pose library
# Body space: X = character's left, Y = back, Z = up, -Y = forward (towards the tower wall).
DANGLE_FOOT = (0, -0.55, -0.85)

HANG = merge(
    body(head=(-14, 0, 0), neck=(-4, 0, 0)),
    arms((0.32, -0.42, 1.0), (0.12, -0.5, 1.0), (0.32, -0.42, 1.0), (0.12, -0.5, 1.0),
         lsh=(1, 0, 0.32), rsh=(1, 0, 0.32)),
    legs((0.06, -0.14, -1), (0.0, 0.14, -1), (0.06, -0.14, -1), (0.0, 0.14, -1), DANGLE_FOOT, DANGLE_FOOT),
)
HANG_SWAY_L = merge(HANG, body(hips_rot=(0, -4, 0), hips_loc=(0, 0, 0.01), spine=(0, 2, 0), head=(-14, 0, 3)),
                    legs((0.02, -0.20, -1), (-0.02, 0.10, -1), (0.10, -0.06, -1), (0.0, 0.2, -1),
                         DANGLE_FOOT, DANGLE_FOOT))
HANG_SWAY_R = mirror(HANG_SWAY_L)

CLIMB_A = merge(
    body(hips_rot=(0, 5, 0), hips_loc=(0, 0, -0.02), spine=(4, -8, 0), head=(-18, 0, 5)),
    arms((0.18, -0.38, 1.0), (0.06, -0.32, 1.0),        # left: reaching high
         (0.82, -0.40, -0.18), (0.08, -0.55, 1.0),     # right: pulling, hand at head height
         lsh=(1, 0, 0.38), rsh=(1, 0, 0.05)),
    legs((0.04, 0.06, -1), (0.0, 0.25, -1),             # left: extended, pushing
         (0.10, -0.85, -0.5), (0.0, 0.25, -1),          # right: knee up towards the wall
         (0, -0.35, -0.9), (0, -0.8, -0.4)),
)
CLIMB_B = mirror(CLIMB_A)

FALL_0 = merge(
    body(hips_rot=(-10, 0, 0), spine=(-12, 0, 0), head=(-22, 0, 0)),
    arms((0.55, 0.15, 1.0), (0.35, 0.05, 1.0), (1.0, 0.15, -0.5), (1.0, -0.35, -0.1),
         lsh=(1, 0, 0.3), rsh=(1, 0, 0)),
    legs((0.18, -0.8, -0.6), (0.08, 0.3, -1), (0.15, 0.35, -1), (0.05, 0.55, -1),
         (0, -0.6, -0.8), (0, -0.4, -0.9)),
)
FALL_1 = merge(
    body(hips_rot=(-10, 0, 0), spine=(-12, 0, 6), head=(-18, 0, 8)),
    arms((1.0, 0.35, 0.15), (1.0, 0.1, 0.6), (1.0, -0.4, 0.3), (0.8, -0.6, 0.5),
         lsh=(1, 0, 0.15), rsh=(1, 0, 0.15)),
    legs((0.15, -0.2, -1), (0.05, 0.5, -1), (0.18, -0.5, -0.9), (0.05, 0.4, -1),
         (0, -0.5, -0.85), (0, -0.5, -0.85)),
)
FALL_2 = mirror(FALL_0)
FALL_3 = mirror(FALL_1)

HIT_A = merge(
    body(hips_rot=(-8, 0, 8), hips_loc=(0, 0.05, 0.03), spine=(-14, 0, 10), chest=(-6, 0, 0),
         neck=(-6, 0, 0), head=(-24, 0, -14)),
    arms((1.0, 0.5, 0.55), (1.0, 0.65, 0.9), (1.0, 0.6, 0.25), (0.9, 0.7, 0.7),
         lsh=(1, 0, 0.35), rsh=(1, 0, 0.3)),
    legs((0.3, -0.6, -0.8), (0.1, 0.15, -1), (0.3, 0.15, -1), (0.1, 0.4, -1),
         (0, -0.5, -0.85), (0, -0.4, -0.9)),
)
HIT_B = merge(
    body(hips_rot=(-12, 0, -6), hips_loc=(0, 0.07, 0.04), spine=(-16, 0, -8), chest=(-6, 0, 0),
         neck=(-6, 0, 0), head=(-22, 0, 12)),
    arms((0.9, 0.5, 0.9), (0.6, 0.6, 1.0), (1.0, 0.5, 0.75), (0.7, 0.6, 1.0),
         lsh=(1, 0, 0.35), rsh=(1, 0, 0.35)),
    legs((0.25, -0.9, -0.5), (0.1, 0.0, -1), (0.35, -0.3, -1), (0.1, 0.3, -1),
         (0, -0.6, -0.8), (0, -0.6, -0.8)),
)

CARRY_ARMS = arms((0.12, -0.05, 1.0), (0.07, -0.05, 1.0), (0.12, -0.05, 1.0), (0.07, -0.05, 1.0),
                  lsh=(1, 0, 0.36), rsh=(1, 0, 0.36))
CARRY_0 = merge(body(hips_rot=(6, 0, 0), spine=(2, 0, 0), head=(-6, 0, 0)), CARRY_ARMS,
                legs((0.07, -0.5, -1), (0.0, -0.1, -1), (0.07, -0.42, -1), (0.0, 0.05, -1),
                     (0, -0.5, -0.85), (0, -0.5, -0.85)))
CARRY_1 = merge(body(hips_rot=(-6, 0, 0), spine=(-3, 0, 0), head=(-10, 0, 0)), CARRY_ARMS,
                legs((0.07, 0.3, -1), (0.0, 0.8, -1), (0.07, 0.22, -1), (0.0, 0.65, -1),
                     (0, -0.4, -0.9), (0, -0.4, -0.9)))

SHIFT_L = merge(
    body(hips_rot=(0, 16, 0), spine=(0, -6, 0), head=(-12, 0, 0)),
    arms((0.85, -0.35, 0.65), (0.75, -0.45, 0.55),        # left arm reaches out to the new grip
         (0.05, -0.45, 1.0), (-0.05, -0.5, 1.0),          # right arm still holding
         lsh=(1, 0, 0.32), rsh=(1, 0, 0.36)),
    legs((-0.05, -0.45, -1), (-0.1, 0.55, -1), (0.15, -0.35, -1), (0.1, 0.65, -1),
         (0, -0.6, -0.8), (0, -0.6, -0.8)),
)
SHIFT_R = mirror(SHIFT_L)

WIN_PULL = merge(
    body(hips_loc=(0, -0.02, 0.07), spine=(16, 0, 0), head=(-8, 0, 0)),
    arms((0.75, -0.55, -0.35), (0.12, -0.65, 1.0), (0.75, -0.55, -0.35), (0.12, -0.65, 1.0),
         lsh=(1, 0, 0.1), rsh=(1, 0, 0.1)),
    legs((0.1, -0.85, -0.5), (0.0, 0.35, -1), (0.1, -0.7, -0.7), (0.0, 0.35, -1)),
)
STAND = merge(
    body(),
    arms((0.32, 0.0, -1.0), (0.22, -0.25, -1.0), (0.32, 0.0, -1.0), (0.22, -0.25, -1.0)),
    legs((0.07, 0.0, -1), (0.02, 0.02, -1), (0.07, 0.0, -1), (0.02, 0.02, -1), (0, -1, -0.25), (0, -1, -0.25)),
)
TURN_HOP = merge(STAND, body(hips_rot=(0, 0, 90), hips_loc=(0, 0, 0.06)),
                 legs((0.08, -0.35, -1), (0.0, 0.45, -1), (0.08, -0.35, -1), (0.0, 0.45, -1)))
FIST_L = {"LeftUpperArm": {"dir": (0.75, -0.05, -0.75)}, "LeftLowerArm": {"dir": (0.2, -0.95, 0.35)}}
CROUCH = merge(
    body(hips_rot=(0, 0, 180), hips_loc=(0, 0, -0.07), spine=(12, 0, 0), head=(4, 0, 0)),
    arms((0.55, -0.35, -0.8), (0.1, -1.0, 0.35), (0.55, -0.35, -0.8), (0.1, -1.0, 0.35)),
    legs((0.12, -0.45, -1), (0.02, 0.5, -1), (0.12, -0.45, -1), (0.02, 0.5, -1), (0, -1, -0.25), (0, -1, -0.25)),
)
CHEER = merge(
    body(hips_rot=(0, 0, 180), hips_loc=(0, 0, 0.09), spine=(-10, 4, 0), head=(-14, 0, -6)),
    arms((0.75, -0.05, -0.75), (0.2, -0.95, 0.35), (0.18, -0.08, 1.0), (0.1, -0.05, 1.0),
         lsh=(1, 0, 0), rsh=(1, 0, 0.36)),
    legs((0.22, -0.05, -1), (0.1, 0.1, -1), (0.22, 0.05, -1), (0.1, 0.15, -1), (0, -1, -0.5), (0, -1, -0.5)),
)
CHEER_DOWN = merge(CHEER, body(hips_rot=(0, 0, 180), hips_loc=(0, 0, -0.02), spine=(-4, 2, 0), head=(-8, 0, -4)),
                   {"RightUpperArm": {"dir": (-0.6, -0.15, 0.75)}, "RightLowerArm": {"dir": (-0.05, -0.45, 1.0)}},
                   legs((0.2, -0.2, -1), (0.08, 0.3, -1), (0.2, -0.15, -1), (0.08, 0.3, -1)))
CHEER_END = merge(CHEER, body(hips_rot=(0, 0, 180), hips_loc=(0, 0, 0.0), spine=(-8, 3, 0), head=(-12, 0, -6)),
                  legs((0.2, -0.02, -1), (0.08, 0.06, -1), (0.2, 0.02, -1), (0.08, 0.06, -1),
                       (0, -1, -0.25), (0, -1, -0.25)))

LOSE_SLIP = merge(
    body(hips_rot=(-6, 0, 0), hips_loc=(0, 0.02, -0.07), spine=(-8, 0, 0), head=(-28, 0, 0)),
    arms((0.42, -0.15, 1.0), (0.3, -0.05, 1.0), (0.42, -0.15, 1.0), (0.3, -0.05, 1.0),
         lsh=(1, 0, 0.36), rsh=(1, 0, 0.36)),
    legs((0.3, -0.25, -1), (0.12, 0.25, -1), (0.3, -0.1, -1), (0.12, 0.3, -1)),
)
LOSE_DISMAY = merge(
    body(hips_rot=(-5, 0, 0), hips_loc=(0, 0.03, -0.04), spine=(-8, 0, 4), neck=(-6, 0, 0), head=(-22, 0, 8)),
    arms((0.75, 0.15, 1.0), (0.55, 0.3, 1.0), (0.85, 0.1, 0.9), (0.65, 0.35, 1.0),
         lsh=(1, 0, 0.36), rsh=(1, 0, 0.36)),
    legs((0.32, -0.55, -1), (0.15, 0.35, -1), (0.25, 0.15, -1), (0.1, 0.55, -1),
         (0, -0.6, -0.8), (0, -0.5, -0.85)),
)
LOSE_DISMAY_2 = merge(mirror(LOSE_DISMAY), body(hips_rot=(-5, 0, 0), hips_loc=(0, 0.03, -0.05),
                                                 spine=(-8, 0, -4), neck=(-6, 0, 0), head=(-22, 0, -8)))

# =============================================================================== clips
CLIPS = [
    # name, loop, end frame, keys [(frame, pose, only)]
    ("Hero_ClimbUp", True, 24, [(0, CLIMB_A, None), (6, merge(CLIMB_A, body(hips_loc=(0, 0, 0.035))), {"Hips"}),
                                (12, CLIMB_B, None), (18, merge(CLIMB_B, body(hips_loc=(0, 0, 0.035))), {"Hips"}),
                                (24, CLIMB_A, None)]),
    ("Hero_HangIdle", True, 60, [(0, HANG_SWAY_L, None), (30, HANG_SWAY_R, None), (60, HANG_SWAY_L, None)]),
    ("Hero_ShiftLeft", False, 8, [(0, HANG, None), (3, SHIFT_L, None), (8, HANG, None)]),
    ("Hero_ShiftRight", False, 8, [(0, HANG, None), (3, SHIFT_R, None), (8, HANG, None)]),
    ("Hero_Hit", False, 12, [(0, HANG, None), (2, HIT_A, None), (6, HIT_B, None), (12, FALL_0, None)]),
    ("Hero_Fall", True, 20, [(0, FALL_0, None), (5, FALL_1, None), (10, FALL_2, None), (15, FALL_3, None),
                             (20, FALL_0, None)]),
    ("Hero_Carried", True, 30, [(0, CARRY_0, None), (15, CARRY_1, None), (30, CARRY_0, None)]),
    ("Hero_Win", False, 45, [(0, HANG, None), (8, WIN_PULL, None), (15, STAND, None), (20, TURN_HOP, None),
                             (26, merge(STAND, body(hips_rot=(0, 0, 180))), None), (30, CROUCH, None),
                             (34, CHEER, None), (39, CHEER_DOWN, None), (43, CHEER, None), (45, CHEER_END, None)]),
    ("Hero_Lose", False, 36, [(0, HANG, None), (4, LOSE_SLIP, None), (10, LOSE_DISMAY, None),
                              (20, LOSE_DISMAY_2, None), (28, LOSE_DISMAY, None), (36, LOSE_DISMAY, None)]),
]

rig.animation_data_create()
actions = []
for name, loop, end, keys in CLIPS:
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    last = {}
    for frame, pose, only in keys:
        solved = solve(pose)
        for pb in rig.pose.bones:
            if only is not None and pb.name not in only:
                continue
            q, loc = solved[pb.name]
            prev = last.get(pb.name)
            if prev is not None and prev.dot(q) < 0:
                q = -q
            last[pb.name] = q.copy()
            pb.rotation_quaternion = q
            pb.keyframe_insert("rotation_quaternion", frame=frame, group=pb.name)
            if pb.name == "Hips":
                pb.location = loc if loc is not None else Vector()
                pb.keyframe_insert("location", frame=frame, group=pb.name)
    act.use_frame_range = True
    act.frame_start = 0
    act.frame_end = end
    act.use_cyclic = loop
    actions.append(act)
    print("CLIP", name, "frames 0-%d" % end, "loop" if loop else "once")

# Reset pose to rest, put every clip on its own NLA track (strip name = FBX take name).
ad = rig.animation_data
ad.action = None
for pb in rig.pose.bones:
    pb.rotation_quaternion = Quaternion()
    pb.location = Vector()
for act in actions:
    track = ad.nla_tracks.new()
    track.name = act.name
    strip = track.strips.new(act.name, 0, act)
    strip.name = act.name
    if act.slots:
        strip.action_slot = act.slots[0]
scene.frame_start = 0
scene.frame_end = 60

# =============================================================================== export
os.makedirs(os.path.join(OUT_DIR, "Textures"), exist_ok=True)
shutil.copyfile(TEX_SRC, TEX_OUT)

common = dict(use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
              axis_forward='-Z', axis_up='Y', bake_space_transform=False, add_leaf_bones=False,
              primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=False,
              armature_nodetype='NULL', path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE',
              use_mesh_modifiers=False)


def select(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


# 1) Mesh + rig in T-pose (no animation).
ad.use_nla = False
scene.frame_set(0)
select([rig, body_ob])
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, "Hero.fbx"), object_types={'ARMATURE', 'MESH'},
                         bake_anim=False, **common)

# 2) Rig + all clips as separate takes (one per NLA strip).
ad.use_nla = True
select([rig])
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, "Hero_Animations.fbx"), object_types={'ARMATURE'},
                         bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=True,
                         bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
                         bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **common)

# Leave the .blend tidy: NLA evaluation off so a single action can be previewed.
ad.use_nla = False
bpy.context.preferences.filepaths.save_version = 0  # no hero.blend1 backups
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC_DIR, "hero.blend"))
print("BUILD_OK")
