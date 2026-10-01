# Status — lane sound

Updated by the lane agent. Format: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked · `[-]` cut.

## Tasks

- [x] S-01 Tooling: `Tools/Audio/synth.py` (oscillators, noise, ADSR, filters incl. sweeping SVF, distortion, bitcrush, reverb, echo, loop helpers, limiter), `Tools/Audio/music.py`, `Tools/Audio/generate_all.py`
- [-] S-02 Reference audio analysis (optional) — skipped; timbre/tempo chosen from Plan (cartoon SFX, 140 BPM music)
- [x] S-03 punch x3
- [x] S-04 explosion x2
- [x] S-05 truck crash
- [x] S-06 axe whoosh
- [x] S-07 missile flyby, warning beep
- [x] S-08 jetpack (loop), phoenix
- [x] S-09 climb step x3, lane shift, fall
- [x] S-10 win, lose, countdown tick
- [x] S-11 UI click/locked/open (generated, not CC0 picks)
- [x] S-12 level music loop, menu music loop

## Files delivered

All in `Assets/_Project/Audio/Generated/`, 44.1 kHz 16-bit WAV. SFX mono, music stereo.

| File | Dur (s) | Peak dBFS | Event / purpose |
|---|---|---|---|
| sfx_punch_01/02/03.wav | 0.58 | -1.2 | Bump gloves hitting the hero (webhook effect); pick randomly, vary pitch slightly |
| sfx_explosion_01/02.wav | 2.61 / 3.01 | -1.2 | Missile impact |
| sfx_truck_crash.wav | 2.01 | -1.2 | Truck lands on lane |
| sfx_axe_whoosh.wav | 1.00 | -1.5 | Spinning axes sweep |
| sfx_missile_flyby.wav | 1.50 | -2.0 | Missile passing (during flight) |
| sfx_warning_beep.wav | 0.55 | -9.0 | Villain telegraph (4 alternating beeps) |
| sfx_jetpack.wav | 2.00 | -6.0 | Jetpack hero event; **loopable** (seamless crossfaded) |
| sfx_phoenix.wav | 3.21 | -2.0 | Phoenix hero event (screech + fire roar) |
| sfx_climb_step_01/02/03.wav | 0.18 | -4.0 | Climbing grab; cycle randomly while holding |
| sfx_lane_shift.wav | 0.28 | -2.0 | Swipe lane change |
| sfx_fall.wav | 1.10 | -7.0 | Hit -> fall (descending whistle) |
| sfx_win.wav | 3.23 | -2.0 | Win screen fanfare |
| sfx_lose.wav | 2.96 | -2.0 | Lose screen, sad trombone |
| sfx_countdown_tick.wav | 0.12 | -3.0 | Timer last seconds tick |
| ui_click.wav | 0.10 | -4.0 | Button press |
| ui_locked.wav | 0.21 | -9.0 | Locked level tapped |
| ui_open.wav | 0.56 | -2.5 | Screen/panel open |
| music_level_loop.wav | 68.57 | -7.6 | In-level music, 140 BPM, 40 bars, ~-16.2 LUFS, seamless loop |
| music_menu_loop.wav | 34.91 | -8.2 | Menu music, 110 BPM, 16 bars, ~-18.1 LUFS, seamless loop |

Per-file peak targets differ on purpose (tonal sounds sit lower) so perceived loudness is similar (RMS roughly -11..-17 dBFS).
Import tips: set `sfx_jetpack`, both music files to loop; music as Streaming / Vorbis, SFX Decompress On Load.

## How to regenerate

```
uv run --with numpy --with scipy --with pyloudnorm python Tools/Audio/generate_all.py
```
Deterministic (fixed seeds, verified identical on re-run). The script also checks: finite values, peak <= -1 dBFS, DC offset,
faded edges, loop seam continuity (jetpack, music), and prints a table.

## Known issues

- Sounds were not auditioned by ear (no human); verified numerically only. Tweak in `generate_all.py` if something sounds off.
- S-02 skipped (optional).
