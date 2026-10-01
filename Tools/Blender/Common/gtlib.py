"""Shared Blender helpers for God Tower asset generators (run inside Blender 5.x).

Conventions:
- Blender units = metres, Z up. Models face Blender -Y (front view).
- FBX export: forward -Z, up Y, bake_space_transform -> Unity: Y up, model faces +Z, rotation (0,0,0), scale 1.
"""
import math
import os

import bmesh
import bpy
from mathutils import Vector

PROJECT = "D:/Unity/Projects/GodTower"
PREVIEWS = PROJECT + "/Art/Source/Environment/Previews"


# ---------------------------------------------------------------- scene

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = 'METRIC'
    scn.unit_settings.scale_length = 1.0
    return scn


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def mesh_object(name, verts, faces, uvs=None, smooth=True):
    """verts: list of (x,y,z); faces: list of index tuples; uvs: per-face list of (u,v) tuples."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
    if uvs is not None:
        uvl = me.uv_layers.new(name="UVMap")
        li = 0
        for poly, fuv in zip(me.polygons, uvs):
            for k, loop_index in enumerate(poly.loop_indices):
                uvl.data[loop_index].uv = fuv[k]
    me.validate()
    me.update()
    obj = bpy.data.objects.new(name, me)
    link(obj)
    for p in me.polygons:
        p.use_smooth = smooth
    return obj


def select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def smooth_by_angle(obj, angle_deg=35.0):
    select_only([obj])
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle_deg), keep_sharp_edges=True)


def apply_modifiers(obj):
    select_only([obj])
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)


def join(objs, name):
    select_only(objs)
    with bpy.context.temp_override(active_object=objs[0], object=objs[0],
                                   selected_objects=objs, selected_editable_objects=objs):
        bpy.ops.object.join()
    o = objs[0]
    o.name = name
    o.data.name = name
    return o


def apply_transform(obj):
    select_only([obj])
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def tri_count(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    n = sum(len(p.vertices) - 2 for p in me.polygons)
    ev.to_mesh_clear()
    return n


def triangulate(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(obj.data)
    bm.free()


# ---------------------------------------------------------------- lathe

def lathe(name, profile, sides, uv_fn, sectors=1, rib=None, phase=0.0):
    """Revolve a profile around Z.

    profile: list of (z, r) points bottom->top (or any order). Each point may carry a 3rd item: rib amplitude.
    uv_fn(sector_frac, point_index) -> (u, v): sector_frac in [0,1] across one sector.
    sectors: number of angular UV islands (texture repeats `sectors` times around).
    rib: optional function(g) -> factor in [0,1] used with per-point rib amplitude (radial ribs/flutes).
    """
    n = len(profile)
    verts = []
    for g in range(sides):
        a = phase + 2.0 * math.pi * g / sides
        ca, sa = math.cos(a), math.sin(a)
        for i, p in enumerate(profile):
            z, r = p[0], p[1]
            if rib is not None and len(p) > 2 and p[2]:
                r = r + p[2] * rib(g)
            verts.append((r * ca, r * sa, z))
    faces, uvs = [], []
    per = sides // sectors
    for g in range(sides):
        g2 = (g + 1) % sides
        s = g // per
        f0 = (g - s * per) / per
        f1 = (g + 1 - s * per) / per
        for i in range(n - 1):
            a = g * n + i
            b = g2 * n + i
            c = g2 * n + i + 1
            d = g * n + i + 1
            faces.append((a, b, c, d))
            uvs.append((uv_fn(f0, i), uv_fn(f1, i), uv_fn(f1, i + 1), uv_fn(f0, i + 1)))
    obj = mesh_object(name, verts, faces, uvs)
    # merge degenerate (r == 0) rings
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=1e-6)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


# ---------------------------------------------------------------- materials

def image(path, non_color=False):
    img = bpy.data.images.load(path, check_existing=True)
    if non_color:
        img.colorspace_settings.name = 'Non-Color'
    return img


def material(name, albedo=None, normal=None, color=(0.8, 0.8, 0.8, 1.0), roughness=0.7,
             metallic=0.0, normal_strength=1.0, emission=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Base Color"].default_value = color
    if albedo:
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = image(albedo)
        tex.location = (-500, 200)
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if normal:
        tn = nt.nodes.new("ShaderNodeTexImage")
        tn.image = image(normal, non_color=True)
        tn.location = (-700, -250)
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nm.location = (-350, -250)
        nm.inputs["Strength"].default_value = normal_strength
        nt.links.new(tn.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    if emission:
        bsdf.inputs["Emission Color"].default_value = emission[0]
        bsdf.inputs["Emission Strength"].default_value = emission[1]
    return mat


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


# ---------------------------------------------------------------- export

def export_fbx(path, objs, anim=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    select_only(objs)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={'MESH', 'ARMATURE', 'EMPTY'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        mesh_smooth_type='OFF',
        use_mesh_modifiers=True,
        use_tspace=True,
        add_leaf_bones=False,
        path_mode='RELATIVE',
        embed_textures=False,
        bake_anim=anim,
        bake_anim_use_all_actions=anim,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
    )


# ---------------------------------------------------------------- preview

def preview(path, target=(0, 0, 1), cam_dir=(0.0, -1.0, 0.25), dist=10.0, ortho=None,
            res=(720, 1080), lens=50, sky=(0.18, 0.45, 0.95)):
    """Render a bright Eevee preview. cam_dir: direction from target to camera."""
    scn = bpy.context.scene
    scn.render.engine = 'BLENDER_EEVEE'
    scn.render.resolution_x, scn.render.resolution_y = res
    scn.render.film_transparent = False
    scn.view_settings.view_transform = 'Standard'
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    nt = world.node_tree
    bg = nt.nodes["Background"]
    bg.inputs["Color"].default_value = (0.62, 0.68, 0.78, 1)   # neutral-cool ambient for lighting
    bg.inputs["Strength"].default_value = 0.85
    cam_bg = nt.nodes.new("ShaderNodeBackground")
    cam_bg.inputs["Color"].default_value = (*sky, 1)
    lp = nt.nodes.new("ShaderNodeLightPath")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(lp.outputs["Is Camera Ray"], mix.inputs[0])
    nt.links.new(bg.outputs[0], mix.inputs[1])
    nt.links.new(cam_bg.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], nt.nodes["World Output"].inputs["Surface"])
    scn.world = world
    # sun
    sun = bpy.data.lights.new("Sun", 'SUN')
    sun.energy = 3.5
    sun_o = bpy.data.objects.new("Sun", sun)
    sun_o.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    link(sun_o)
    fill = bpy.data.lights.new("Fill", 'SUN')
    fill.energy = 0.8
    fill_o = bpy.data.objects.new("Fill", fill)
    fill_o.rotation_euler = (math.radians(70), 0, math.radians(150))
    link(fill_o)
    cam = bpy.data.cameras.new("Cam")
    cam.lens = lens
    cam.clip_end = 500
    if ortho:
        cam.type = 'ORTHO'
        cam.ortho_scale = ortho
    cam_o = bpy.data.objects.new("Cam", cam)
    link(cam_o)
    d = Vector(cam_dir).normalized()
    t = Vector(target)
    cam_o.location = t + d * dist
    cam_o.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    scn.camera = cam_o
    scn.render.filepath = path
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.render.render(write_still=True)
    # remove preview-only objects so they never get exported
    for o in (sun_o, fill_o, cam_o):
        bpy.data.objects.remove(o, do_unlink=True)
