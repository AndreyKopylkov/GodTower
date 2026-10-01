"""Tiny numpy/scipy synthesis toolkit used by generate_all.py.

All signals are float64 numpy arrays at SR (44.1 kHz), mono unless stated otherwise.
Everything is deterministic: noise functions take an explicit seed.
"""
import wave

import numpy as np
from scipy import signal

SR = 44100


def n_samples(dur):
    return int(round(dur * SR))


def time(dur):
    return np.arange(n_samples(dur)) / SR


def db(x):
    return 10 ** (x / 20.0)


# ---------------------------------------------------------------- oscillators
def osc(kind, freq, dur, pw=0.5, oversample=4):
    """Oscillator. freq may be a scalar or per-sample array (pitch sweeps).
    Non-sine shapes are generated oversampled and decimated to limit aliasing."""
    n = n_samples(dur)
    f = np.broadcast_to(np.asarray(freq, dtype=np.float64), (n,))
    if kind == "sine":
        return np.sin(np.cumsum(2 * np.pi * f / SR))
    fo = np.repeat(f, oversample)
    ph = np.cumsum(fo / (SR * oversample)) % 1.0
    if kind == "saw":
        y = 2 * ph - 1
    elif kind == "square":
        y = np.where(ph < pw, 1.0, -1.0)
    elif kind == "tri":
        y = 4 * np.abs(ph - 0.5) - 1
    else:
        raise ValueError(kind)
    return signal.resample_poly(y, 1, oversample)[:n]


def noise(dur, seed=0, color="white"):
    rng = np.random.default_rng(seed)
    x = rng.standard_normal(n_samples(dur))
    if color == "pink":
        x = signal.lfilter([0.049922035, -0.095993537, 0.050612699, -0.004408786],
                           [1, -2.494956002, 2.017265875, -0.522189400], x)
    elif color == "brown":
        x = np.cumsum(x)
        x -= np.linspace(x[0], x[-1], len(x))
    return x / (np.max(np.abs(x)) + 1e-9)


# ------------------------------------------------------------------ envelopes
def adsr(dur, a=0.005, d=0.05, s=0.7, r=0.05, curve=2.0):
    """Gate length dur (seconds) then release r. Total length dur + r."""
    n = n_samples(dur)
    nr = n_samples(r)
    na = max(1, min(n_samples(a), n))
    nd = max(1, min(n_samples(d), max(n - na, 1)))
    env = np.empty(n + nr)
    env[:na] = np.linspace(0, 1, na)
    env[na:na + nd] = np.linspace(1, s, nd)
    env[na + nd:n] = s
    last = env[n - 1] if n > 0 else s
    if nr:
        env[n:] = last * (1 - np.linspace(0, 1, nr)) ** curve
    return env


def expdecay(dur, tau):
    """Exponential decay with a 1 ms attack so it never clicks on."""
    e = np.exp(-time(dur) / tau)
    na = n_samples(0.001)
    e[:na] *= np.linspace(0, 1, na)
    return e


def fade(x, fin=0.002, fout=0.01):
    x = x.copy()
    a, b = min(n_samples(fin), len(x)), min(n_samples(fout), len(x))
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def sweep(f0, f1, dur):
    """Exponential frequency sweep array."""
    t = np.linspace(0, 1, n_samples(dur))
    return f0 * (f1 / f0) ** t


def bell(dur, power=2.0):
    """Smooth 0-1-0 amplitude shape (whooshes, flybys)."""
    return np.sin(np.linspace(0, np.pi, n_samples(dur))) ** power


# -------------------------------------------------------------------- filters
def _sos(kind, cutoff, order=2):
    return signal.butter(order, cutoff, btype=kind, fs=SR, output="sos")


def lowpass(x, cutoff, order=2):
    return signal.sosfilt(_sos("low", min(cutoff, SR * 0.45), order), x)


def highpass(x, cutoff, order=2):
    return signal.sosfilt(_sos("high", cutoff, order), x)


def bandpass(x, lo, hi, order=2):
    return signal.sosfilt(_sos("band", [lo, min(hi, SR * 0.45)], order), x)


def svf(x, cutoff, q=1.0, mode="band"):
    """Time-varying state-variable filter (TPT). cutoff scalar or per-sample array."""
    n = len(x)
    fc = np.broadcast_to(np.asarray(cutoff, dtype=np.float64), (n,))
    g = np.tan(np.pi * np.clip(fc, 20, SR * 0.45) / SR)
    k = 1.0 / q
    a1 = 1.0 / (1.0 + g * (g + k))
    a2 = g * a1
    a3 = g * a2
    out = np.empty(n)
    ic1 = ic2 = 0.0
    xl, a1l, a2l, a3l = x.tolist(), a1.tolist(), a2.tolist(), a3.tolist()
    for i in range(n):
        v3 = xl[i] - ic2
        v1 = a1l[i] * ic1 + a2l[i] * v3
        v2 = ic2 + a2l[i] * ic1 + a3l[i] * v3
        ic1 = 2 * v1 - ic1
        ic2 = 2 * v2 - ic2
        if mode == "band":
            out[i] = v1
        elif mode == "low":
            out[i] = v2
        else:
            out[i] = xl[i] - k * v1 - v2
    return out


# -------------------------------------------------------------------- effects
def distort(x, drive=3.0):
    return np.tanh(x * drive) / np.tanh(drive)


def bitcrush(x, bits=6, hold=4):
    y = np.repeat(x[::hold], hold)[:len(x)]
    q = 2 ** bits
    return np.round(y * q) / q


def vibrato(x, rate, depth_ms=0.5):
    n = len(x)
    t = np.arange(n) / SR
    idx = np.arange(n) + depth_ms * 1e-3 * SR * np.sin(2 * np.pi * rate * t)
    return np.interp(idx, np.arange(n), x)


def reverb(x, decay=0.8, wet=0.25, seed=1, predelay=0.012, damp=4500):
    """Convolution reverb with a synthetic decaying-noise IR. Extends the signal by ~decay seconds.
    Accepts mono (n,) or stereo (n, 2) arrays."""
    rng = np.random.default_rng(seed)
    n = n_samples(decay)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-6.9 * t / decay)
    ir = lowpass(ir, damp)
    na = n_samples(0.002)
    ir[:na] *= np.linspace(0, 1, na)
    ir = np.concatenate([np.zeros(n_samples(predelay)), ir])
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9

    def one(sig, impulse):
        w = signal.fftconvolve(sig, impulse)
        d = np.concatenate([sig, np.zeros(len(w) - len(sig))])
        return d * (1 - wet * 0.5) + w * wet * 1.5

    if x.ndim == 1:
        return one(x, ir)
    return np.stack([one(x[:, c], ir if c == 0 else np.roll(ir, 37)) for c in range(x.shape[1])], axis=1)


def echo(x, delay, feedback=0.35, taps=4, damp=3500):
    y = np.concatenate([x, np.zeros(n_samples(delay) * taps)])
    cur = x
    for i in range(1, taps + 1):
        cur = lowpass(cur, damp, 1) * feedback
        off = n_samples(delay) * i
        y[off:off + len(cur)] += cur
    return y


# -------------------------------------------------------------------- mixing
def mix(*parts, length=None):
    """Sum signals of different lengths. Each part is an array or (array, offset_seconds, gain)."""
    items = []
    for p in parts:
        if isinstance(p, tuple):
            sig = p[0]
            off = p[1] if len(p) > 1 else 0.0
            gain = p[2] if len(p) > 2 else 1.0
            items.append((sig, n_samples(off), gain))
        else:
            items.append((p, 0, 1.0))
    total = length or max(o + len(s) for s, o, _ in items)
    out = np.zeros(total)
    for s, o, g in items:
        e = min(total, o + len(s))
        if e > o:
            out[o:e] += s[:e - o] * g
    return out


def pad_to(x, dur):
    n = n_samples(dur)
    return x[:n] if len(x) >= n else np.concatenate([x, np.zeros(n - len(x))])


def make_loop(x, loop_len, xfade):
    """Make x (length >= loop_len + xfade) loop seamlessly: equal-power crossfade of the
    overshoot tail into the start, so the last sample flows into the first one."""
    n, xf = n_samples(loop_len), n_samples(xfade)
    out = x[:n].copy()
    t = np.linspace(0, np.pi / 2, xf)
    out[:xf] = x[:xf] * np.sin(t) + x[n:n + xf] * np.cos(t)
    return out


def wrap_tail(x, loop_len):
    """Fold anything past loop_len back onto the start (music with reverb tails)."""
    n = n_samples(loop_len)
    out = x[:n].copy()
    over = x[n:]
    out[:len(over)] += over
    return out


# ------------------------------------------------------------------ mastering
def peak_db(x):
    return 20 * np.log10(np.max(np.abs(x)) + 1e-12)


def normalize_peak(x, target_db=-1.2):
    return x * (db(target_db) / (np.max(np.abs(x)) + 1e-12))


def remove_dc(x):
    return x - np.mean(x, axis=0)


def soft_limit(x, ceiling_db=-1.0):
    """tanh soft limiter keeping |x| below the ceiling."""
    c = db(ceiling_db)
    return c * np.tanh(x / c)


def write_wav(path, x):
    """16-bit PCM WAV; x shape (n,) mono or (n, 2) stereo."""
    x = np.clip(x, -1.0, 1.0)
    data = (x * 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1 if x.ndim == 1 else x.shape[1])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)
