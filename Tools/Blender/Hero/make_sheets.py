"""Compose preview renders into contact sheets (turnaround, clip frames, small-size back read test).

    uv run --with pillow python Tools/Blender/Hero/make_sheets.py
"""
import os

from PIL import Image, ImageDraw

OUT = "Art/Source/Character/Concept"
SKY = (64, 140, 230, 255)

# Turnaround
views = ["front", "threequarter_front", "left", "back", "threequarter_back", "right"]
ims = [Image.open(f"{OUT}/hero_tpose_{v}.png").resize((512, 512)) for v in views]
sheet = Image.new("RGBA", (512 * 3, 512 * 2), (235, 238, 245, 255))
for i, im in enumerate(ims):
    sheet.alpha_composite(im, ((i % 3) * 512, (i // 3) * 512))
sheet.save(f"{OUT}/hero_turnaround_sheet.png")

# Clip frames: one row per clip
clips = sorted({f.rsplit("_f", 1)[0] for f in os.listdir(f"{OUT}/clips")})
cw, ch = 256, 320
sheet = Image.new("RGBA", (cw * 4 + 170, ch * len(clips)), (235, 238, 245, 255))
d = ImageDraw.Draw(sheet)
for r, clip in enumerate(clips):
    frames = sorted(f for f in os.listdir(f"{OUT}/clips") if f.startswith(clip + "_f"))
    d.text((6, r * ch + ch // 2), clip, fill=(0, 0, 0, 255))
    for c, f in enumerate(frames[:4]):
        im = Image.open(f"{OUT}/clips/{f}").resize((cw, ch))
        sheet.alpha_composite(im, (170 + c * cw, r * ch))
sheet.save(f"{OUT}/hero_clips_sheet.png")

# Small-size readability: hero at ~15% of a 1920 px tall screen (~290 px), and at half that.
back = Image.open(f"{OUT}/hero_tpose_threequarter_back.png")
climb = Image.open(f"{OUT}/clips/Hero_ClimbUp_f00.png")
test = Image.new("RGBA", (900, 420), SKY)
for i, (im, h) in enumerate([(back, 290), (climb, 290), (back, 145), (climb, 145)]):
    w = int(im.width * h / im.height)
    test.alpha_composite(im.resize((w, h), Image.LANCZOS), (20 + i * 220, 400 - h))
test.save(f"{OUT}/hero_small_read_test.png")
print("sheets ok")
