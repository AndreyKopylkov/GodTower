"""Shared helpers: supersampled drawing, outlines, gradients, glossy shapes."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
from scipy import ndimage as ndi

NAVY = (24, 34, 78)


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(len(a)))


def lighten(c, t):
    return tuple(int(v + (255 - v) * t) for v in c[:3])


def darken(c, t):
    return tuple(int(v * (1 - t)) for v in c[:3])


def vgrad(w, h, top, bot, y0=0, y1=None):
    """RGBA vertical gradient image (w,h); gradient spans y0..y1 (px)."""
    y1 = h if y1 is None else y1
    t = np.clip((np.arange(h) - y0) / max(1, (y1 - y0)), 0, 1)[:, None, None]
    top = np.array(top[:3], float)[None, None, :]
    bot = np.array(bot[:3], float)[None, None, :]
    rgb = top + (bot - top) * t
    rgb = np.repeat(rgb, w, axis=1)
    a = np.full((h, w, 1), 255.0)
    return Image.fromarray(np.concatenate([rgb, a], 2).astype(np.uint8), "RGBA")


def dilate(mask, r):
    """Exact (round) dilation of an L mask by r pixels."""
    arr = np.array(mask) > 127
    d = ndi.distance_transform_edt(~arr) <= r
    return Image.fromarray((d * 255).astype(np.uint8), "L")


def erode(mask, r):
    arr = np.array(mask) > 127
    d = ndi.distance_transform_edt(arr) > r
    return Image.fromarray((d * 255).astype(np.uint8), "L")


def fill(mask, img_or_color):
    if isinstance(img_or_color, tuple):
        base = Image.new("RGBA", mask.size, tuple(img_or_color[:3]) + (255,))
    else:
        base = img_or_color.copy()
    base.putalpha(mask)
    return base


def scale_alpha(mask, a):
    return mask.point(lambda v: int(v * a))


def shadowed(layer_alpha, size, dx, dy, blur, alpha, color=NAVY):
    """Blurred drop shadow image for a given alpha mask."""
    m = ImageChops.offset(layer_alpha, int(dx), int(dy))
    m = m.filter(ImageFilter.GaussianBlur(blur))
    m = scale_alpha(m, alpha)
    return fill(m, color)


def over(base, top):
    return Image.alpha_composite(base, top)


class Glossy:
    """Glossy rounded UI shape rendered at ss x. Geometry given in final px."""

    def __init__(self, W, H, ss=4):
        self.W, self.H, self.ss = W, H, ss
        self.n = (W * ss, H * ss)

    def rrect(self, box, rad, inset=0):
        s = self.ss
        m = Image.new("L", self.n, 0)
        x0, y0, x1, y1 = [v * s for v in box]
        i = inset * s
        ImageDraw.Draw(m).rounded_rectangle([x0 + i, y0 + i, x1 - i, y1 - i], radius=max(1, rad * s - i), fill=255)
        return m

    def ellipse(self, box, inset=0):
        s = self.ss
        m = Image.new("L", self.n, 0)
        x0, y0, x1, y1 = [v * s for v in box]
        i = inset * s
        ImageDraw.Draw(m).ellipse([x0 + i, y0 + i, x1 - i, y1 - i], fill=255)
        return m

    def render(self, maskfn, top, bot, outline, ow, lip=0, lip_col=None, shadow=(0, 5, 4, 0.35), gloss=0.5, gloss_h=0.5, rim=0.35):
        """maskfn(inset)->L mask at supersampled size. Returns finished RGBA at final size."""
        s = self.ss
        W, H = self.n
        outer = maskfn(0)
        out = Image.new("RGBA", self.n, (0, 0, 0, 0))
        if shadow:
            dx, dy, bl, al = shadow
            out = over(out, shadowed(outer, self.n, dx * s, dy * s, bl * s, al))
        out = over(out, fill(outer, outline))
        body = maskfn(ow)
        bbox = body.getbbox()
        if lip:
            lip_col = lip_col or darken(bot, 0.3)
            out = over(out, fill(body, lip_col))
            face = ImageChops.multiply(body, ImageChops.offset(body, 0, -lip * s))
            # keep the original top edge
            face = ImageChops.lighter(face, ImageChops.multiply(body, ImageChops.offset(body, 0, 0)).point(lambda v: 0))
        else:
            face = body
        fb = face.getbbox()
        grad = vgrad(W, H, top, bot, fb[1], fb[3])
        out = over(out, fill(face, grad))
        # soft inner bottom shade
        sh = ImageChops.subtract(face, ImageChops.offset(erode(face, 6 * s), 0, -3 * s))
        sh = sh.filter(ImageFilter.GaussianBlur(3 * s))
        sh = ImageChops.multiply(sh, face)
        out = over(out, fill(scale_alpha(sh, 0.0), (0, 0, 0)))
        # rim light along top inside edge
        if rim:
            inner = ImageChops.offset(erode(face, 3 * s), 0, 0)
            ring = ImageChops.subtract(erode(face, 1 * s), erode(face, 4 * s))
            top_only = Image.new("L", self.n, 0)
            ImageDraw.Draw(top_only).rectangle([0, 0, W, fb[1] + (fb[3] - fb[1]) * 0.45], fill=255)
            ring = ImageChops.multiply(ring, top_only)
            out = over(out, fill(scale_alpha(ring, rim), (255, 255, 255)))
        if gloss:
            gi = ImageChops.multiply(erode(face, 5 * s), Image.new("L", self.n, 255))
            g = Image.new("L", self.n, 0)
            gb = erode(face, 5 * s).getbbox()
            if gb:
                gh = (gb[3] - gb[1]) * gloss_h
                ga = np.zeros((H, W), np.float32)
                ys = np.arange(H)
                ramp = np.clip(1 - (ys - gb[1]) / max(1, gh), 0, 1)
                ga[:] = ramp[:, None]
                gmask = Image.fromarray((ga * 255 * gloss).astype(np.uint8), "L")
                g = ImageChops.multiply(gmask, gi)
                # round off highlight bottom with a wide ellipse
                e = Image.new("L", self.n, 0)
                ImageDraw.Draw(e).rounded_rectangle([gb[0] + 4 * s, gb[1], gb[2] - 4 * s, gb[1] + gh], radius=gh * 0.5, fill=255)
                e = e.filter(ImageFilter.GaussianBlur(1.5 * s))
                g = ImageChops.multiply(g, e)
                out = over(out, fill(g, (255, 255, 255)))
        return out.resize((self.W, self.H), Image.LANCZOS)


def save(img, path):
    import os
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)
    return path
