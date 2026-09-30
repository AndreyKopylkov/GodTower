# Character Pipeline — brief for the Astra agent

Goal: produce the player character for **God Tower** — a 3D climber in a Roblox-like blocky style,
visually inspired by the character in the reference video — rigged, animated and ready for Unity (Android).

All work is AI-generated or done by agents. No human artist contributions (brief §4.3).

---

## 1. Inputs — study the reference first

Local, not in git (private material from the test brief):

| File | What to look at |
|---|---|
| `Reference/brief.md` | Full test brief + video breakdown notes at the end. |
| `Reference/ref.png` | Still frame: character on the tower, camera framing, scale vs tower. |
| `Reference/ref.mp4` | 103 s, 576×1080 portrait. Poses and motion to reproduce (timestamps below). |

Key observations:

- Camera looks at the tower from the front; the character is mostly seen **from the back / 3⁄4 back** while climbing.
  Back and silhouette readability matter more than face detail.
- Character height ≈ 1.2× tower column radius; small on screen (~12–15% of screen height).
- Motion is snappy and exaggerated (Roblox-like), not realistic.

Video timestamps:

| Time | Pose / motion |
|---|---|
| 1–8 s, 14–24 s, 55 s, 82 s | Climbing cycle: alternating arms reach up, legs push. Back view. |
| 10 s, 68 s, 86 s | Hit by explosion / truck — knocked off, falls down the tower. |
| 73–77 s | Hanging with both arms up (carried by a flying helper). |
| 91 s | Reaches the top platform, hangs under the dish edge. |
| 95–100 s | Hit by boxing gloves, then victory with trophy. |

---

## 2. Character spec

**Style:** original character *inspired by* the reference fighter, built in a Roblox-avatar style
(blocky torso and limbs, simple rounded head, flat colors, chunky proportions).

Keep (for visual match):
- Orange martial-arts gi (top + trousers), dark-blue undershirt, belt, wristbands and boots.
- Black spiky hair with a strong silhouette.

Must avoid (original work, no third-party IP):
- Kanji / emblems from any existing franchise (e.g. 悟, 亀). Use no emblem, or a simple original one
  (e.g. a small white circle with a stylised tower).
- An exact copy of any known character's hairstyle or face. Change the spike layout and count.
- Roblox logos or official Roblox assets. "Roblox style" = proportions and shading only.

**Technical budget (mobile):**

| Item | Target |
|---|---|
| Triangles | ≤ 8,000 |
| Materials | 1 (URP Lit or Simple Lit) |
| Textures | 1× albedo 1024², optional 1024² normal. No baked lighting in albedo. |
| Scale | 1 unit = 1 m, height ≈ 1.8 m |
| Orientation | Y-up, faces +Z, pivot at feet center |
| Rig | Humanoid-compatible skeleton (hips, spine, chest, neck, head, shoulders, upper/lower arms, hands, upper/lower legs, feet). Fingers optional (a single "fist" bone per hand is fine). |

---

## 3. Pipeline

### Step 1 — Concept turnaround (images)

Generate a **T-pose turnaround** of the same character:

- Views: front, back, left, right, 3⁄4 front, 3⁄4 back.
- **Transparent background** (PNG with alpha), no ground shadow.
- Orthographic-looking, flat neutral lighting, same scale and vertical alignment in every view.
- ≥ 1024×1024 per view.
- Arms straight out at shoulder height, palms down, legs slightly apart.

Check before moving on: colors and silhouette consistent across views; no IP markers from §2.

Output: `Art/Source/Character/Concept/hero_tpose_<view>.png`

### Step 2 — 3D model

1. Image-to-3D from the turnaround (front + back + side views).
2. Cleanup in Blender: remove floaters, fix symmetry, decimate / retopologise to budget,
   clean UVs, bake or re-project the albedo to one 1024² texture.
3. Apply transforms, set scale / orientation / pivot as in §2.

Output: `Art/Source/Character/hero.blend`

### Step 3 — Rig and skin

- Humanoid skeleton as in §2, T-pose as bind pose.
- Rigid-looking skinning is fine (Roblox style): limbs mostly weighted to single bones, soft only at joints.
- Test extreme poses (arms fully up, legs bent) for candy-wrapper twisting.

### Step 4 — Animations (from the video)

Watch `ref.mp4` at the timestamps in §1 and create these clips. **No root motion** — the game code moves the character.
All clips are at 30 fps.

| Clip | Loop | Length | Description |
|---|---|---|---|
| `Hero_ClimbUp` | yes | ~0.8 s | Alternating arm reach + leg push, body bobs slightly. Matches 1–8 s. |
| `Hero_HangIdle` | yes | ~2 s | Holding the wall with both hands, gentle sway, legs dangle a bit. Played when the player releases the finger. |
| `Hero_ShiftLeft` / `Hero_ShiftRight` | no | ~0.25 s | Quick sideways hop along the column (lane change). |
| `Hero_Hit` | no | ~0.4 s | Violent recoil: head snaps back, arms fly out. Matches 10 s / 68 s. |
| `Hero_Fall` | yes | ~0.6 s | Flailing arms and legs while knocked down the tower. Matches 86 s. |
| `Hero_Carried` | yes | ~1 s | Both arms up holding on, legs swing. Matches 73–77 s. |
| `Hero_Win` | no → hold last frame | ~1.5 s | Pulls up onto the top platform, then fist-pump / cheer. Matches 91–100 s. |
| `Hero_Lose` | no → hold last frame | ~1.2 s | Loses grip and drops, arms up in dismay. |

Output: one FBX with all clips as separate takes, or one FBX per clip with the same skeleton.

### Step 5 — Export to Unity

- FBX, binary, *Apply Transform* on, no cameras/lights, embedded textures off.
- Deliver into the Unity project:

```
Assets/Art/Characters/Hero/
    Hero.fbx                  # mesh + rig (T-pose)
    Hero_Animations.fbx       # all clips (or Hero_<Clip>.fbx per clip)
    Textures/Hero_Albedo.png  # 1024², sRGB
    Textures/Hero_Normal.png  # optional
```

Unity import settings (the game side will verify):
- Rig: **Humanoid**, avatar created from `Hero.fbx`; animation FBX copies that avatar.
- Loop Time on for looping clips; Root Transform rotation / position baked into pose.
- Texture: max size 1024, ASTC compression on Android, mipmaps on.

---

## 4. Acceptance criteria

- [ ] Imports into Unity 6000.5 without errors; Humanoid avatar is valid (green in Configure Avatar).
- [ ] ≤ 8k triangles, 1 material, textures within budget.
- [ ] All 8 clips present, named as above; looping clips have no pop at the seam.
- [ ] Reads clearly from the back at ~15% screen height (orange gi + black hair silhouette).
- [ ] No franchise emblems, kanji or copied likeness; no Roblox logos.
- [ ] Short note for the README: tools used for image generation, image-to-3D and animation, stating the assets are AI-generated.
