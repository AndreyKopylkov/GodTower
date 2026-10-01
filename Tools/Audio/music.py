"""Procedural chiptune-style music for God Tower. Used by generate_all.py.

music_level_loop: 140 BPM, 40 bars (~68.6 s), drums + bass + arpeggio + lead, C major.
music_menu_loop : 110 BPM, 16 bars (~34.9 s), calm pad + bells + soft groove.
Loops are made seamless by folding the render tail (reverb + release) back onto the start.
"""
import numpy as np

import synth as S

TAIL = 2.0  # seconds rendered past the loop end and folded back
SCALE = [0, 2, 4, 5, 7, 9, 11]  # C major

# chord = (root pitch class, intervals)
MAJ, MIN = (0, 4, 7), (0, 3, 7)
C, G, Am, F, Em, Dm = (0, MAJ), (7, MAJ), (9, MIN), (5, MAJ), (4, MIN), (2, MIN)


class Track:
    def __init__(self, total):
        self.x = np.zeros(S.n_samples(total + TAIL))

    def add(self, sig, t0, gain=1.0):
        o = S.n_samples(t0)
        e = min(len(self.x), o + len(sig))
        if e > o:
            self.x[o:e] += sig[:e - o] * gain


def tone(kind, midi_note, gate, a=0.003, d=0.08, s=0.5, r=0.06, pw=0.5, vib=0.0):
    f = S.midi(midi_note)
    n = S.n_samples(gate + r)
    freq = np.full(n, f)
    if vib:
        t = np.arange(n) / S.SR
        freq = freq * (1 + vib * np.sin(2 * np.pi * 5.5 * t) * np.minimum(1, t / 0.15))
    x = S.osc(kind, freq, gate + r, pw)
    return x * S.adsr(gate, a, d, s, r)[:len(x)]


# ------------------------------------------------------------------- drum samples
def drum_kit():
    kick = S.distort(S.osc("sine", S.sweep(170, 48, 0.28), 0.28) * S.expdecay(0.28, 0.09), 1.8)
    kick[:S.n_samples(0.01)] += 0.4 * S.highpass(S.noise(0.01, 1), 2000) * S.expdecay(0.01, 0.003)
    snare = S.bandpass(S.noise(0.2, 2), 1200, 7000) * S.expdecay(0.2, 0.05)
    snare[:S.n_samples(0.12)] += 0.6 * S.osc("sine", S.sweep(240, 170, 0.12), 0.12) * S.expdecay(0.12, 0.04)
    hat = S.highpass(S.noise(0.05, 3), 7000) * S.expdecay(0.05, 0.012)
    ohat = S.highpass(S.noise(0.22, 4), 6500) * S.expdecay(0.22, 0.06)
    shaker = S.bandpass(S.noise(0.08, 5), 5000, 9000) * S.expdecay(0.08, 0.02)
    return kick, snare, hat, ohat, shaker


# ----------------------------------------------------------------- lead generation
def lead_phrase(seed, chords, bars_per_phrase=4, octave_base=72, rest_prob=0.0):
    """Yield (bar, step, length_steps, midi) for one phrase of bars; chord tones land on strong beats."""
    rng = np.random.default_rng(seed)
    rhythms = [
        [(0, 3), (3, 3), (6, 2), (8, 4), (12, 2), (14, 2)],
        [(0, 2), (2, 2), (4, 4), (8, 2), (10, 2), (12, 4)],
        [(0, 4), (4, 2), (6, 2), (8, 3), (11, 1), (12, 4)],
        [(0, 2), (2, 1), (3, 1), (4, 4), (8, 4), (12, 3), (15, 1)],
    ]
    out = []
    idx = int(rng.integers(0, len(SCALE)))  # scale index within octave
    octv = 0
    for b, chord in enumerate(chords):
        root, iv = chord
        tones = {(root + i) % 12 for i in iv}
        rh = rhythms[(b + int(rng.integers(0, 2)) * 2) % len(rhythms)]
        for k, (step, ln) in enumerate(rh):
            move = int(rng.choice([-2, -1, -1, 0, 1, 1, 2]))
            idx += move
            while idx < 0:
                idx += 7
                octv -= 1
            while idx > 6:
                idx -= 7
                octv += 1
            octv = int(np.clip(octv, -1, 1))
            if step % 4 == 0 or k == len(rh) - 1:  # snap to nearest chord tone
                best = min(range(7), key=lambda j: (0 if SCALE[j] in tones else 9) + abs(j - idx))
                idx = best
            if b == len(chords) - 1 and k == len(rh) - 1:  # phrase end rests on a stable tone
                idx = min(range(7), key=lambda j: (0 if SCALE[j] in tones else 9) + abs(j - 4))
            if rng.random() < rest_prob and step % 4 != 0:
                continue
            out.append((b, step, ln, octave_base + SCALE[idx] + 12 * octv))
    return out


# ------------------------------------------------------------------ level music
def level_loop():
    bpm = 140.0
    step = 60.0 / bpm / 4
    bar = step * 16
    prog = [
        [C, G, Am, F, C, G, F, G],      # 1 groove (no lead)
        [C, G, Am, F, C, G, F, G],      # 2 lead A
        [Am, F, C, G, Am, F, G, G],     # 3 bridge (pad, no kick on first half)
        [F, G, Em, Am, F, G, C, C],     # 4 lead B
        [C, G, Am, F, C, G, F, G],      # 5 final push, octave lead
    ]
    n_bars = 8 * len(prog)
    total = n_bars * bar
    kick, snare, hat, ohat, shaker = drum_kit()
    tk = {k: Track(total) for k in ("kick", "snare", "hat", "bass", "arpL", "arpR", "lead", "pad", "sub")}

    for sec, chords in enumerate(prog):
        lead_chords = chords[:4]
        if sec in (1, 3, 4):
            seed = 500 + sec
            for half in range(2):
                phrase_seed = seed  # second half repeats the first phrase's contour
                notes = lead_phrase(phrase_seed, chords[half * 4:half * 4 + 4], rest_prob=0.0 if sec != 3 else 0.1)
                for b, st, ln, m in notes:
                    t0 = (sec * 8 + half * 4 + b) * bar + st * step
                    tk["lead"].add(tone("square", m, ln * step * 0.9, a=0.004, d=0.05, s=0.7, r=0.05, pw=0.5, vib=0.004), t0, 0.5)
                    if sec == 4:
                        tk["lead"].add(tone("square", m + 12, ln * step * 0.9, a=0.004, d=0.05, s=0.5, r=0.05, pw=0.25), t0, 0.22)
        for bi, (root, iv) in enumerate(chords):
            gb = sec * 8 + bi
            t_bar = gb * bar
            last_bar = bi == 7
            kick_on = not (sec == 2 and bi < 4)
            # drums
            for beat in range(4):
                if kick_on:
                    tk["kick"].add(kick, t_bar + beat * 4 * step, 1.0)
                if beat in (1, 3):
                    tk["snare"].add(snare, t_bar + beat * 4 * step, 0.9)
            for e in range(8):
                tk["hat"].add(hat if e % 2 == 0 else ohat * 0.6, t_bar + e * 2 * step + (step if e % 2 else 0) * 0, 0.35 if e % 2 == 0 else 0.3)
            for s16 in range(16):
                if s16 % 2 == 1:
                    tk["hat"].add(shaker, t_bar + s16 * step, 0.12)
            if last_bar:  # fill: snare roll in the last beat
                for s16 in range(12, 16):
                    tk["snare"].add(snare, t_bar + s16 * step, 0.35 + 0.1 * (s16 - 12))
            # bass: root / octave bounce on 8ths
            for e in range(8):
                note = 36 + root + (12 if e % 4 == 3 else 0)
                if e % 8 == 6:
                    note = 36 + root + iv[2]
                tk["bass"].add(tone("square", note, step * 1.7, a=0.002, d=0.1, s=0.5, r=0.04, pw=0.5), t_bar + e * 2 * step, 0.55)
            tk["sub"].add(tone("sine", 24 + root + 12, bar * 0.97, a=0.01, d=0.2, s=0.8, r=0.05), t_bar, 0.5 if kick_on else 0.3)
            # arpeggio (pulse 25%), alternating pan; sparser in the pad bridge
            tones = [60 + root + i for i in iv] + [72 + root + iv[0]]
            pattern = [0, 1, 2, 3, 2, 1, 2, 1] * 2
            for s16 in range(16):
                m = tones[pattern[s16]] + (12 if sec == 4 else 0)
                tr = tk["arpL"] if s16 % 2 == 0 else tk["arpR"]
                tr.add(tone("square", m, step * 0.8, a=0.002, d=0.06, s=0.2, r=0.05, pw=0.25), t_bar + s16 * step, 0.2 if sec != 2 else 0.15)
            # pad
            if sec in (2, 3, 4) or bi >= 4:
                for i in iv:
                    tk["pad"].add(tone("tri", 48 + root + i, bar * 0.95, a=0.12, d=0.3, s=0.7, r=0.25), t_bar, 0.18)

    # mixdown
    bass = S.lowpass(tk["bass"].x, 1100)
    drums_l = tk["kick"].x * 0.95 + tk["snare"].x * 0.55 + tk["hat"].x * 0.4
    L = drums_l + bass + tk["sub"].x + tk["arpL"].x * 1.1 + tk["arpR"].x * 0.4 + tk["lead"].x * 0.95 + tk["pad"].x
    R = drums_l + bass + tk["sub"].x + tk["arpL"].x * 0.4 + tk["arpR"].x * 1.1 + tk["lead"].x * 1.0 + tk["pad"].x
    mixed = np.stack([L, R], axis=1)
    mixed = S.reverb(mixed, 0.7, 0.12, 9)
    return S.wrap_tail(mixed, total), total


# ------------------------------------------------------------------- menu music
def menu_loop():
    bpm = 110.0
    step = 60.0 / bpm / 4
    bar = step * 16
    chords = [C, Am, F, G] * 4
    total = len(chords) * bar
    kick, snare, hat, ohat, shaker = drum_kit()
    tk = {k: Track(total) for k in ("pad", "bass", "bell", "arp", "perc")}
    for gb, (root, iv) in enumerate(chords):
        t_bar = gb * bar
        for i in iv + (iv[0] + 12,):
            tk["pad"].add(tone("tri", 55 + root + i, bar * 0.98, a=0.35, d=0.5, s=0.7, r=0.5), t_bar, 0.14)
            tk["pad"].add(tone("sine", 67 + root + i, bar * 0.98, a=0.5, d=0.5, s=0.6, r=0.5), t_bar, 0.06)
        for beat in (0, 2):
            tk["bass"].add(tone("tri", 36 + root + (12 if beat == 2 else 0), step * 3.5, a=0.005, d=0.15, s=0.5, r=0.1), t_bar + beat * 4 * step, 0.6)
        pattern = [0, 2, 1, 2, 3, 2, 1, 2]
        tones = [60 + root + i for i in iv] + [72 + root]
        for e in range(8):
            tk["arp"].add(tone("sine", tones[pattern[e]] + 12, step * 2.2, a=0.003, d=0.15, s=0.0, r=0.2), t_bar + e * 2 * step, 0.25)
        tk["perc"].add(kick * 0.8, t_bar, 0.5)
        tk["perc"].add(kick * 0.6, t_bar + 8 * step, 0.4)
        tk["perc"].add(snare, t_bar + 4 * step, 0.18)
        tk["perc"].add(snare, t_bar + 12 * step, 0.18)
        for s8 in range(8):
            tk["perc"].add(shaker, t_bar + s8 * 2 * step + step, 0.15)
    # sparse bell melody: two 4-bar phrases per 8 bars, reused
    for rep in range(2):
        for half in range(2):
            ch = chords[(rep * 8 + half * 4):(rep * 8 + half * 4) + 4]
            for b, st, ln, m in lead_phrase(900 + half, ch, octave_base=72, rest_prob=0.35):
                t0 = (rep * 8 + half * 4 + b) * bar + st * step
                bell = S.osc("sine", S.midi(m), 0.9) + 0.35 * S.osc("sine", S.midi(m) * 2.76, 0.9) * S.expdecay(0.9, 0.08)
                tk["bell"].add(bell * S.expdecay(0.9, 0.25), t0, 0.22)
    pad = tk["pad"].x
    mono = tk["bass"].x * 0.9 + tk["perc"].x + tk["bell"].x + tk["arp"].x
    echoed = S.echo(tk["bell"].x + tk["arp"].x * 0.5, step * 3, 0.4, 3)[:len(mono)]
    pad_l = pad
    pad_r = np.roll(pad, 521)
    L = mono + echoed * 0.6 * 0.8 + pad_l
    R = mono + echoed * 0.6 + pad_r
    mixed = S.reverb(np.stack([L, R], axis=1), 1.2, 0.2, 12)
    return S.wrap_tail(mixed, total), total


# ------------------------------------------------------------------- mastering
def lufs(x):
    try:
        import pyloudnorm as pyln
        return pyln.Meter(S.SR).integrated_loudness(x)
    except ImportError:  # fallback: RMS in dBFS is within ~1-2 dB of LUFS for full-band music
        return 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)


def master(x, target=-16.0):
    x = S.remove_dc(x)
    for _ in range(4):
        x = x * S.db(target - lufs(x))
        x = S.soft_limit(x, -1.2)
    return x


def render_all():
    out = {}
    lvl, _ = level_loop()
    out["music_level_loop"] = master(lvl)
    men, _ = menu_loop()
    out["music_menu_loop"] = master(men, -18.0)
    return out
