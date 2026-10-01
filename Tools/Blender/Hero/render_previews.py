"""Render preview images of the hero (T-pose turnaround + frames of every clip) with Eevee.

Run from the project root:
    "D:/Games/Steam/steamapps/common/Blender/blender.exe" -b Art/Source/Character/hero.blend --python Tools/Blender/Hero/render_previews.py
Then build contact sheets:
    uv run --with pillow python Tools/Blender/Hero/make_sheets.py
"""
import math
import os

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Art", "Source", "Character", "Concept")
os.makedirs(os.path.join(OUT, "clips"), exist_ok=True)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.film_transparent = True
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'Standard'

world = bpy.data.worlds.new("PreviewWorld")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.82, 0.95, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.9

sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 3.0
sun.data.angle = math.radians(10)
sun.rotation_euler = (math.radians(50), 0, math.radians(30))
scene.collection.objects.link(sun)

target = bpy.data.objects.new("Target", None)
target.location = (0, 0, 0.92)
scene.collection.objects.link(target)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.25
scene.collection.objects.link(cam)
scene.camera = cam
con = cam.constraints.new('TRACK_TO')
con.target = target
con.track_axis = 'TRACK_NEGATIVE_Z'
con.up_axis = 'UP_Y'

rig = bpy.data.objects["HeroRig"]
ad = rig.animation_data
ad.use_nla = False
ad.action = None

VIEWS = {
    "front": (0, -1, 0.12),
    "back": (0, 1, 0.12),
    "left": (1, 0, 0.12),
    "right": (-1, 0, 0.12),
    "threequarter_front": (-0.7, -0.7, 0.2),
    "threequarter_back": (0.7, 0.7, 0.25),
}


def place(view):
    d = Vector(VIEWS[view]).normalized()
    cam.location = target.location + d * 8


def render(path):
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# T-pose turnaround
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
cam.data.ortho_scale = 2.3
target.location = (0, 0, 0.9)
for pb in rig.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)
scene.frame_set(0)
for v in VIEWS:
    place(v)
    render(os.path.join(OUT, f"hero_tpose_{v}.png"))

# Clip frames, seen from 3/4 back (the gameplay camera sees the hero from behind).
scene.render.resolution_x = 512
scene.render.resolution_y = 640
cam.data.ortho_scale = 2.5
target.location = (0, 0, 0.95)
CLIP_FRAMES = {
    "Hero_ClimbUp": [0, 6, 12, 18],
    "Hero_HangIdle": [0, 15, 30, 45],
    "Hero_ShiftLeft": [0, 3, 5, 8],
    "Hero_ShiftRight": [0, 3, 5, 8],
    "Hero_Hit": [0, 2, 6, 12],
    "Hero_Fall": [0, 5, 10, 15],
    "Hero_Carried": [0, 8, 15, 23],
    "Hero_Win": [8, 20, 34, 45],
    "Hero_Lose": [4, 10, 20, 36],
}
for clip, frames in CLIP_FRAMES.items():
    act = bpy.data.actions[clip]
    ad.action = act
    if act.slots:
        ad.action_slot = act.slots[0]
    for f in frames:
        scene.frame_set(f)
        place("threequarter_back")
        render(os.path.join(OUT, "clips", f"{clip}_f{f:02d}.png"))
print("RENDER_OK")
