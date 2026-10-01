"""Procedural cartoon icons. Drawing coordinates are in a 256-unit space."""
import math
from PIL import Image, ImageDraw, ImageChops
from common import *


class Canvas:
    def __init__(self, out=256, ss=4):
        self.out, self.ss = out, ss
        self.n = out * ss
        self.k = self.n / 256.0
        self.img = Image.new("RGBA", (self.n, self.n), (0, 0, 0, 0))

    def _mask(self, fn, rot=None):
        m = Image.new("L", (self.n, self.n), 0)
        fn(ImageDraw.Draw(m), self.k)
        if rot:
            a, cx, cy = rot
            m = m.rotate(a, resample=Image.BICUBIC, center=(cx * self.k, cy * self.k))
        return m

    def shape(self, fn, col, edge=True, ew=2.4, grad=0.28, alpha=1.0, rot=None, edge_col=None):
        m = self._mask(fn, rot)
        bb = m.getbbox()
        if not bb:
            return
        inner = erode(m, ew * self.k) if edge else m
        g = vgrad(self.n, self.n, lighten(col, grad), darken(col, grad * 0.35), bb[1], bb[3])
        layer = Image.new("RGBA", (self.n, self.n), (0, 0, 0, 0))
        if edge:
            layer = over(layer, fill(m, edge_col or darken(col, 0.45)))
        layer = over(layer, fill(inner, g))
        if alpha < 1:
            layer.putalpha(scale_alpha(layer.getchannel("A"), alpha))
        self.img = over(self.img, layer)

    def poly(self, pts, col, **kw):
        self.shape(lambda d, k: d.polygon([(x * k, y * k) for x, y in pts], fill=255), col, **kw)

    def ell(self, box, col, **kw):
        self.shape(lambda d, k: d.ellipse([v * k for v in box], fill=255), col, **kw)

    def rr(self, box, r, col, **kw):
        self.shape(lambda d, k: d.rounded_rectangle([v * k for v in box], radius=r * k, fill=255), col, **kw)

    def line(self, pts, w, col, **kw):
        def f(d, k):
            d.line([(x * k, y * k) for x, y in pts], fill=255, width=int(w * k), joint="curve")
            for x, y in (pts[0], pts[-1]):
                d.ellipse([(x - w / 2) * k, (y - w / 2) * k, (x + w / 2) * k, (y + w / 2) * k], fill=255)
        self.shape(f, col, **kw)

    def arc(self, box, a0, a1, w, col, **kw):
        def f(d, k):
            d.arc([v * k for v in box], a0, a1, fill=255, width=int(w * k))
            # round caps
            cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
            r = (box[2] - box[0]) / 2 - w / 2
            for a in (a0, a1):
                x, y = cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))
                d.ellipse([(x - w / 2) * k, (y - w / 2) * k, (x + w / 2) * k, (y + w / 2) * k], fill=255)
        self.shape(f, col, **kw)

    def shine(self, box, alpha=0.6):
        self.ell(box, (255, 255, 255), edge=False, grad=0, alpha=alpha)

    def finish(self, ow=7, shadow=True):
        s = self.ss
        f = self.out / 256
        alpha = self.img.getchannel("A")
        ol = dilate(alpha, ow * s * f)
        out = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        if shadow:
            out = over(out, shadowed(ol, self.img.size, 0, int(6 * s * f), 4 * s * f, 0.35))
        out = over(out, fill(ol, NAVY))
        out = over(out, self.img)
        return out.resize((self.out, self.out), Image.LANCZOS)


def group(c, drawfn, angle=0, scale=1.0, dx=0, dy=0):
    g = Canvas(c.out, c.ss)
    drawfn(g)
    im = g.img
    n = c.n
    if scale != 1.0:
        sz = int(n * scale)
        im = im.resize((sz, sz), Image.LANCZOS)
        canvas = Image.new("RGBA", (n, n), (0, 0, 0, 0))
        canvas.paste(im, ((n - sz) // 2, (n - sz) // 2), im)
        im = canvas
    if angle:
        im = im.rotate(angle, resample=Image.BICUBIC)
    if dx or dy:
        im = ImageChops.offset(im, int(dx * c.k), int(dy * c.k))
    c.img = over(c.img, im)


WHITE = (250, 250, 255)
RED = (240, 60, 60)
BLUE = (60, 150, 245)
YEL = (255, 205, 40)
ORG = (255, 140, 30)
GREY = (170, 180, 200)
BROWN = (150, 90, 50)
STEEL = (200, 210, 228)


def flame(c, cx, ytop, ybot, w):
    c.poly([(cx - w, ytop), (cx, ybot), (cx + w, ytop)], ORG)
    c.poly([(cx - w * .55, ytop), (cx, ybot - (ybot - ytop) * .25), (cx + w * .55, ytop)], YEL, edge=False)


def missile(c):
    def d(g):
        flame(g, 128, 178, 244, 22)
        g.poly([(98, 150), (60, 206), (60, 168), (86, 120)], RED)
        g.poly([(158, 150), (196, 206), (196, 168), (170, 120)], RED)
        g.rr((96, 62, 160, 186), 24, WHITE)
        g.poly([(96, 84), (128, 4), (160, 84)], RED)
        g.ell((108, 104, 148, 144), BLUE, ew=3.5)
        g.shine((114, 108, 130, 122), 0.8)
        g.rr((102, 160, 154, 176), 6, GREY, edge=False)
    group(c, d, angle=-40, scale=0.88)


def truck(c):
    c.rr((20, 70, 150, 170), 12, (255, 235, 200))
    c.rr((30, 82, 140, 96), 5, (255, 190, 60), edge=False)
    c.poly([(150, 94), (196, 94), (230, 130), (230, 172), (150, 172)], RED)
    c.poly([(164, 106), (192, 106), (212, 130), (164, 130)], (170, 225, 255), ew=2.2)
    c.shine((168, 108, 184, 118), 0.7)
    c.rr((16, 160, 236, 184), 8, (70, 78, 105))
    for x in (62, 190):
        c.ell((x - 28, 148, x + 28, 204), (50, 56, 78), ew=3)
        c.ell((x - 12, 164, x + 12, 188), (190, 200, 215), ew=2.5)


def axes(c):
    def axe(g, sign):
        g.rr((118, 58, 138, 232), 8, BROWN)
        def blade(d, k):
            pts = [(138, 44), (200, 22), (214, 70), (200, 120), (138, 100)]
            if sign < 0:
                pts = [(256 - x, y) for x, y in pts]
            d.polygon([(x * k, y * k) for x, y in pts], fill=255)
        g.shape(blade, STEEL)
        hl = [(142, 52), (196, 34), (204, 70), (198, 104), (142, 92)]
        if sign < 0:
            hl = [(256 - x, y) for x, y in hl]
        g.poly(hl, (232, 238, 250), edge=False, grad=0.1)
    group(c, lambda g: axe(g, 1), angle=-38, scale=0.86, dx=4)
    group(c, lambda g: axe(g, -1), angle=38, scale=0.86, dx=-4)


def jetpack(c):
    for x in (40, 136):
        flame(c, x + 40, 196, 238, 20)
    c.rr((88, 100, 168, 140), 6, (90, 98, 125))
    for x in (40, 136):
        c.rr((x, 40, x + 80, 190), 34, STEEL)
        c.rr((x + 4, 78, x + 76, 112), 4, RED, edge=False)
        c.rr((x + 10, 186, x + 70, 204), 8, (70, 78, 105))
        c.shine((x + 12, 50, x + 28, 100), 0.7)
    c.rr((30, 128, 226, 148), 8, BROWN)


def phoenix(c):
    group(c, phoenix_draw, scale=0.88)


def phoenix_draw(c):
    c.poly([(128, 120), (22, 40), (30, 96), (14, 120), (40, 150), (30, 176), (96, 170), (128, 160)], RED)
    c.poly([(128, 120), (234, 40), (226, 96), (242, 120), (216, 150), (226, 176), (160, 170), (128, 160)], RED)
    c.poly([(116, 120), (40, 62), (46, 100), (30, 124), (58, 140), (90, 142)], ORG, edge=False)
    c.poly([(140, 120), (216, 62), (210, 100), (226, 124), (198, 140), (166, 142)], ORG, edge=False)
    c.poly([(128, 170), (92, 244), (128, 224), (164, 244)], ORG)
    c.poly([(128, 170), (110, 232), (128, 216), (146, 232)], YEL, edge=False)
    c.ell((96, 90, 160, 200), (255, 120, 40))
    c.ell((108, 130, 148, 192), YEL, edge=False)
    c.poly([(116, 56), (104, 18), (124, 40), (128, 8), (134, 40), (152, 18), (140, 56)], YEL)
    c.ell((100, 40, 156, 100), (255, 140, 40))
    c.poly([(116, 84), (140, 84), (128, 108)], YEL, ew=2.2)
    c.ell((108, 62, 124, 80), WHITE, ew=2)
    c.ell((132, 62, 148, 80), WHITE, ew=2)
    c.ell((113, 68, 121, 78), NAVY, edge=False, grad=0)
    c.ell((135, 68, 143, 78), NAVY, edge=False, grad=0)


def glove(c):
    def d(g):
        g.rr((92, 168, 170, 236), 12, WHITE)
        g.line([(100, 190), (162, 190)], 4, (205, 212, 230), edge=False, grad=0)
        g.rr((72, 24, 188, 188), 52, RED)
        g.ell((54, 84, 126, 150), (255, 90, 80))
        g.line([(100, 120), (96, 104)], 3, (150, 20, 30), edge=False, grad=0)
        g.shine((88, 40, 134, 66), 0.55)
        g.line([(112, 150), (150, 150)], 3, (150, 20, 30), edge=False, grad=0)
    group(c, d, angle=-22, scale=0.92)


def lock(c):
    c.line([(82, 100), (82, 70)], 28, (190, 198, 218))
    c.line([(174, 100), (174, 70)], 28, (190, 198, 218))
    c.arc((82, 34, 174, 126), 180, 360, 28, (190, 198, 218))
    c.rr((44, 98, 212, 228), 28, YEL)
    c.rr((54, 108, 202, 130), 10, (255, 232, 130), edge=False, grad=0.1)
    c.ell((108, 140, 148, 180), (70, 50, 40), ew=2)
    c.poly([(121, 168), (135, 168), (140, 206), (116, 206)], (70, 50, 40), edge=False, grad=0)
    c.shine((60, 140, 76, 200), 0.4)


def star(c):
    def sp(ro, ri, cy):
        return [(128 + (ro if i % 2 == 0 else ri) * math.cos(-math.pi / 2 + i * math.pi / 5),
                 cy + (ro if i % 2 == 0 else ri) * math.sin(-math.pi / 2 + i * math.pi / 5)) for i in range(10)]
    c.poly(sp(106, 52, 134), YEL, ew=3.5)
    c.poly(sp(70, 34, 136), (255, 233, 120), edge=False, grad=0.2)
    c.shine((106, 58, 134, 84), 0.7)


def pause(c):
    c.rr((58, 40, 106, 216), 16, WHITE)
    c.rr((150, 40, 198, 216), 16, WHITE)


def play(c):
    c.poly([(72, 36), (210, 128), (72, 220)], WHITE, grad=0.1, ew=3)


def home(c):
    c.rr((58, 112, 198, 218), 12, (255, 240, 210))
    c.poly([(26, 124), (128, 28), (230, 124)], RED, ew=3)
    c.rr((178, 52, 202, 100), 4, (190, 70, 60))
    c.rr((104, 150, 152, 218), 14, BROWN)
    c.ell((136, 182, 144, 190), YEL, edge=False, grad=0)
    c.rr((70, 128, 96, 154), 4, (170, 225, 255), ew=2)
    c.rr((160, 128, 186, 154), 4, (170, 225, 255), ew=2)


def retry(c):
    c.arc((40, 48, 212, 220), 60, 340, 34, WHITE, ew=3)
    c.poly([(152, 106), (230, 106), (191, 156)], WHITE, grad=0.1, ew=3)


def nxt(c):
    c.poly([(40, 40), (170, 128), (40, 216)], WHITE, grad=0.1, ew=3)
    c.rr((170, 40, 222, 216), 14, WHITE)


def trophy(c):
    c.arc((20, 50, 100, 150), 90, 270, 18, YEL, ew=3)
    c.arc((156, 50, 236, 150), 270, 450, 18, YEL, ew=3)
    c.rr((106, 140, 150, 196), 6, (230, 160, 20))
    c.rr((72, 196, 184, 232), 10, BROWN)
    c.rr((88, 202, 168, 216), 4, YEL, edge=False, grad=0.1)

    def bowl(d, k):
        d.polygon([(x * k, y * k) for x, y in [(60, 24), (196, 24), (190, 100), (160, 148), (96, 148), (66, 100)]], fill=255)
        d.ellipse([60 * k, 8 * k, 196 * k, 40 * k], fill=255)
    c.shape(bowl, YEL, ew=3)
    c.ell((74, 12, 182, 36), (255, 235, 140), ew=2, edge=False)
    c.shine((78, 52, 96, 110), 0.55)
    c.poly([(128, 56), (136, 80), (160, 80), (141, 94), (148, 118), (128, 104), (108, 118), (115, 94), (96, 80), (120, 80)],
           (255, 245, 190), ew=2)


ICONS = dict(missile=missile, truck=truck, axes=axes, jetpack=jetpack, phoenix=phoenix, boxing_glove=glove, lock=lock,
             star=star, pause=pause, play=play, home=home, retry=retry, next=nxt, trophy=trophy)


def render_icon(name, out=256, ss=4):
    c = Canvas(out, ss)
    ICONS[name](c)
    return c.finish()
