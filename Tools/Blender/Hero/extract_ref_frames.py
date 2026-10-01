"""Extract reference frames from Reference/ref.mp4 into Art/Source/Character/RefFrames (contact sheets).
Run: uv run --with imageio-ffmpeg --with pillow python Tools/Blender/Hero/extract_ref_frames.py
"""
import subprocess, os, imageio_ffmpeg
from PIL import Image
ff = imageio_ffmpeg.get_ffmpeg_exe()
out = "Art/Source/Character/RefFrames"
times = [1,2,3,4,5,6,7,8,10,10.5,11,14,18,22,55,68,68.5,69,73,75,77,82,86,86.5,91,92,95,96,97,98,99,100]
for t in times:
    p = f"{out}/f_{t:06.1f}.png"
    subprocess.run([ff,"-y","-loglevel","error","-ss",str(t),"-i","Reference/ref.mp4","-frames:v","1",p],check=True)
# contact sheets of 8 per sheet, half size
files = [f"{out}/f_{t:06.1f}.png" for t in times]
for s in range(0,len(files),8):
    ims=[Image.open(f) for f in files[s:s+8]]
    w,h=ims[0].size; w//=2; h//=2
    sheet=Image.new("RGB",(w*4,h*2))
    for i,im in enumerate(ims):
        sheet.paste(im.resize((w,h)),((i%4)*w,(i//4)*h))
    sheet.save(f"{out}/sheet_{s//8}.png")
print("ok")
