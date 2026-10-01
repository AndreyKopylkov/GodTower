"""Render every God Tower sound deterministically and sanity-check the results.

Run from the repo root:
    uv run --with numpy --with scipy --with pyloudnorm python Tools/Audio/generate_all.py
Output: Assets/_Project/Audio/Generated/*.wav (44.1 kHz, 16-bit; SFX mono, music stereo).
"""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parent))
import synth as S  # noqa: E402
import music  # noqa: E402

OUT = Path(__file__).resolve().parents[2] / "Assets" / "_Project" / "Audio" / "Generated"


# ------------------------------------------------------------------ building blocks
def thump(dur, f0, f1, tau, drive=1.5):
    """Pitch-dropping sine body."""
    return S.distort(S.osc("sine", S.sweep(f0, f1, dur), dur) * S.expdecay(dur, tau), drive)


def click(seed, dur=0.012, hp=2500):
    return S.highpass(S.noise(dur, seed), hp) * S.expdecay(dur, dur / 3)


def metal(seed, dur, base, tau):
    """Inharmonic ring-modulated metal hit."""
    ratios = [1.0, 1.47, 2.09, 2.56, 3.3, 4.12]
    out = np.zeros(S.n_samples(dur))
    rng = np.random.default_rng(seed)
    for r in ratios:
        out += S.osc("sine", base * r * (1 + rng.uniform(-0.02, 0.02)), dur) * rng.uniform(0.4, 1.0)
    return out * S.expdecay(dur, tau)


# ------------------------------------------------------------------------- SFX
def punch(variant):
    seed = 10 + variant
    pitch = [1.0, 1.18, 0.85][variant]
    dur = 0.4
    body = thump(dur, 220 * pitch, 55 * pitch, 0.07, 2.0)
    snap = S.bandpass(S.noise(dur, seed), 900 * pitch, 3200 * pitch) * S.expdecay(dur, 0.035)
    boing = S.osc("sine", S.sweep(520 * pitch, 190 * pitch, 0.12), 0.12) * S.expdecay(0.12, 0.05)
    x = S.mix((body, 0, 1.0), (snap, 0, 0.9), (boing, 0.004, 0.35), (click(seed), 0, 0.8))
    x = S.distort(x, 1.6)
    return S.fade(S.reverb(x, 0.18, 0.10, seed, 0.004), 0.001, 0.05)


def explosion(variant):
    seed = 20 + variant
    dur = [1.7, 2.1][variant]
    n = S.n_samples(dur)
    boom_noise = S.noise(dur, seed, "brown") + 0.5 * S.noise(dur, seed + 1)
    cut = S.sweep(7000 if variant == 0 else 5000, 150, dur)
    rumble = S.svf(boom_noise, cut, 0.8, "low") * S.expdecay(dur, 0.55 + 0.15 * variant)
    sub = thump(dur, 110, 32, 0.45, 1.8) * 1.2
    crack = S.highpass(S.noise(0.12, seed + 2), 1500) * S.expdecay(0.12, 0.03)
    debris = np.zeros(n)
    rng = np.random.default_rng(seed)
    for _ in range(14):
        off = rng.uniform(0.08, 0.9)
        c = S.bandpass(S.noise(0.04, int(rng.integers(1e6))), 1500, 6000) * S.expdecay(0.04, 0.01)
        debris[S.n_samples(off):S.n_samples(off) + len(c)] += c * rng.uniform(0.1, 0.4) * np.exp(-off * 2)
    x = S.mix(rumble, sub, (crack, 0, 0.8), debris)
    x = S.distort(x, 1.8)
    return S.fade(S.reverb(x, 0.9, 0.22, seed), 0.001, 0.15)


def truck_crash():
    dur = 1.5
    n = S.n_samples(dur)
    rng = np.random.default_rng(31)
    thud = thump(dur, 90, 38, 0.18, 2.2) * 1.3
    crunch = np.zeros(n)
    for i in range(18):  # scattered metal hits and bit-crushed grit
        off = rng.uniform(0.0, 0.5) ** 1.3
        m = metal(100 + i, 0.5, rng.uniform(280, 900), rng.uniform(0.03, 0.12))
        crunch[S.n_samples(off):S.n_samples(off) + len(m)] += m[:n - S.n_samples(off)] * rng.uniform(0.15, 0.5)
    grit = S.bitcrush(S.bandpass(S.noise(dur, 32), 400, 5000), 4, 3) * S.expdecay(dur, 0.12)
    glass = S.highpass(S.noise(0.5, 33), 4000) * S.expdecay(0.5, 0.1)
    rattle = np.zeros(n)
    for i in range(7):  # trailing clinks
        off = 0.45 + i * 0.1 + rng.uniform(-0.02, 0.02)
        m = metal(200 + i, 0.2, rng.uniform(600, 1500), 0.04) * (0.5 ** i)
        rattle[S.n_samples(off):S.n_samples(off) + len(m)] += m[:n - S.n_samples(off)] * 0.35
    x = S.mix(thud, crunch, (grit, 0, 0.7), (glass, 0, 0.3), rattle)
    x = S.distort(x, 1.7)
    return S.fade(S.reverb(x, 0.5, 0.15, 31), 0.001, 0.1)


def axe_whoosh():
    dur = 1.0
    n = S.n_samples(dur)
    t = S.time(dur)
    sw = 500 + 2300 * np.sin(np.linspace(0, np.pi, n)) ** 1.5
    x = S.svf(S.noise(dur, 41), sw, 2.5, "band")
    spin = 0.55 + 0.45 * np.sin(2 * np.pi * 7.0 * t)  # blade passing
    x = x * spin * S.bell(dur, 1.6)
    low = S.lowpass(S.noise(dur, 42), 300) * S.bell(dur, 2) * 0.6
    return S.fade(S.mix(x * 3, low), 0.01, 0.1)


def missile_flyby():
    dur = 1.5
    n = S.n_samples(dur)
    t = np.linspace(0, 1, n)
    amp = np.exp(-((t - 0.45) / 0.22) ** 2)
    fc = 600 + 3000 * amp
    jet = S.svf(S.noise(dur, 51), fc, 1.8, "band") * amp * 2.5
    f = 260 - 110 / (1 + np.exp(-12 * (t - 0.45)))  # doppler drop
    eng = S.lowpass(S.osc("saw", f, dur), 1800) * amp * 0.45
    rumble = S.lowpass(S.noise(dur, 52, "brown"), 200) * amp * 1.2
    return S.fade(S.mix(jet, eng, rumble), 0.02, 0.15)


def warning_beep():
    seq = [880, 660, 880, 660]
    parts, pos = [], 0.0
    for f in seq:
        d = 0.1
        tone = S.osc("square", f, d + 0.03, 0.5) * S.adsr(d, 0.004, 0.02, 0.8, 0.03)
        tone = S.lowpass(tone, 3500)
        parts.append((tone[:S.n_samples(d + 0.03)], pos, 1.0))
        pos += 0.14
    return S.fade(S.mix(*parts), 0.001, 0.02)


def jetpack():
    loop_len, xf = 2.0, 0.25
    dur = loop_len + xf
    t = S.time(dur)
    burn = S.svf(S.noise(dur, 61, "pink"), 1400 + 500 * np.sin(2 * np.pi * 1.0 * t * (loop_len / loop_len)), 0.9, "low")
    hiss = S.highpass(S.noise(dur, 62), 3000) * 0.18
    flutter = np.clip(0.8 + 0.5 * S.lowpass(S.noise(dur, 63), 40, 1), 0.3, 1.3)
    rumble = S.lowpass(S.osc("saw", 55, dur), 250) * 0.6
    x = (burn * flutter * 1.4 + hiss + rumble)
    return S.make_loop(x, loop_len, xf)


def phoenix():
    dur = 2.6
    n = S.n_samples(dur)
    t = S.time(dur)
    # screech: rising saw with fast vibrato, formant filtered
    f = S.sweep(900, 2600, 0.7)
    f = np.concatenate([f, 2600 * np.exp(-np.linspace(0, 0.25, n - len(f)))])
    f = f * (1 + 0.04 * np.sin(2 * np.pi * 14 * t))
    scr = S.osc("saw", f, dur)
    scr = S.svf(scr, 2200, 3.0, "band") * 2.0 + 0.3 * scr
    scr = S.distort(scr, 2.0)
    env = S.adsr(1.3, 0.04, 0.3, 0.7, 1.3)[:n]
    scr = scr * env
    # fire roar: noise with crackle swell
    roar = S.svf(S.noise(dur, 71), 900 + 700 * np.sin(np.linspace(0, np.pi, n)), 0.7, "low")
    roar *= (0.5 + 0.5 * S.lowpass(S.noise(dur, 72), 30, 1) / 0.1) * np.clip(S.bell(dur, 0.6), 0, 1)
    crackle = np.zeros(n)
    rng = np.random.default_rng(73)
    for off in rng.uniform(0, dur - 0.1, 40):
        c = S.highpass(S.noise(0.02, int(rng.integers(1e6))), 2000) * S.expdecay(0.02, 0.005)
        crackle[S.n_samples(off):S.n_samples(off) + len(c)] += c * rng.uniform(0.2, 0.6)
    x = S.mix(scr * 0.6, roar * 1.6, crackle * 0.5)
    return S.fade(S.reverb(x, 0.6, 0.15, 74), 0.01, 0.3)


def climb_step(variant):
    seed = 80 + variant
    p = [1.0, 1.12, 0.92][variant]
    dur = 0.18
    grab = S.lowpass(S.noise(dur, seed), 1800) * S.expdecay(dur, 0.025)
    body = thump(dur, 190 * p, 90 * p, 0.04, 1.2)
    tick = S.highpass(S.noise(0.01, seed), 3000) * S.expdecay(0.01, 0.003) * 0.4
    return S.fade(S.mix(grab * 0.9, body * 0.7, tick), 0.001, 0.04)


def lane_shift():
    dur = 0.28
    n = S.n_samples(dur)
    sw = S.sweep(500, 3500, dur)
    x = S.svf(S.noise(dur, 90), sw, 2.0, "band") * S.bell(dur, 1.3) * 3
    return S.fade(x, 0.01, 0.06)


def fall():
    dur = 1.1
    n = S.n_samples(dur)
    f = S.sweep(1900, 280, dur)
    f = f * (1 + 0.012 * np.sin(2 * np.pi * 9 * S.time(dur)))
    whistle = S.osc("sine", f, dur) + 0.25 * S.osc("sine", f * 2, dur)
    env = S.adsr(dur - 0.25, 0.02, 0.1, 0.8, 0.25)[:n]
    air = S.bandpass(S.noise(dur, 91), 1000, 4000) * 0.12 * env
    return S.fade(S.mix(whistle * env * 0.6, air), 0.005, 0.2)


def _brass(freq, dur, r=0.1, bright=3000):
    n_tot = dur + r
    f = np.broadcast_to(np.asarray(freq, dtype=float), (S.n_samples(n_tot),))
    x = (S.osc("saw", f * 0.997, n_tot) + S.osc("saw", f * 1.003, n_tot) + S.osc("square", f, n_tot, 0.4)) / 3
    env = S.adsr(dur, 0.025, 0.08, 0.75, r)
    cutoff = bright * (0.35 + 0.65 * np.minimum(1, np.arange(len(x)) / (0.12 * S.SR)))
    return S.svf(x, cutoff, 0.8, "low") * env


def win():
    notes = [(60, 0.0, 0.14), (64, 0.14, 0.14), (67, 0.28, 0.14), (72, 0.42, 0.28),
             (67, 0.78, 0.12), (72, 0.92, 0.9)]
    parts = []
    for m, off, d in notes:
        parts.append((_brass(S.midi(m), d, 0.12), off, 0.8))
        parts.append((_brass(S.midi(m - 12), d, 0.12, 1500), off, 0.4))
    for m in (76, 79, 84):  # sparkle on the final chord
        parts.append((S.osc("sine", S.midi(m), 1.0) * S.expdecay(1.0, 0.35), 0.92 + (m - 76) * 0.05, 0.22))
    x = S.mix(*parts)
    return S.fade(S.reverb(x, 0.9, 0.25, 101), 0.002, 0.4)


def lose():
    # sad trombone: Bb - A - Ab - G (final note slides down with wobble)
    plan = [(58, 0.0, 0.42), (57, 0.45, 0.42), (56, 0.9, 0.42), (55, 1.35, 1.0)]
    parts = []
    for i, (m, off, d) in enumerate(plan):
        f0 = S.midi(m)
        n = S.n_samples(d + 0.1)
        f = np.full(n, f0)
        if i == 3:
            f = f0 * np.exp(-np.linspace(0, 0.18, n) ** 1.5 * 3)
        t = np.arange(n) / S.SR
        f = f * (1 + 0.012 * np.sin(2 * np.pi * 5.5 * t) * np.minimum(1, t / 0.25))
        parts.append((_brass(f, d, 0.1, 1800), off, 0.9))
    return S.fade(S.reverb(S.mix(*parts), 0.5, 0.15, 102), 0.002, 0.3)


def countdown_tick():
    dur = 0.12
    x = S.osc("sine", 1200, dur) * S.expdecay(dur, 0.025)
    x += 0.4 * S.osc("sine", 2400, dur) * S.expdecay(dur, 0.012)
    return S.fade(S.mix(x, (click(5, 0.006, 4000), 0, 0.3)), 0.001, 0.02)


def ui_click():
    dur = 0.1
    x = S.osc("sine", S.sweep(900, 600, dur), dur) * S.expdecay(dur, 0.02)
    x += 0.35 * S.osc("sine", 1800, dur) * S.expdecay(dur, 0.01)
    return S.fade(x, 0.001, 0.02)


def ui_locked():
    parts = []
    for off, f in ((0.0, 220), (0.09, 165)):
        t = S.lowpass(S.osc("square", f, 0.12), 1200) * S.adsr(0.08, 0.003, 0.02, 0.8, 0.04)
        parts.append((t, off, 1.0))
    return S.fade(S.mix(*parts), 0.001, 0.03)


def ui_open():
    dur = 0.4
    sw = S.svf(S.noise(0.25, 7), S.sweep(600, 3000, 0.25), 2, "band") * S.bell(0.25, 1.2) * 1.5
    ch = [(S.osc("sine", S.midi(m), 0.4) * S.expdecay(0.4, 0.09), off, 0.5) for m, off in ((79, 0.08), (84, 0.16))]
    return S.fade(S.mix((sw, 0, 0.7), *ch), 0.003, 0.08)


# name -> (builder, target peak dBFS). Tonal/sustained sounds sit lower so they feel as loud as impacts.
SFX = {
    "sfx_punch_01": (lambda: punch(0), -1.2),
    "sfx_punch_02": (lambda: punch(1), -1.2),
    "sfx_punch_03": (lambda: punch(2), -1.2),
    "sfx_explosion_01": (lambda: explosion(0), -1.2),
    "sfx_explosion_02": (lambda: explosion(1), -1.2),
    "sfx_truck_crash": (truck_crash, -1.2),
    "sfx_axe_whoosh": (axe_whoosh, -1.5),
    "sfx_missile_flyby": (missile_flyby, -2.0),
    "sfx_warning_beep": (warning_beep, -9.0),
    "sfx_jetpack": (jetpack, -6.0),
    "sfx_phoenix": (phoenix, -2.0),
    "sfx_climb_step_01": (lambda: climb_step(0), -4.0),
    "sfx_climb_step_02": (lambda: climb_step(1), -4.0),
    "sfx_climb_step_03": (lambda: climb_step(2), -4.0),
    "sfx_lane_shift": (lane_shift, -2.0),
    "sfx_fall": (fall, -7.0),
    "sfx_win": (win, -2.0),
    "sfx_lose": (lose, -2.0),
    "sfx_countdown_tick": (countdown_tick, -3.0),
    "ui_click": (ui_click, -4.0),
    "ui_locked": (ui_locked, -9.0),
    "ui_open": (ui_open, -2.5),
}

LOOPS = {"sfx_jetpack", "music_level_loop", "music_menu_loop"}


def check(name, x, loop):
    """Sanity checks; returns (duration, peak dB, rms dB, notes)."""
    problems = []
    if not np.all(np.isfinite(x)):
        problems.append("NaN/inf")
    peak = S.peak_db(x)
    if peak > -1.0 + 1e-6:
        problems.append(f"peak {peak:.2f} > -1 dBFS")
    dc = float(np.max(np.abs(np.mean(x, axis=0))))
    if dc > 0.005:
        problems.append(f"DC offset {dc:.4f}")
    mono = x if x.ndim == 1 else x.mean(axis=1)
    if not loop and (abs(mono[0]) > 0.02 or abs(mono[-1]) > 0.005):
        problems.append("edge not faded")
    if loop:
        step = abs(mono[0] - mono[-1])
        typical = float(np.percentile(np.abs(np.diff(mono)), 99.5))
        if step > max(3 * typical, 0.02):
            problems.append(f"loop seam jump {step:.4f} vs typical {typical:.4f}")
    rms = 20 * np.log10(np.sqrt(np.mean(mono ** 2)) + 1e-12)
    return len(x) / S.SR, peak, rms, problems


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    report, failed = [], False
    for name, (fn, peak_target) in SFX.items():
        x = S.remove_dc(fn())
        x = S.normalize_peak(S.soft_limit(S.normalize_peak(x, peak_target + 3), peak_target + 1.5), peak_target)
        x = S.normalize_peak(x, peak_target)
        loop = name in LOOPS
        if not loop:
            x = S.fade(x, 0.0005, 0.005)
        S.write_wav(OUT / f"{name}.wav", x)
        report.append((name, 1, loop, *check(name, x, loop)))
    for name, x in music.render_all().items():
        S.write_wav(OUT / f"{name}.wav", x)
        report.append((name, 2, True, *check(name, x, True), music.lufs(x)))
    print(f"{'file':26} {'ch':>2} {'dur s':>7} {'peak':>7} {'rms':>7}  notes")
    for row in report:
        name, ch, loop, dur, peak, rms, problems = row[:7]
        extra = f"LUFS {row[7]:.1f}" if len(row) > 7 else ""
        failed |= bool(problems)
        print(f"{name:26} {ch:>2} {dur:7.2f} {peak:7.2f} {rms:7.1f}  {extra} {'; '.join(problems)}")
    if failed:
        print("SANITY CHECK FAILED")
        sys.exit(1)
    print("All files OK")


if __name__ == "__main__":
    main()
