"""Shared prop helpers: palette atlas material, parametric primitives, finalize/export.

All props share ONE material `M_Props` with a 256^2 palette texture `T_PropPalette.png`
(8x8 swatches, 32 px each). RGB = sRGB albedo, A = smoothness (URP Lit: Smoothness Source = Albedo Alpha).
Modelling uses temporary per-swatch materials; finalize() collapses each face's UVs onto its swatch centre.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "Common"))
import gtlib as gt  # noqa: E402

OUT = gt.PROJECT + "/Assets/Art/Props"
TEX = OUT + "/Textures"
PALETTE_PATH = TEX + "/T_PropPalette.png"
PREV = gt.PROJECT + "/Art/Source/Environment/Previews"

# name: (sRGB 0-255, smoothness 0-1)
PALETTE = [
    ("red", (226, 32, 38), 0.85),
    ("red_dark", (150, 18, 26), 0.70),
    ("white", (245, 245, 240), 0.55),
    ("grey", (196, 200, 208), 0.45),
    ("dark", (52, 54, 64), 0.30),
    ("black", (28, 28, 32), 0.25),
    ("metal", (196, 204, 214), 0.80),
    ("metal_dark", (112, 120, 132), 0.65),
    ("wood", (156, 98, 54), 0.25),
    ("wood_dark", (110, 66, 36), 0.20),
    ("gold", (255, 196, 40), 0.90),
    ("gold_dark", (214, 140, 18), 0.80),
    ("glass", (120, 200, 245), 0.95),
    ("yellow", (255, 210, 40), 0.55),
    ("orange", (255, 128, 24), 0.55),
    ("blue", (40, 112, 226), 0.55),
    ("blue_dark", (24, 64, 150), 0.50),
    ("green", (70, 192, 90), 0.50),
    ("fire_yellow", (255, 236, 90), 0.15),
    ("fire_orange", (255, 150, 30), 0.15),
    ("fire_red", (236, 60, 30), 0.15),
    ("fire_deep", (180, 30, 40), 0.15),
    ("teal", (40, 200, 200), 0.50),
    ("cream", (250, 236, 200), 0.45),
    ("pink", (255, 120, 150), 0.50),
    ("purple", (140, 70, 200), 0.50),
    ("rubber", (44, 46, 52), 0.15),
    ("chrome", (230, 236, 244), 0.95),
    ("brown", (120, 80, 50), 0.30),
    ("skin", (240, 190, 150), 0.30),
    ("lightblue", (150, 210, 255), 0.55),
    ("navy", (30, 40, 80), 0.40),
]
GRID = 8
SW = 32
INDEX = {name: i for i, (name, _c, _s) in enumerate(PALETTE)}


def swatch_uv(name):
    i = INDEX[name]
    return ((i % GRID + 0.5) / GRID, 1.0 - (i // GRID + 0.5) / GRID)


def write_palette():
    os.makedirs(TEX, exist_ok=True)
    size = GRID * SW
    img = bpy.data.images.new("T_PropPalette", size, size, alpha=True)
    px = [0.0] * (size * size * 4)
    for i, (_n, c, s) in enumerate(PALETTE):
        gx, gy = i % GRID, i // GRID
        for y in range(SW):
            row = size - 1 - (gy * SW + y)       # image rows start at the bottom
            for x in range(SW):
                k = (row * size + gx * SW + x) * 4
                px[k:k + 4] = [c[0] / 255, c[1] / 255, c[2] / 255, s]
    img.pixels = px
    img.filepath_raw = PALETTE_PATH
    img.file_format = 'PNG'
    img.save()
    return img


# ------------------------------------------------------------------ temp materials

_TMP = {}


def tmat(name):
    if name not in _TMP:
        m = bpy.data.materials.new("tmp_" + name)
        c = next(c for n, c, s in PALETTE if n == name)
        m.diffuse_color = (c[0] / 255, c[1] / 255, c[2] / 255, 1)
        _TMP[name] = m
    return _TMP[name]


def paint(obj, name):
    obj.data.materials.clear()
    obj.data.materials.append(tmat(name))
    return obj


def paint_faces(obj, name, pred):
    """Assign swatch `name` to faces whose centre satisfies pred(center, normal)."""
    me = obj.data
    m = tmat(name)
    if m.name not in [x.name for x in me.materials]:
        me.materials.append(m)
    idx = [x.name for x in me.materials].index(m.name)
    for p in me.polygons:
        if pred(p.center, p.normal):
            p.material_index = idx


# ------------------------------------------------------------------ primitives

def _sc(w, e):
    c, s = math.cos(w), math.sin(w)
    return (math.copysign(abs(c) ** e, c), math.copysign(abs(s) ** e, s))


def superellipsoid(name, radii, nu=16, nv=10, e1=1.0, e2=1.0, matrix=None, deform=None, color="white"):
    """Poles along local Z. e<1 = boxier. deform(Vector)->Vector applied after matrix (object space)."""
    rx, ry, rz = radii
    verts, faces = [], []
    verts.append(Vector((0, 0, -rz)))
    for j in range(1, nv):
        v = -math.pi / 2 + math.pi * j / nv
        cv, sv = _sc(v, e1)
        for i in range(nu):
            u = -math.pi + 2 * math.pi * i / nu
            cu, su = _sc(u, e2)
            verts.append(Vector((rx * cv * cu, ry * cv * su, rz * sv)))
    verts.append(Vector((0, 0, rz)))
    top = len(verts) - 1

    def vi(j, i):
        return 1 + (j - 1) * nu + (i % nu)
    for i in range(nu):
        faces.append((0, vi(1, i + 1), vi(1, i)))
    for j in range(1, nv - 1):
        for i in range(nu):
            faces.append((vi(j, i), vi(j, i + 1), vi(j + 1, i + 1), vi(j + 1, i)))
    for i in range(nu):
        faces.append((vi(nv - 1, i), vi(nv - 1, i + 1), top))
    if matrix is not None:
        verts = [matrix @ v for v in verts]
    if deform:
        verts = [deform(Vector(v)) for v in verts]
    obj = gt.mesh_object(name, verts, faces)
    return paint(obj, color)


def tube(name, path, radius, sides=8, cap=True, color="white", radius_fn=None, scale_xy=(1, 1), up=None):
    """Sweep a circle along a polyline path (list of Vectors). radius_fn(t) overrides radius."""
    verts, faces = [], []
    n = len(path)
    up_ref = Vector(up) if up else Vector((0, 0, 1))
    for k, p in enumerate(path):
        if k == 0:
            t = (path[1] - path[0]).normalized()
        elif k == n - 1:
            t = (path[-1] - path[-2]).normalized()
        else:
            t = (path[k + 1] - path[k - 1]).normalized()
        ref = up_ref if abs(t.dot(up_ref)) < 0.95 else Vector((1, 0, 0))
        b1 = t.cross(ref).normalized()
        b2 = t.cross(b1).normalized()
        r = radius_fn(k / (n - 1)) if radius_fn else radius
        for i in range(sides):
            a = 2 * math.pi * i / sides
            verts.append(p + b1 * math.cos(a) * r * scale_xy[0] + b2 * math.sin(a) * r * scale_xy[1])
    for k in range(n - 1):
        for i in range(sides):
            a, b = k * sides + i, k * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    if cap:
        verts.append(path[0])
        c0 = len(verts) - 1
        verts.append(path[-1])
        c1 = len(verts) - 1
        for i in range(sides):
            faces.append((c0, (i + 1) % sides, i))
            faces.append((c1, (n - 1) * sides + i, (n - 1) * sides + (i + 1) % sides))
    obj = gt.mesh_object(name, verts, faces)
    return paint(obj, color)


def box(name, size, loc=(0, 0, 0), bevel=0.0, segments=1, color="white", rot=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    gt.link(obj)
    if bevel > 0:
        m = obj.modifiers.new("bev", 'BEVEL')
        m.width = bevel
        m.segments = segments
        m.limit_method = 'NONE'
        gt.apply_modifiers(obj)
    if rot:
        obj.rotation_euler = rot
    obj.location = loc
    gt.apply_transform(obj)
    for p in obj.data.polygons:
        p.use_smooth = True
    return paint(obj, color)


def cylinder(name, r, depth, sides=12, loc=(0, 0, 0), rot=None, color="white", r2=None, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=sides,
                          radius1=r, radius2=r if r2 is None else r2, depth=depth)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    gt.link(obj)
    if bevel > 0:
        m = obj.modifiers.new("bev", 'BEVEL')
        m.width = bevel
        m.segments = 2
        m.limit_method = 'ANGLE'
        gt.apply_modifiers(obj)
    if rot:
        obj.rotation_euler = rot
    obj.location = loc
    gt.apply_transform(obj)
    return paint(obj, color)


def torus(name, R, r, nu=16, nv=6, loc=(0, 0, 0), rot=None, color="white", scale=(1, 1, 1)):
    verts, faces = [], []
    for i in range(nu):
        a = 2 * math.pi * i / nu
        for j in range(nv):
            b = 2 * math.pi * j / nv
            verts.append(Vector(((R + r * math.cos(b)) * math.cos(a) * scale[0],
                                 (R + r * math.cos(b)) * math.sin(a) * scale[1],
                                 r * math.sin(b) * scale[2])))
    for i in range(nu):
        for j in range(nv):
            a = i * nv + j
            b = ((i + 1) % nu) * nv + j
            c = ((i + 1) % nu) * nv + (j + 1) % nv
            d = i * nv + (j + 1) % nv
            faces.append((a, b, c, d))
    obj = gt.mesh_object(name, verts, faces)
    if rot:
        obj.rotation_euler = rot
    obj.location = loc
    gt.apply_transform(obj)
    return paint(obj, color)


def lathe_part(name, profile, sides=16, color="white", matrix=None):
    """profile: list of (z, r). Revolved around local Z; r=0 points become poles."""
    obj = gt.lathe(name, [(z, r) for z, r in profile], sides, lambda f, i: (0.5, 0.5))
    if matrix is not None:
        obj.data.transform(matrix)
    return paint(obj, color)


def mirror_x(obj, name=None):
    o2 = obj.copy()
    o2.data = obj.data.copy()
    o2.name = name or obj.name + "_R"
    gt.link(o2)
    o2.data.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
    o2.data.flip_normals()
    return o2


# ------------------------------------------------------------------ finalize

def prop_material():
    m = bpy.data.materials.get("M_Props")
    if m:
        return m
    m = gt.material("M_Props", albedo=PALETTE_PATH, roughness=0.4)
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type == 'TEX_IMAGE':
            n.interpolation = 'Closest'     # flat swatches: no filtering bleed
            inv = nt.nodes.new("ShaderNodeMath")
            inv.operation = 'SUBTRACT'
            inv.inputs[0].default_value = 1.0
            nt.links.new(n.outputs["Alpha"], inv.inputs[1])     # alpha = smoothness -> roughness
            nt.links.new(inv.outputs[0], nt.nodes["Principled BSDF"].inputs["Roughness"])
    return m


def finalize(parts, name, smooth_angle=50, keep_sharp=True):
    """Join parts, collapse UVs to swatches, single material M_Props."""
    obj = gt.join(parts, name) if len(parts) > 1 else parts[0]
    obj.name = name
    obj.data.name = name
    me = obj.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    while len(me.uv_layers) > 1:
        me.uv_layers.remove(me.uv_layers[1])
    uvl = me.uv_layers[0]
    me.uv_layers.active = uvl
    mats = [m.name[4:] if m and m.name.startswith("tmp_") else "white" for m in me.materials]
    for p in me.polygons:
        uv = swatch_uv(mats[p.material_index] if p.material_index < len(mats) else "white")
        for li in p.loop_indices:
            uvl.data[li].uv = uv
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-6)
    ngons = [f for f in bm.faces if len(f.verts) > 4]
    if ngons:
        bmesh.ops.triangulate(bm, faces=ngons, quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(me)
    bm.free()
    me.materials.clear()
    me.materials.append(prop_material())
    gt.smooth_by_angle(obj, smooth_angle)
    return obj


def export(obj_list, fname, anim=False):
    gt.export_fbx(f"{OUT}/{fname}.fbx", obj_list, anim=anim)


def start():
    gt.reset_scene()
    _TMP.clear()
    write_palette()


def preview(name, target, dist=3.0, cam_dir=(0.8, -1.0, 0.45), res=(800, 800), lens=50):
    gt.preview(f"{PREV}/{name}.png", target=target, cam_dir=cam_dir, dist=dist, res=res, lens=lens)


def extruded_poly(name, pts, thickness, matrix=None, color="white", bevel=0.0, edge_taper=None):
    """Flat polygon in local XY (list of (x, y)), extruded symmetrically along local Z.
    edge_taper(x, y) -> scale (0..1) of the half-thickness at that outline point (for sharp blade edges)."""
    n = len(pts)
    verts, faces = [], []
    for z in (-0.5, 0.5):
        for (x, y) in pts:
            k = edge_taper(x, y) if edge_taper else 1.0
            verts.append(Vector((x, y, z * thickness * k)))
    faces.append(tuple(reversed(range(n))))
    faces.append(tuple(range(n, 2 * n)))
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    obj = gt.mesh_object(name, verts, faces)
    if bevel > 0:
        m = obj.modifiers.new("bev", 'BEVEL')
        m.width = bevel
        m.segments = 1
        m.limit_method = 'ANGLE'
        gt.apply_modifiers(obj)
    if matrix is not None:
        obj.data.transform(matrix)
    # triangulate concave caps cleanly
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    big = [f for f in bm.faces if len(f.verts) > 4]
    if big:
        bmesh.ops.triangulate(bm, faces=big, quad_method='BEAUTY', ngon_method='EAR_CLIP')
    bm.normal_update()
    bm.to_mesh(obj.data)
    bm.free()
    for p in obj.data.polygons:
        p.use_smooth = False
    return paint(obj, color)


def paint_by(obj, fn):
    """fn(center, normal) -> swatch name or None (keep)."""
    me = obj.data
    names = [m.name[4:] for m in me.materials]
    for p in me.polygons:
        nm = fn(p.center, p.normal)
        if nm is None:
            continue
        if nm not in names:
            me.materials.append(tmat(nm))
            names.append(nm)
        p.material_index = names.index(nm)
