"""B-09 Phoenix: low-poly stylised fire bird with a 1 s wing-flap loop (30 frames @ 30 fps).
Pivot = body centre; flies along Unity +Z, up +Y, wings span ±X. Rigid skinning on a small Generic rig:
  Root > Body, Tail, Wing1.L > Wing2.L, Wing1.R > Wing2.R.  Action/clip: "Phoenix_Flap" (loop).
Fire VFX is added in Unity.
Run: blender.exe -b --factory-startup --python Tools/Blender/Props/build_phoenix.py
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import propslib as pl  # noqa: E402
import gtlib as gt  # noqa: E402

PERM_YZ = Matrix(((0, 0, 1, 0), (1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 0, 1)))  # local x->Y, y->Z, z->X
SHOULDER_X, ELBOW_X, TIP_X, WING_Z = 0.18, 0.85, 1.75, 0.10
FPS, FRAMES = 30, 30


def group(obj, name):
    vg = obj.vertex_groups.new(name=name)
    vg.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    return obj


def fire_by_dist(origin, d0, d1):
    o = Vector(origin)

    def fn(c, n):
        d = (Vector((c.x, c.y, 0)) - Vector((o.x, o.y, 0))).length
        if d < d0:
            return "fire_red"
        if d < d1:
            return "fire_orange"
        return "fire_yellow"
    return fn


def wing_parts(side):
    """Return [(obj, bone)] for one wing; side = +1 (right, +X) or -1 (left)."""
    sfx = "R" if side > 0 else "L"
    mir = Matrix.Scale(side, 4, (1, 0, 0))
    parts = []

    def make(name, pts, th, z, color, bone):
        o = pl.extruded_poly(f"{name}.{sfx}", pts, th, matrix=mir @ Matrix.Translation((0, 0, WING_Z + z)), color=color)
        if side < 0:
            o.data.flip_normals()
        parts.append((group(o, bone), bone))
        return o
    # inner wing (red) with scalloped trailing edge
    make("inner", [(SHOULDER_X - 0.05, -0.26), (ELBOW_X + 0.04, -0.32), (ELBOW_X + 0.04, 0.30), (0.74, 0.47), (0.62, 0.33),
                   (0.5, 0.5), (0.38, 0.34), (0.26, 0.46), (SHOULDER_X - 0.05, 0.28)], 0.07, 0.0, "fire_red", f"Wing1.{sfx}")
    # inner covert (yellow) along the leading edge
    make("covI", [(SHOULDER_X, -0.29), (ELBOW_X + 0.02, -0.34), (ELBOW_X + 0.02, -0.02), (0.6, 0.08), (0.4, -0.02),
                  (SHOULDER_X, 0.06)], 0.05, 0.035, "fire_yellow", f"Wing1.{sfx}")
    # outer wing: flame-like primary feathers (orange)
    make("outer", [(ELBOW_X - 0.04, -0.32), (1.35, -0.28), (TIP_X, -0.12), (1.86, 0.02), (1.52, 0.12), (1.72, 0.40),
                   (1.36, 0.30), (1.42, 0.66), (1.14, 0.40), (1.12, 0.70), (0.96, 0.38), (ELBOW_X - 0.04, 0.32)],
         0.06, 0.0, "fire_orange", f"Wing2.{sfx}")
    make("covO", [(ELBOW_X - 0.02, -0.34), (1.35, -0.30), (1.68, -0.15), (1.4, -0.02), (1.1, 0.04), (ELBOW_X - 0.02, 0.0)],
         0.045, 0.03, "fire_yellow", f"Wing2.{sfx}")
    return parts


def build_mesh():
    P = []
    rotX = Matrix.Rotation(math.radians(90), 4, 'X')    # local Z -> -Y
    body = pl.superellipsoid("body", (0.26, 0.27, 0.56), nu=14, nv=10, e1=0.95, e2=1.0,
                             matrix=Matrix.Translation((0, 0.02, 0.0)) @ rotX, color="fire_orange",
                             deform=lambda v: Vector((v.x, v.y, v.z + 0.10 * max(0.0, -v.y - 0.2))))
    pl.paint_by(body, lambda c, n: "fire_yellow" if n.z < -0.35 else None)
    P.append(group(body, "Body"))
    head = pl.superellipsoid("head", (0.17, 0.2, 0.17), nu=12, nv=8, color="fire_red",
                             matrix=Matrix.Translation((0, -0.62, 0.30)) @ rotX)
    P.append(group(head, "Body"))
    beak = pl.cylinder("beak", 0.075, 0.22, sides=8, r2=0.0, color="gold",
                       loc=(0, -0.86, 0.27), rot=(math.radians(90), 0, 0))
    beak.data.transform(Matrix.Translation((0, 0, 0)))
    P.append(group(beak, "Body"))
    for sx in (-1, 1):
        eye = pl.superellipsoid(f"eye{sx}", (0.045, 0.045, 0.045), nu=8, nv=5, color="black",
                                matrix=Matrix.Translation((sx * 0.12, -0.74, 0.35)))
        P.append(group(eye, "Body"))
        glint = pl.superellipsoid(f"glint{sx}", (0.016, 0.016, 0.016), nu=6, nv=4, color="white",
                                  matrix=Matrix.Translation((sx * 0.145, -0.765, 0.375)))
        P.append(group(glint, "Body"))
    # crest: three flame plumes sweeping back from the head
    for k, (dy, h, col) in enumerate(((0.0, 0.42, "fire_yellow"), (0.12, 0.34, "fire_orange"), (0.22, 0.26, "fire_yellow"))):
        pts = [(-0.06, 0.0), (0.08, 0.0), (0.22 + 0.1 * k, h * 0.7), (0.30 + 0.1 * k, h), (0.12 + 0.08 * k, h * 0.55)]
        o = pl.extruded_poly(f"crest{k}", pts, 0.05, matrix=Matrix.Translation((0, -0.66 + dy, 0.40 - 0.05 * k)) @ PERM_YZ,
                             color=col)
        P.append(group(o, "Body"))
    # tail: fanned flame ribbons (red -> orange -> yellow)
    for i in range(-2, 3):
        a = math.radians(i * 15)
        L = 1.45 if i == 0 else 1.2 - 0.08 * abs(i)
        base = Vector((i * 0.04, 0.45, 0.0))
        pts = []
        for s in range(6):
            t = s / 5
            pts.append(base + Vector((math.sin(a) * L * t, math.cos(a) * L * t, -0.18 * t * t + 0.22 * max(0, t - 0.7) ** 2)))
        rib = pl.tube(f"tail{i}", pts, 1.0, sides=6, color="fire_red", up=(0, 0, 1), scale_xy=(1.0, 0.22),
                      radius_fn=lambda t: 0.07 + 0.11 * math.sin(math.pi * min(t, 0.92) * 1.0) * (1 - 0.6 * t) if t < 1 else 0.01)
        pl.paint_by(rib, fire_by_dist(base, 0.35, 0.8))
        P.append(group(rib, "Tail"))
    for side in (1, -1):
        for o, _b in wing_parts(side):
            P.append(o)
    return pl.finalize(P, "Phoenix", smooth_angle=50)


def build_rig():
    arm = bpy.data.armatures.new("PhoenixRig")
    ao = bpy.data.objects.new("PhoenixRig", arm)
    gt.link(ao)
    gt.select_only([ao])
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.edit_bones

    def bone(name, head, tail, parent=None):
        b = eb.new(name)
        b.head, b.tail = Vector(head), Vector(tail)
        b.roll = 0.0
        if parent:
            b.parent = eb[parent]
        return b
    bone("Root", (0, 0, -0.3), (0, 0, 0.0))
    bone("Body", (0, 0, 0), (0, -0.5, 0), "Root")
    bone("Tail", (0, 0.45, 0), (0, 1.2, 0), "Root")
    for side, sfx in ((1, "R"), (-1, "L")):
        bone(f"Wing1.{sfx}", (side * SHOULDER_X, 0, WING_Z), (side * ELBOW_X, 0, WING_Z), "Root")
        b2 = bone(f"Wing2.{sfx}", (side * ELBOW_X, 0, WING_Z), (side * TIP_X, 0, WING_Z), f"Wing1.{sfx}")
        b2.use_connect = True
    bpy.ops.object.mode_set(mode='OBJECT')
    return ao


def animate(ao):
    scn = bpy.context.scene
    scn.render.fps = FPS
    scn.frame_start, scn.frame_end = 0, FRAMES
    ao.animation_data_create()
    act = bpy.data.actions.new("Phoenix_Flap")
    ao.animation_data.action = act
    pb = ao.pose.bones
    for b in pb:
        b.rotation_mode = 'XYZ'
    # (frame, upper deg, lower deg, root z, tail deg)
    keys = [(0, 38, 12, -0.06, -6), (8, 2, -4, 0.0, 0), (15, -42, -22, 0.09, 7), (22, -6, 18, 0.03, 2), (30, 38, 12, -0.06, -6)]
    for f, up, lo, rz, tl in keys:
        for sfx in ("L", "R"):
            pb[f"Wing1.{sfx}"].rotation_euler = (0, 0, 0)
            # bone local X = world -Y (right) / +Y (left): the same sign flaps both wings symmetrically
            pb[f"Wing1.{sfx}"].rotation_euler.x = math.radians(up)
            pb[f"Wing2.{sfx}"].rotation_euler = (math.radians(lo), 0, 0)
            pb[f"Wing1.{sfx}"].keyframe_insert("rotation_euler", frame=f)
            pb[f"Wing2.{sfx}"].keyframe_insert("rotation_euler", frame=f)
        pb["Root"].location = (0, rz, 0)       # Root bone points +Z: local Y = world Z
        pb["Root"].keyframe_insert("location", frame=f)
        pb["Tail"].rotation_euler = (math.radians(tl), 0, 0)
        pb["Tail"].keyframe_insert("rotation_euler", frame=f)
    # smooth, cyclic
    for fc in _fcurves(act, ao):
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
    return act


def _fcurves(act, ao):
    try:
        return list(act.fcurves)
    except AttributeError:   # layered actions (Blender 4.4+)
        out = []
        for layer in act.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    out.extend(cb.fcurves)
        return out


def main():
    pl.start()
    mesh = build_mesh()
    ao = build_rig()
    mesh.parent = ao
    mod = mesh.modifiers.new("Armature", 'ARMATURE')
    mod.object = ao
    animate(ao)
    print("TRIS Phoenix:", gt.tri_count(mesh), "dims", tuple(round(x, 3) for x in mesh.dimensions))
    pl.export([ao, mesh], "Prop_Phoenix", anim=True)
    scn = bpy.context.scene
    for f, nm in ((0, "prop_phoenix_up"), (15, "prop_phoenix_down")):
        scn.frame_set(f)
        pl.preview(nm, target=(0, 0.2, 0.1), dist=6.5, cam_dir=(0.35, -1.0, 0.55))


if __name__ == "__main__":
    main()
