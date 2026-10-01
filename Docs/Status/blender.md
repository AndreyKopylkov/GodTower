# Status — lane blender

Updated by the lane agent. Format: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked.

## Tasks

- [x] B-01 — Reference study (notes below)
- [x] B-02 — 4 tileable tower segments
- [x] B-03 — Top platform (dish)
- [x] B-04 — Boxing glove
- [x] B-05 — Missile
- [x] B-06 — Truck
- [x] B-07 — Axe
- [x] B-08 — Jetpack
- [x] B-09 — Phoenix (+ 1 s flap loop)
- [x] B-10 — Trophy
- [x] B-11 — Status file complete

## Export conventions (all assets)

- FBX binary, `axis_forward=-Z`, `axis_up=Y`, `bake_space_transform=True` (Apply Transform), `FBX_SCALE_ALL`
  → in Unity: Y up, 1 unit = 1 m, root rotation (0,0,0), scale 1. Front of a model = Unity **+Z**.
- Normals exported (sharp edges by angle) → Unity import: Normals = Import, Tangents = Calculate Mikktspace.
- Normal maps are OpenGL convention (+Y = up) = Unity convention → set texture type **Normal map**.
- No cameras/lights in FBX. Textures referenced relatively from `Textures/` (materials in FBX are only hints; build URP Lit materials).

## B-02 / B-03 — Tower (`Assets/Art/Environment/Tower/`)

| File | Tris | Pivot | Notes |
|---|---|---|---|
| `Tower_Segment_Rings.fbx` | 2400 | base centre (y=0), height 3.0 | bead rings, boss rows, notched collar, ledge |
| `Tower_Segment_Zigzag.fbx` | 2000 | base centre, height 3.0 | zig-zag + wave frieze, chevrons, ledge |
| `Tower_Segment_Relief.fbx` | 2320 | base centre, height 3.0 | two recessed glyph bands (original glyphs), ledge |
| `Tower_Segment_Ribbed.fbx` | 2400 | base centre, height 3.0 | real vertical flutes, knurled band, ledge |
| `Tower_Top.fbx` | 5280 | joint centre (y=0) = top of last segment | fluted neck → bell → saucer; **deck surface y = 3.40**, deck radius 6.0, rim r 6.55, rim top y 3.66 |

- Column radius 1.5 m, every segment exactly 3.0 m, starts/ends with a plain 6 cm cylinder at r=1.5 → any order stacks seamlessly
  (place segment k at y = 3k; top at y = 3·count). Ledges stick out to r=1.80 m; ring beads to ≤ 1.6 m
  → hang the climber with its body centre at r ≥ ~1.95 m.
- Segments: one shared material **`M_TowerStone`** → `Textures/T_TowerStone_Albedo.png` + `Textures/T_TowerStone_Normal.png` (1024², 2×2 atlas).
  Recommended URP Lit: smoothness ~0.15, normal strength 1. Texture wrap: Repeat or Clamp both fine (UVs stay inside tiles).
- Top: material **`M_TowerTop`** → `Textures/T_TowerTop_Albedo.png` + `Textures/T_TowerTop_Normal.png` (1024²; deck mosaic + fluted sides).
- Previews: `Art/Source/Environment/Previews/tower_segments_close.png`, `tower_segments_stack.png`, `tower_top_under.png`, `tower_top_deck.png`.
- Generators: `Tools/Blender/Environment/{tower_spec.py, tower_textures.py, platform_textures.py, build_tower.py, build_platform.py}`, shared `Tools/Blender/Common/gtlib.py`.
  Rebuild: `uv run --with numpy --with scipy --with pillow python Tools/Blender/Environment/tower_textures.py` (+ `platform_textures.py`),
  then `blender.exe -b --factory-startup --python Tools/Blender/Environment/build_tower.py` (+ `build_platform.py`).

## B-04…B-10 — Props (`Assets/Art/Props/`) — shared material `M_Props`

All props share ONE material **`M_Props`** with palette texture **`Textures/T_PropPalette.png`** (256², 8×8 flat swatches of 32 px).
URP Lit: Base Map = palette, **Smoothness Source = Albedo Alpha** (alpha holds per-swatch smoothness), Metallic 0.
Import the palette with **Generate Mip Maps off** and Filter Mode Point (or Bilinear) so swatches never bleed. No normal maps on props.
Sizes are cartoon real-world metres; scale freely in prefabs.

| File | Tris | Size (m, X × Y × Z in Unity) | Pivot / orientation |
|---|---|---|---|
| `Prop_BoxingGlove.fbx` | 1464 | 0.50 × 0.44 × 0.72 | pivot = wrist (centre of the cuff opening); punches along **+Z**; back of hand +Y; right glove (thumb on −X). Mirror (scale.x = −1) for a left glove. Glossy red (smoothness 0.85), white cuff with red stripes. |
| `Prop_Missile.fbx` | 912 | 0.56 × 0.56 × 1.26 | pivot = centre; nose/flight direction **+Z**. Red/white, yellow ring, 4 tail fins + 4 canards; nozzle at z ≈ −0.63 (trail VFX). |
| `Prop_Truck.fbx` | 1404 | 1.50 × 1.79 × 2.63 | pivot = bounding-box centre (can tumble while falling); cab faces **+Z**; roof (+Y) has an orange band + white chevron, readable from above. |
| `Prop_Axe.fbx` | 1302 | 0.90 × 1.58 × 0.17 | pivot = bounding-box centre; handle along **Y** (head up), blades to ±X, blade faces ±Z → spin around Z for a saw-like sweep. |
| `Prop_Jetpack.fbx` | 1288 | 0.71 × 0.79 × 0.39 | pivot = back contact point (centre of the plate's front face); pack extends to **−Z** (behind a wearer facing +Z), up +Y; nozzle exits at (±0.135, −0.47, −0.17) for flame VFX. |
| `Prop_Phoenix.fbx` | 1274 | 2.80 wingspan × 1.6 × 2.85 | pivot = body centre; flies along **+Z**, up +Y, wings ±X. Generic rig `PhoenixRig` (Root > Body, Tail, Wing1.L > Wing2.L, Wing1.R > Wing2.R), rigid skinning. Clip **`PhoenixRig|Phoenix_Flap`**: frames 0–30 @ 30 fps = 1.0 s, first pose = last pose → Rig: Generic, Loop Time on. Optional glow: palette as Emission Map on a phoenix material instance. |
| `Prop_Trophy.fbx` | 1392 | 0.92 × 0.90 × 0.60 | pivot = bottom centre; red star + plaque face **+Z**. |

Prop previews: `Art/Source/Environment/Previews/prop_*.png`. Generators: `Tools/Blender/Props/build_<asset>.py` + shared `propslib.py`
(the palette PNG is rewritten identically by every prop script). Rebuild: `blender.exe -b --factory-startup --python Tools/Blender/Props/build_glove.py` (etc.).

## B-01 — Reference notes (`Reference/ref.mp4`, frames in `Art/Source/Environment/RefFrames/`)

- Column: one smooth cylinder, pale grey-green stone (lit ~#B4BCA4, shade ~#6E7766), soft matte, no visible texture noise.
  Width on screen ≈ 2.5× the character's shoulder width → matches radius 1.5 m with a 1.8 m character.
- Vertical rhythm: carved bands ~0.6–1 m tall separated by stacks of 2–4 thin horizontal rings (rounded lips),
  plus thin wide ledge plates (flat discs, ~1.3× column radius) every few metres; the character hangs just in front of them.
- Band motifs seen: zig-zag/triangle friezes over wavy lines, rows of round bosses (circle-in-circle), basket-weave/knurled band,
  vertical flutes/ribs, notched collar (rectangular crenels), animal/mask glyph reliefs (we use **original** glyphs only).
- Relief depth is shallow (2–5 cm) but strongly shaded; edges are rounded, chunky, toy-like.
- 91 s top: column → fluted collar → ring → bell-shaped fluted flare → wide saucer/dish underside (≈4–5× column radius) with radial ribs.
  100 s: flat circular deck on top with a raised rim and a patterned floor; trophy stands on it.
- Props seen: big glossy red boxing gloves with white/grey cuffs (bump), grey double-bladed axes (spinning swarm), jetpack with twin tanks,
  falling car/truck, missile, phoenix (fire bird).

## Files delivered

See tables above (FBX + `Textures/` PNGs only; no `.meta` written by this lane).

## Known issues

- Tower segment ends are open (no caps) — invisible in normal play; keep the bottom of the first segment below the cloud layer / camera view.
- Tower carvings are normal-map only (silhouette shows rings/ledges/flutes); at grazing angles the relief flattens — fine at phone size.
- `Tower_Top` (5.3k tris) exceeds the per-segment budget; it is a single unique piece.
- Props have no normal maps (flat palette style). Glove 1464 tris is above the "~500" hint but inside the 1.5k prop budget (hero effect).
- Phoenix wings are flat extruded plates (stylised low-poly); the fire look relies on the Unity VFX.
