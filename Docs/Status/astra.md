# Status — lane astra

Updated by the lane agent. Format: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked.

## Tasks

- [x] **A-01** Reference studied (`ref.png` + frames at 1–8, 10, 14–24, 55, 68, 73–77, 82, 86, 91, 95–100 s → `Art/Source/Character/RefFrames/`, gitignored).
  Notes: camera sees the climber from the back, hero ≈ 12–15% of screen height, centered; climbing = alternating
  overhead reach with the opposite knee up; hit = recoil then tumbling fall; carried/hanging = both arms straight up,
  legs swinging; at the top the hero hangs under the dish edge, then cheers. Silhouette that matters: orange gi + black spiky hair.
- [x] **A-02** "Turnaround" = Eevee renders of the model (no image-gen tool, see decisions A-02):
  `Art/Source/Character/Concept/hero_tpose_{front,back,left,right,threequarter_front,threequarter_back}.png` (1024², transparent),
  sheets `hero_turnaround_sheet.png`, `hero_clips_sheet.png`, `hero_small_read_test.png` (back read at ~15% / ~7% screen height on sky blue).
- [x] **A-03** Model built procedurally in Blender 5.2 Python (no image-to-3D tool): 3,436 tris, 1 material, 1024² albedo, 1.80 m, pivot at feet.
- [x] **A-04** Humanoid rig, T-pose bind, rigid skinning.
- [x] **A-05** 9 clips, 30 fps, no root motion.
- [x] **A-06** Exported to `Assets/Art/Characters/Hero/`; re-import verified in Blender (`verify_fbx.py` → VERIFY_OK).
- [x] **A-07** This status file.

## Files delivered

| File | Content |
|---|---|
| `Assets/Art/Characters/Hero/Hero.fbx` | Mesh `HeroBody` + armature `HeroRig`, T-pose, no takes. Binary FBX. |
| `Assets/Art/Characters/Hero/Hero_Animations.fbx` | Armature only + 9 takes (one per clip). |
| `Assets/Art/Characters/Hero/Textures/Hero_Albedo.png` | 1024² sRGB albedo atlas, flat colours, no baked lighting. |
| `Art/Source/Character/hero.blend` | Source scene (actions + one NLA track per clip). |
| `Art/Source/Character/Hero_Albedo.png` | Atlas source (copied into Assets by the build). |
| `Art/Source/Character/Concept/` | Preview renders + contact sheets. |
| `Tools/Blender/Hero/` | Generator: `hero_layout.py` (palette/atlas layout), `make_atlas.py` (Pillow), `build_hero.py` (Blender: mesh, UVs, rig, clips, FBX), `verify_fbx.py`, `render_previews.py`, `make_sheets.py`, `build_all.sh` (runs everything), `extract_ref_frames.py`. |

Rebuild: `bash Tools/Blender/Hero/build_all.sh` from the project root (needs Blender 5.2 + uv).

## Model

- Triangles: **3,436** (budget 8,000). Vertices 1,800. Material: `M_Hero` (one), texture `Hero_Albedo.png`.
- Height 1.80 m (feet at 0, top of hair spikes at 1.80). Y-up, faces +Z in Unity (Blender -Y), pivot at feet centre.
- Look: Roblox-style bevelled blocks; orange gi top + trousers, dark-blue undershirt (V at collar, short sleeves, back collar),
  dark-blue belt with knot, wristbands, navy boots with cream soles, black swept-back spiky hair (original 13-spike layout),
  painted face (big eyes, determined brows, grin, blush). Back emblem: original white disc with a gold stylised tower. No kanji, no franchise or Roblox marks.

## Rig

21 bones, names = Unity `HumanBodyBones`:
`Hips` → `Spine` → `Chest` → `Neck` → `Head`; `Chest` → `LeftShoulder` → `LeftUpperArm` → `LeftLowerArm` → `LeftHand` (same for Right);
`Hips` → `LeftUpperLeg` → `LeftLowerLeg` → `LeftFoot` → `LeftToes` (same for Right).
Rigid skinning (each block 100% on one bone). Spine, Shoulders and Toes carry no geometry (16 weighted groups).
Elbows/knees bend on a single hinge axis (upper limbs are twisted so Humanoid retargeting stays faithful).

## Clips (30 fps, start frame 0, FBX take name = clip name)

| Clip | Frames | Length | Loop | Notes |
|---|---|---|---|---|
| `Hero_ClimbUp` | 0–24 | 0.80 s | yes | Alternating overhead reach + opposite knee up, hips bob. Faces the wall. |
| `Hero_HangIdle` | 0–60 | 2.00 s | yes | Both hands up on the wall, gentle sway, dangling legs. |
| `Hero_ShiftLeft` | 0–8 | 0.27 s | no | Lean + reach to the hero's left (screen-left from behind), starts/ends in hang pose. |
| `Hero_ShiftRight` | 0–8 | 0.27 s | no | Mirror of ShiftLeft. |
| `Hero_Hit` | 0–12 | 0.40 s | no | Head snaps back, arms fly out; ends in the `Hero_Fall` first pose. |
| `Hero_Fall` | 0–20 | 0.67 s | yes | Windmilling arms, bicycling legs. |
| `Hero_Carried` | 0–30 | 1.00 s | yes | Both arms straight up, legs swing. |
| `Hero_Win` | 0–45 | 1.50 s | no (hold last) | Pull-up, stand, hop-turn 180° to face the camera, fist pump; last frame = cheer pose. |
| `Hero_Lose` | 0–36 | 1.20 s | no (hold last) | Grip slips, drops, arms up in dismay. |

Looping clips have identical first/last keys (no pop). No root motion: only small Hips offsets (bob/sway, ≤ 9 cm) that
Unity bakes into the pose.

## Unity import notes (for U-62)

- Rig: Humanoid, Create From This Model on `Hero.fbx`; `Hero_Animations.fbx` → Copy From Other Avatar (Hero avatar).
- Loop Time on: ClimbUp, HangIdle, Fall, Carried. Root Transform Rotation / Position (Y) / Position (XZ): Bake Into Pose
  (needed for the 180° turn in `Hero_Win`).
- The rig root may carry a -90° X node rotation (Blender axis conversion without the experimental Apply Transform); "Bake Axis Conversion" in the importer removes it if needed.
- Texture: sRGB, max 1024, ASTC, mipmaps. Material: URP Lit/Simple Lit with `Hero_Albedo` as base map, smoothness ~0.3.
- `.meta` files in this folder were created by Unity, not by this lane.

## Tools used (for README)

- Character concept, model, UVs, texture, rig and animation: fully generated by an AI agent (Claude, lane "Astra") writing
  Blender 5.2 Python scripts (procedural modelling + keyframed animation) and a Pillow script for the texture.
- No image-generation or image-to-3D service was used (none available); concept views are Blender renders of the generated model.
- Reference frames extracted with imageio-ffmpeg (study only, not shipped).

## Acceptance checklist (CharacterPipeline §4)

- [ ] Imports into Unity 6000.5 without errors; Humanoid avatar valid — **to be checked by lane U** (Unity not run by this lane). Bone names match HumanBodyBones, T-pose bind.
- [x] ≤ 8k triangles (3,436), 1 material, 1× 1024² texture.
- [x] All 9 clips present and named as specified (verified by re-import); looping clips have matching first/last frames.
- [x] Reads clearly from the back at ~15% screen height (`Concept/hero_small_read_test.png`).
- [x] No franchise emblems, kanji or copied likeness; no Roblox logos (original hair layout and tower emblem).
- [x] README note on tools (section above).

## Known issues

- Rigid blocky joints: elbows/knees show small gaps/overlaps at strong bends (intended Roblox look).
- Spine, Shoulders and Toes have no skinned geometry (the torso is one rigid block on `Chest`).
- Unity Humanoid avatar validity not tested here (no Unity in this lane).
- `Hero_Win` ends facing the camera (turned 180° from the climbing direction).

## Misplaced entries found in this file (written by other lanes, kept verbatim)

- I-01..I-06: all 2D art rendered procedurally with Pillow (no AI image tool); logo font Arial Black; hero/villain banner badges left blank for icons — O-01 follow-up, simplest option
- B-02: Tower segments share one 1024² atlas material `M_TowerStone` (2×2 tiles, one per segment; texture repeats 4× around via UV islands) — one material for all segments = static/SRP batching, smaller APK.
- B-02: Carvings are normal-map + baked cavity in albedo on a lathed 40-side column; rings/ledges/flutes are real geometry — keeps segments ≤ 2.4k tris.
- B-02: Ledge plates stick out to r = 1.80 m (column 1.5 m); the climber should hang with its body centre at r ≥ ~1.95 m to avoid clipping the ledges.
- B-03: Top platform `Tower_Top` is 5.3k tris (one-off hero piece, segment budget does not apply); walkable deck at y = 3.40 m above its pivot, deck radius 6.0 m, rim lip up to y = 3.66 m.
