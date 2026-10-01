from PIL import Image
import sys, glob, os

def sheet(paths, out, cell=256, cols=5, bg=(120, 190, 240)):
    ims = [Image.open(p).convert("RGBA") for p in paths]
    rows = (len(ims) + cols - 1) // cols
    sh = Image.new("RGBA", (cols * cell, rows * cell), bg + (255,))
    for i, im in enumerate(ims):
        im.thumbnail((cell - 8, cell - 8))
        sh.alpha_composite(im, ((i % cols) * cell + (cell - im.width) // 2, (i // cols) * cell + (cell - im.height) // 2))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sh.convert("RGB").save(out)

if __name__ == "__main__":
    sheet(sorted(glob.glob(sys.argv[1])), sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 256, int(sys.argv[4]) if len(sys.argv) > 4 else 5)
