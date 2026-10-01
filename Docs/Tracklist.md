# God Tower — Tracklist (for the orchestrator session)

Complete task list for the whole test, from kickoff to submission.
Design and tech decisions are **locked** in [`Plan.md`](Plan.md) — read it first. This file says *what to do, who does it, in what order*.
Character details: [`CharacterPipeline.md`](CharacterPipeline.md). Brief: `Reference/brief.md` (local, gitignored).

---

## 0. Rules for the orchestrator

- **No human for ~8 hours.** Never wait for answers. On ambiguity: choose the simplest option consistent with `Plan.md`,
  implement it, append one line to `Docs/Status/decisions.md` (`- <task id>: <decision> — <reason>`).
- **Single writer of this file** = orchestrator. Update statuses at every checkpoint:
  `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked · `[-]` cut.
- **Lanes** (agents): `O` orchestrator · `U` Unity agent · `A` Astra (character) · `B` Blender agent · `S` sound agent · `I` image agent · `H` human (morning only, do not execute).
- **Unity lock:** only lane `U` runs Unity, one process at a time, Unity Editor closed. Other lanes never touch Unity or `ProjectSettings`/`Packages`.
- **Git:** only lane `U` commits and pushes (`main`, English conventional commits, trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`).
  Commit at the end of every milestone and after every asset integration.
- **Asset handoff:** asset lanes write only into their folders and keep `Docs/Status/<lane>.md` current
  (done items, exact file paths, triangle counts, known issues). When an asset is marked done, the orchestrator
  unblocks the matching `U` integration task. Until then `U` uses primitive placeholders.
- **Language:** all code comments, docs, commit messages, README — English.
- **Checkpoints:** every ~60 min (see §9). Behind schedule → apply the cut order (§10).
- **Never cut:** webhook `/bump`, 5 completable levels, bump effect, APK build, README.

Task format: `ID · owner · depends on` — deliverable. **Done when:** acceptance check.

---

## 1. Phase O — kickoff

- [x] **O-01 · O** — Environment check.
  **Done when:** no `Unity.exe` process has this project open; `C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe` exists;
  `.../PlaybackEngines/AndroidPlayer/{SDK,NDK,OpenJDK}` exist; `uv --version` works; `git status` clean on `main`;
  `Assets/JMO Assets` and `Assets/Hovl Studio` present locally; `Reference/{brief.md,ref.png,ref.mp4}` present.
  Any failure → mark `[!]`, log in `decisions.md`, continue with what is possible.
- [x] **O-02 · O** — Create `Docs/Status/{unity,astra,blender,sound,image}.md` (header + empty checklist).
- [x] **O-03 · O** — Spawn lanes `U`, `A`, `B`, `S`, `I` in parallel. Each prompt: its section of this file + `Plan.md` + (A) `CharacterPipeline.md`
  + reference paths + the ownership/coordination rules from §0.
- [~] **O-04 · O** — Run the checkpoint loop (§9) until all non-`H` tasks are `[x]` or `[-]`.
- [ ] **O-05 · O** — Final report in `Docs/Status/summary.md`: what is done, what is cut, known issues, morning to-do (lane `H`).

---

## 2. Lane U — Unity agent (serial, the critical path)

### M0 — Project setup (≈0.5 h)

- [ ] **U-01 · U · O-01** — Add packages to `Packages/manifest.json`: VContainer (git URL), UniTask (git URL), PrimeTween (OpenUPM scoped registry),
  `com.unity.cinemachine` 3.x, `"testables": ["com.unity.inputsystem"]`. **Done when:** batchmode import resolves with no errors.
- [ ] **U-02 · U** — Remove template leftovers: `Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene.unity`, unused default input actions if replaced.
  **Done when:** no references broken, compiles.
- [ ] **U-03 · U** — Folder layout per `Plan.md` §3 and asmdefs `GodTower.Runtime`, `GodTower.Editor`, `GodTower.Tests.EditMode`, `GodTower.Tests.PlayMode`.
- [ ] **U-04 · U** — `GodTower.Editor.BuildTools.SetupProject` (CLI entry): portrait only, IL2CPP, ARM64 only, min API 26,
  package `com.andreykopylkov.godtower`, product name "God Tower", Internet Access = Require, Android quality → `Mobile_RPAsset`, target 60 fps, ASTC.
  **Done when:** method runs in batchmode and `ProjectSettings` reflect all values.
- [ ] **U-05 · U** — `BuildTools.BuildAndroid` (CLI entry) → `Builds/GodTower.apk`. Not run yet, only compiles.
- [ ] **U-06 · U** — VContainer root: `RootLifetimeScope` via VContainerSettings; scenes `Menu.unity`, `Game.unity` with `MenuLifetimeScope`/`GameLifetimeScope`;
  build scene list = Menu, Game. **Done when:** both scenes load in a PlayMode smoke test. **Commit** `chore: project setup`.

### M1 — Webhook `/bump` (≈1.5 h) — highest priority feature

- [ ] **U-10 · U · U-06** — `BumpRequestParser` (pure C#): parse request line, method, path; tolerate headers/body; reject malformed.
- [ ] **U-11 · U** — `BumpResponse` builder: 200 `{"status":"triggered"}`, 409 `{"status":"ignored","reason":"not_playing"}`, 404, 405, 400; JSON, `Connection: close`.
- [ ] **U-12 · U** — `BumpHttpServer`: `TcpListener` on `IPAddress.Loopback:56789`, background thread, cancellation, `IDisposable`;
  port in use → error log + game keeps running; started from root scope, disposed on quit.
- [ ] **U-13 · U** — `MainThreadDispatcher` (`ITickable`, `ConcurrentQueue`), request → main thread → `TaskCompletionSource` answer, 1 s timeout.
- [ ] **U-14 · U** — `IPlayState` (Playing / Paused / Menu / Result) + gate deciding 200 vs 409; `BumpRequested` C# event for the effect.
- [ ] **U-15 · U** — EditMode tests: parser (valid GET/POST, query string, lowercase method, garbage), responses (codes, headers, body).
- [ ] **U-16 · U** — PlayMode test with real `HttpClient`: 200 while playing, 409 in menu and while paused, 404, 405; 10 parallel requests → all answered.
  **Done when:** EditMode + PlayMode green via CLI. **Commit** `feat: webhook bump server`.

### M2 — Core loop (≈2 h)

- [ ] **U-20 · U · U-06** — `ClimbInput`: Input System touch + mouse (editor); hold state; swipe detection (horizontal delta ≥ 8% screen width within 0.35 s, works while holding).
  EditMode tests for swipe classification.
- [ ] **U-21 · U** — `LaneModel` (pure): 3 lanes (−35°, 0°, +35°), clamp at edges. Tests.
- [ ] **U-22 · U** — `KnockdownModel` (pure): villain −8% tower height, bump −3% with cap 6% per rolling 5 s, floor at 0. Tests.
- [ ] **U-23 · U** — `ClimberController` state machine: Idle(hang) / Climb / Shift / Hit / Fall / Carried / Win / Lose;
  position on the cylinder arc (column radius 1.5 m + offset), climb speed 6 m/s; Animator parameter mapping ready for the 9 clips; capsule placeholder.
- [ ] **U-24 · U** — `TowerBuilder`: stacks tower segments up to `LevelConfig.height` from a `TowerSet` config, adds top platform; cylinder placeholders.
- [ ] **U-25 · U** — `LevelConfig`, `LevelCatalog`, `SelectedLevel` service; `Level_01.asset` from `Plan.md` table.
- [ ] **U-26 · U** — `LevelRunner`: countdown timer, win at top, lose on timeout, pause/resume, drives `IPlayState`.
- [ ] **U-27 · U** — Camera: Cinemachine follow on height with damping, framing like `ref.mp4` (character centered, ~12–15% screen height),
  Impulse source/listener, zoom-out hook for hero boosts.
  **Done when:** Level 1 is playable in PlayMode with placeholders (hold/release/swipe/win/lose). **Commit** `feat: core climbing loop`.

### M3 — Level events (≈1 h)

- [ ] **U-30 · U · U-26** — `EventTimeline`: seeded schedule from `LevelConfig` (villain interval, allowed villains, hero event times). Ordering tests.
- [ ] **U-31 · U** — Telegraph: lane warning marker + red banner (right side) for `telegraph` seconds before impact.
- [ ] **U-32 · U** — Villain **Missile**: horizontal flight into one lane at character height; hit → `Hit`/`Fall`/knockdown; explosion FX (CFX).
- [ ] **U-33 · U** — Villain **Truck**: falls from above into one lane; crash FX + shake.
- [ ] **U-34 · U** — Villain **Axes**: spinning axes sweep two lanes, one safe lane.
- [ ] **U-35 · U** — Hero **Jetpack**: +10% height over ~2 s, `Carried` state, blue banner (left).
- [ ] **U-36 · U** — Hero **Phoenix**: +20% over ~3 s, full-screen fire VFX (Hovl), camera zoom-out.
- [ ] **U-37 · U** — Event banners UI: blue left (heroes) / red right (villains), slide-in stack like the reference, own naming (e.g. "Missile ×1"), no "TikTikBox" text.
  **Done when:** every event fires per config, hits and dodges behave correctly. **Commit** `feat: level events`.

### M4 — Bump effect (≈1 h)

- [ ] **U-40 · U · U-14, U-23** — `BumpEffectConfig` SO (glove count 6–8, arc duration, flash, impulse, knockdown values).
- [ ] **U-41 · U** — Glove wave: pooled 3D gloves (placeholder until B-04), spawn from random screen edges, arc to the character (PrimeTween), punch-in rotation.
- [ ] **U-42 · U** — Contact: CFX hit bursts, full-screen white flash overlay, Cinemachine impulse, punch SFX (placeholder until S).
- [ ] **U-43 · U** — Gameplay: `Hit` animation, input locked 0.5 s, knockdown via `KnockdownModel`.
- [ ] **U-44 · U** — Layering: max 3 concurrent waves, extra requests queued; PlayMode stress test: 20 requests in 2 s → no errors, play continues, level still completable.
  **Commit** `feat: bump boxing glove event`.

### M5 — UI and flow (≈1.5 h)

- [ ] **U-50 · U** — Font: bold casual font from Google Fonts (OFL/Apache) + license file in repo; TMP font asset.
- [ ] **U-51 · U** — `ProgressService` (`PlayerPrefs`, sequential unlock). Tests.
- [ ] **U-52 · U** — Menu scene: main menu (title, Play, Levels), level select (5 buttons, locked state, level number + best result optional).
- [ ] **U-53 · U** — HUD: left vertical height bar with max value on top, current height number and character marker (like `ref.png`/video), timer, pause button, SafeArea.
- [ ] **U-54 · U** — Pause panel (Resume / Restart / Menu); pause → `/bump` returns 409.
- [ ] **U-55 · U** — Win panel (trophy, Next / Menu) and Lose panel (Retry / Menu).
- [ ] **U-56 · U** — Scene transitions with fade (UniTask), no double-loads.
  **Done when:** Menu → Level 1 → Win → Next → Level 2 → Pause → Menu works in PlayMode. **Commit** `feat: menus and HUD`.

### M6 — Content, asset integration, polish (≈1 h + integrations as assets arrive)

- [ ] **U-60 · U · U-30** — Author `Level_01..05` exactly per `Plan.md` level table.
- [ ] **U-61 · U** — 5 `SkyPreset`s (sky gradient, fog, directional light color, cloud tint): Day → Bright afternoon → Golden hour → Sunset → Dusk.
- [ ] **U-62 · U · A-07** — Integrate hero: Humanoid import, avatar, `Hero.controller` with 9 clips mapped to states, replace capsule.
- [ ] **U-63 · U · B-03** — Integrate tower segments + top platform into `TowerSet`, materials, static batching.
- [ ] **U-64 · U · B-04..B-09** — Integrate props: glove, missile, truck, axe, jetpack, phoenix.
- [ ] **U-65 · U · S-12** — `AudioService` + mixer (Music/SFX), clip mapping for all events and UI; music loop in levels.
- [ ] **U-66 · U · I-02..I-05** — Integrate UI sprites (panels, buttons, banners, icons, logo), app icon.
- [ ] **U-67 · U** — Visual polish: URP bloom (needed by Hovl FX), HDR on, lighting/colors matched to `ref.mp4` (saturated blue sky, pale grey-green stone),
  cloud layer, texture import settings (≤1024, ASTC, mipmaps), SRP Batcher on.
- [ ] **U-68 · U** — Asset hygiene: nothing from Asset Store demo scenes referenced; no missing references (editor validation script over all scenes/prefabs).
  **Commit** after each integration: `feat: integrate <asset>`.

### M7 — Verification and delivery (≈1 h)

- [ ] **U-70 · U · M5** — Autoplay harness (PlayMode): bot player via `InputTestFixture` (hold, release, swipes away from telegraphed lanes),
  "commentators" = background client sending `/bump` at seeded random intervals (4–10 s), screenshot capture 1080×1920
  (start, first villain hit, first bump, hero boost, win) → `TestResults/Screenshots/Level_0N_*.png`. Run without `-nographics`.
- [ ] **U-71 · U · U-60, U-70** — Autoplay **Level 1**: Win before timer, zero errors in log, all in-play `/bump` → 200.
- [ ] **U-72 · U** — Autoplay **Level 2** (same criteria).
- [ ] **U-73 · U** — Autoplay **Level 3**.
- [ ] **U-74 · U** — Autoplay **Level 4**.
- [ ] **U-75 · U** — Autoplay **Level 5**. If a level is not winnable by the bot, tune that level's numbers (log in `decisions.md`), never the bot to cheat.
- [ ] **U-76 · U** — Full-flow PlayMode test: Menu → Level 1…5 with unlocks, bumps throughout.
- [ ] **U-77 · U** — Build `Builds/GodTower.apk` via CLI (not committed). Record APK size in `Docs/Status/unity.md`; target < 100 MB.
- [ ] **U-78 · U** — Write (do not run) `Tools/Video/record_playthrough.py` for the morning: waits for device, `adb forward tcp:56789 tcp:56789`,
  starts `scrcpy --record`, drives the APK with `adb shell input` (menu taps, hold = long swipe on one point, lane swipes),
  sends `/bump` at random intervals, levels 1–5 in order. Coordinates relative to screen size (from `adb shell wm size`).
- [ ] **U-79 · U** — Final README: overview + "test task, webhook is the main feature"; how to play; webhook trigger in editor (`curl -X POST http://localhost:56789/bump`)
  and on device (`adb forward`, why not `adb reverse`), response codes; architecture overview; how to run tests and build via CLI;
  third-party asset table (licenses, links, AI-generated items with tools used); Asset Store import steps; Assumptions (from `decisions.md`);
  known issues; Unity version.
- [ ] **U-80 · U** — Final commit + push, tag `v1.0-overnight`.

---

## 3. Lane A — Astra (character) · follows `CharacterPipeline.md`

- [ ] **A-01 · A** — Study `Reference/ref.mp4` at the listed timestamps and `ref.png`; note proportions and poses in `Docs/Status/astra.md`.
- [ ] **A-02 · A** — T-pose turnaround (front, back, left, right, ¾ front, ¾ back), transparent PNG, ≥1024² → `Art/Source/Character/Concept/`.
  Original "inspired by" fighter, no franchise IP.
- [ ] **A-03 · A** — Image-to-3D + Blender cleanup: ≤ 8k tris, 1 material, 1024² albedo, 1 unit = 1 m, height 1.8 m, Y-up, faces +Z, pivot at feet.
- [ ] **A-04 · A** — Humanoid rig + skinning, T-pose bind.
- [ ] **A-05 · A** — 9 clips at 30 fps, no root motion: `ClimbUp`, `HangIdle`, `ShiftLeft`, `ShiftRight`, `Hit`, `Fall`, `Carried`, `Win`, `Lose` (names prefixed `Hero_`).
- [ ] **A-06 · A** — Export to `Assets/Art/Characters/Hero/` (`Hero.fbx`, `Hero_Animations.fbx`, `Textures/`).
- [ ] **A-07 · A** — Status file: file list, tri count, tools used (for README), acceptance checklist from `CharacterPipeline.md` §4 ticked.

---

## 4. Lane B — Blender agent (environment and props)

Scale contract: character 1.8 m; **column radius 1.5 m**; tower segment height **3 m** (all segments same radius and height, seamless when stacked).
Budgets: segment ≤ 3k tris, prop ≤ 1.5k tris, textures ≤ 1024², one material per asset. Style: chunky, bright, Roblox-like. FBX: Y-up, +Z forward, Apply Transform.

- [ ] **B-01 · B** — Study the tower in `ref.mp4` (1–23 s: carved drums, rings, zig-zag friezes, animal/glyph reliefs; 91 s: dish-shaped top). Notes → `Docs/Status/blender.md`.
- [ ] **B-02 · B** — 4 tileable tower segments: plain drum with rings, zig-zag frieze, relief band (original glyphs — no copied symbols), ribbed band.
  Pale grey-green stone material, normal map for carving. → `Assets/Art/Environment/Tower/`.
- [ ] **B-03 · B** — Top platform: wide dish/saucer cap + short neck, same radius at the joint. → same folder.
- [ ] **B-04 · B** — Boxing glove: red, glossy, white cuff; pivot at wrist; ~500 tris. → `Assets/Art/Props/`.
- [ ] **B-05 · B** — Missile: cartoon, red/white, fins; pivot at center, flies along +Z.
- [ ] **B-06 · B** — Truck: cartoon delivery truck, readable from above and front.
- [ ] **B-07 · B** — Axe: double-bladed, spinnable around its center.
- [ ] **B-08 · B** — Jetpack: two tanks + nozzles; attaches to the character's back (pivot at the back contact point).
- [ ] **B-09 · B** — Phoenix: low-poly stylised bird (≤ 2k tris), simple wing-flap animation loop (1 s); fire VFX is added in Unity.
- [ ] **B-10 · B** — Trophy (gold cup) for the win screen / top platform.
- [ ] **B-11 · B** — Status file: file list, tri counts, pivots/orientation notes.

---

## 5. Lane S — Sound agent (procedural, Python via `uv`)

Output: WAV 44.1 kHz 16-bit mono (music stereo) → `Assets/_Project/Audio/Generated/`; generator scripts → `Tools/Audio/` (committed by U).
Loudness: SFX peaks ≤ −1 dBFS, consistent perceived level; music around −16 LUFS integrated.

- [ ] **S-01 · S** — Tooling: `Tools/Audio/synth.py` (oscillators, noise, ADSR, filters, pitch sweeps, distortion, reverb tail), run via `uv run --with numpy --with scipy`.
- [ ] **S-02 · S** — Reference analysis (optional, ≤ 20 min): extract audio from `Reference/ref.mp4` with `uv run --with imageio-ffmpeg`,
  compute onsets/spectra around 10 s, 68 s, 86 s, 95–100 s; write findings to `Docs/Status/sound.md` to guide timbre and tempo.
- [ ] **S-03 · S** — `sfx_punch` ×3 variations (glove impact, comedic "thwack").
- [ ] **S-04 · S** — `sfx_explosion` ×2 (missile).
- [ ] **S-05 · S** — `sfx_truck_crash` (metal crunch + thud).
- [ ] **S-06 · S** — `sfx_axe_whoosh` (spinning swoosh).
- [ ] **S-07 · S** — `sfx_missile_flyby`, `sfx_warning_beep` (telegraph).
- [ ] **S-08 · S** — `sfx_jetpack` (loopable burn), `sfx_phoenix` (screech + fire roar).
- [ ] **S-09 · S** — `sfx_climb_step` (soft grab, ×3), `sfx_lane_shift` (swish), `sfx_fall` (descending whistle).
- [ ] **S-10 · S** — `sfx_win` (fanfare), `sfx_lose` (sad trombone-like), `sfx_countdown_tick`.
- [ ] **S-11 · S** — UI: `ui_click`, `ui_locked`, `ui_open` (or pick matching CC0 samples from Dustyroom pack and list them instead).
- [ ] **S-12 · S** — `music_level_loop` (60–90 s seamless loop, upbeat, ~140 BPM) and `music_menu_loop` (30–60 s). Status file with list and durations.

---

## 6. Lane I — Image agent (GPT Codex)

Output: PNG with alpha where relevant → `Assets/Art/UI/` and `Assets/Art/Environment/Sky/`. Style: bright casual mobile game, thick outlines, readable at phone size.
No text baked into buttons/banners (text is TMP), except the logo.

- [ ] **I-01 · I** — Cloud sprites ×4 (soft cartoon clouds like the reference), transparent, 512².
- [ ] **I-02 · I** — UI kit: panel 9-slice, button 9-slice (normal/pressed/disabled), round icon button, height-bar frame + fill.
- [ ] **I-03 · I** — Event banners 9-slice: blue (hero, left) and red/orange (villain, right), like the reference banner shapes.
- [ ] **I-04 · I** — Icons 256²: missile, truck, axes, jetpack, phoenix, boxing glove, lock, star, pause, play, home, retry, next, trophy.
- [ ] **I-05 · I** — Logo "God Tower" (transparent, 1024×512) and app icon 1024².
- [ ] **I-06 · I** — Status file: file list, sizes, 9-slice border values, tool used.

---

## 7. Dependency map (critical path)

```
O-01 → U-01…U-06 → U-10…U-16 (webhook) → U-20…U-27 (core) → U-30…U-37 (events) → U-40…U-44 (bump)
     → U-50…U-56 (UI) → U-60/U-61 → U-70 → U-71…U-76 → U-77 → U-78 → U-79 → U-80

A-01…A-06 ─► U-62        B-02/B-03 ─► U-63        B-04…B-09 ─► U-64
S-03…S-12 ─► U-65        I-01…I-05 ─► U-66
```

Integrations (U-62…U-66) are inserted into lane U as soon as the matching asset is marked done — between milestones, never mid-milestone.

## 8. Expected schedule (hours from kickoff)

| Hour | U | A | B | S | I |
|---|---|---|---|---|---|
| 0–1 | M0, start M1 | A-01, A-02 | B-01, B-02 | S-01, S-02 | I-01, I-02 |
| 1–2 | M1 done | A-03 | B-03, B-04 | S-03…S-07 | I-03, I-04 |
| 2–4 | M2, M3 | A-04, A-05 | B-05…B-08 | S-08…S-11 | I-05, I-06 |
| 4–5 | M4 | A-05, A-06 | B-09…B-11 | S-12 | done |
| 5–6 | M5 | A-07 | done | done | — |
| 6–7 | M6 (integrations, levels, polish) | — | — | — | — |
| 7–8 | M7 (autoplay, APK, README, tag) | — | — | — | — |

## 9. Checkpoint loop (orchestrator, every ~60 min)

1. Read `Docs/Status/*.md` and recent git log.
2. Update statuses in this file; commit is done by U with its next milestone (orchestrator does not run git while U is mid-task).
3. Unblock integration tasks for finished assets; queue them for U after its current milestone.
4. Compare with §8. If U is > 45 min behind at hour 5 → apply §10 cuts.
5. If an asset lane fails or stalls > 60 min on one task → mark `[!]`, keep the placeholder, log in `decisions.md`.
6. If a U task fails CLI compile/tests 3 times → mark `[!]`, log the error summary, move to the next task that does not depend on it.

## 10. Cut order (when behind)

1. U-76 full-flow test (per-level tests remain).
2. U-36 phoenix VFX polish (keep a simple boost).
3. U-34 axes villain.
4. U-61 sky presets beyond a color tint.
5. U-52 level-select locks (keep sequential flow via "Next").
6. B-09/B-10 phoenix and trophy meshes (use VFX / icon).

## 11. Lane H — human, morning (do NOT execute tonight)

- [ ] **H-01 · H** — Review `Docs/Status/summary.md`, `decisions.md`, `TestResults/Screenshots/`.
- [ ] **H-02 · H** — Open the editor, play all 5 levels, `curl` bumps.
- [ ] **H-03 · H** — Install APK on the phone; `adb forward tcp:56789 tcp:56789`; `curl -X POST http://localhost:56789/bump`.
- [ ] **H-04 · H** — Device check: frame rate, touch feel, UI safe area, audio.
- [ ] **H-05 · H** — Fix loop with agents (rebuild APK, retest).
- [ ] **H-06 · H** — Final video, **once**, after all tests pass: `Tools/Video/record_playthrough.py` (adb input + curl "commentators" + scrcpy):
  main menu → levels 1–5 completed, bumps visible.
- [ ] **H-07 · H** — Upload APK + video to cloud storage; one share link together with the GitHub repo link.
- [ ] **H-08 · H** — Submit before the 24 h deadline.
