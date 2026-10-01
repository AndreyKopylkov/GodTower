import imageio_ffmpeg, subprocess, sys
ff = imageio_ffmpeg.get_ffmpeg_exe()
for t in (5,30,75,98):
    subprocess.run([ff,"-y","-ss",str(t),"-i","Reference/ref.mp4","-frames:v","1","-vf","scale=720:-1",f"Art/Source/UI/RefFrames/f{t}.png"],capture_output=True)
