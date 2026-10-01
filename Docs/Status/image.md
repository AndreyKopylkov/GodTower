# Status — lane image

Updated by the lane agent. Format: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked.

Tool: procedural rendering in Python (Pillow + numpy + scipy, 4x supersampling, LANCZOS downscale). No AI image tool.
Regenerate everything (deterministic, ~15 s) from repo root:
`uv run --with pillow --with numpy --with scipy python Tools/Images/generate_all.py`
(scripts: `Tools/Images/{generate_all,common,icons,preview}.py`). Contact sheets: `Art/Source/UI/Previews/`; reference frames: `Art/Source/UI/RefFrames/`.
Logo font: Arial Black (`C:/Windows/Fonts/ariblk.ttf`), yellow-orange / blue gradient fill, white + navy double outline.
All PNG RGBA (app icon RGB, no alpha). No `.meta` files created. Import hints: Sprite (2D and UI), Alpha Is Transparency on, no mipmaps for UI.

## Tasks

- [x] I-01 Clouds x4 (512²)
- [x] I-02 UI kit
- [x] I-03 Event banners
- [x] I-04 Icons x14 (256²)
- [x] I-05 Logo (1024x512) + app icon (1024²)
- [x] I-06 This file

## Files delivered

Clouds (`Assets/Art/Environment/Sky/`): `cloud_01.png` … `cloud_04.png`, 512x512, soft white cloud with blue-grey underside, no outline, transparent.

9-slice borders are in px as Left / Right / Top / Bottom (Unity Sprite Editor: set Border L,B,R,T accordingly). Image Type = Sliced.

| File | Size | 9-slice L/R/T/B |
|---|---|---|
| `Assets/Art/UI/Kit/panel.png` | 256x256 | 64/64/64/64 |
| `Assets/Art/UI/Kit/button_normal.png` (green) | 256x112 | 48/48/48/48 |
| `Assets/Art/UI/Kit/button_pressed.png` | 256x112 | 48/48/48/48 |
| `Assets/Art/UI/Kit/button_disabled.png` | 256x112 | 48/48/48/48 |
| `Assets/Art/UI/Kit/button_blue.png` (secondary) | 256x112 | 48/48/48/48 |
| `Assets/Art/UI/Kit/round_button_normal.png` | 128x128 | none (simple) |
| `Assets/Art/UI/Kit/round_button_pressed.png` | 128x128 | none (simple) |
| `Assets/Art/UI/Kit/heightbar_frame.png` (vertical dark capsule) | 64x512 | 28/28/28/28 |
| `Assets/Art/UI/Kit/heightbar_fill.png` (yellow capsule) | 40x256 | 20/20/20/20 |
| `Assets/Art/UI/Banners/banner_hero_blue.png` (badge circle left, chevrons right) | 384x112 | 64/64/48/48 |
| `Assets/Art/UI/Banners/banner_villain_red.png` (badge circle right, chevrons left) | 384x112 | 64/64/48/48 |

Notes: heightbar frame interior inset is ~8 px per side — fill the bar with `heightbar_fill` inset ~10 px (Image Type Filled/vertical, or Sliced + RectTransform height). Banner badge circle is a blank white disc (put hero/villain icon in it). Pressed button face sits 6 px lower than normal (lip 2 px vs 9 px).

Icons (`Assets/Art/UI/Icons/`, 256x256, outlined, shadow baked inside bounds): `icon_missile`, `icon_truck`, `icon_axes`, `icon_jetpack`, `icon_phoenix`, `icon_boxing_glove`, `icon_lock`, `icon_star`, `icon_pause`, `icon_play`, `icon_home`, `icon_retry`, `icon_next`, `icon_trophy` (.png). White glyph icons (pause/play/home-walls/retry/next) read on coloured buttons.

Logo (`Assets/Art/UI/Logo/`): `logo_god_tower.png` 1024x512 transparent; `app_icon_1024.png` 1024x1024 RGB (tower with star + boxing glove on sky; full-bleed square, no rounded corners — Android adaptive masks it, keep important content central).

## Known issues

- Procedural art, not hand-painted: simple flat-gradient cartoon style; icon details are minimal.
- Missile/jetpack flames and some icons are slightly asymmetric by design; no animation frames.
