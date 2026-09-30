# God Tower — Implementation Plan

Autonomous overnight plan. Agents work **without a human for ~8 hours** — every decision below is final.
If something is ambiguous, pick the simplest option consistent with this document, implement it,
and log it in `Docs/Status/decisions.md` (it will go into the README "Assumptions" section).

Sources of truth, in order: this plan → `Reference/brief.md` (local, gitignored) → `Reference/ref.mp4`.

---

## 1. Game design (locked)

Reproduce the reference video's gameplay; add no mechanics beyond the brief + reference.

| Topic | Decision |
|---|---|
| Orientation | Portrait 9:16 (reference video). Reference resolution 1080×1920. |
| Camera | Front view of the tower, character centered horizontally, follows height smoothly. Cinemachine 3 + Impulse for shakes. Slight zoom-out during hero boosts. |
| Controls | **Hold** anywhere = climb up. **Release** = hang in place (idle). **Swipe left/right** = move one lane. Swipe works while holding. |
| Lanes | 3 lanes on the front arc of the column: −35°, 0°, +35° around the cylinder axis. Lane change ≈ 0.2 s hop. |
| Villain events | Scripted on the level timeline, targeted at lanes, with a telegraph (warning marker + red banner on the right). Hit = `Hit` → `Fall` → knocked down by 8% of tower height. Dodge = be in another lane when it lands. |
| — Missile | Flies in horizontally from the screen side into one lane at the character's height. |
| — Truck | Falls from above into one lane (shadow/marker telegraph). |
| — Axes | Spinning axes sweep **two** lanes at once; only one lane is safe. |
| Hero events | Automatic on the level timeline (blue banner on the left). No input needed. |
| — Jetpack | Carries the character up by 10% of tower height over ~2 s. |
| — Phoenix | Big full-screen fire VFX, carries up by 20% over ~3 s (`Carried` animation). |
| Win | Reach the top platform → `Win` animation, trophy, win screen. |
| Lose | Level timer reaches 0 before the top → `Lose` animation, lose screen. |
| Progress | Sequential unlock saved in `PlayerPrefs`; "Next" button on the win screen. |

### Level table (starting values — tune only if a level is not completable in PlayMode autoplay)

| # | Tower height (m) | Time limit (s) | Villains | Villain interval (s) | Telegraph (s) | Hero events | Sky preset |
|---|---|---|---|---|---|---|---|
| 1 | 300 | 75 | Missile | 8 | 1.0 | Jetpack ×1 | Day |
| 2 | 400 | 85 | Missile, Truck | 7 | 0.9 | Jetpack ×1 | Bright afternoon |
| 3 | 500 | 95 | Missile, Truck, Axes | 6 | 0.8 | Jetpack ×1, Phoenix ×1 | Golden hour |
| 4 | 600 | 105 | All | 5 | 0.7 | Phoenix ×1 | Sunset |
| 5 | 750 | 120 | All, incl. back-to-back pairs | 4 | 0.6 | Jetpack ×1, Phoenix ×1 | Dusk |

Climb speed: 6 m/s (displayed meters). Timelines use a fixed seed per level (deterministic).

## 2. Webhook `/bump` (main feature, 20% of the grade)

- `TcpListener` on `IPAddress.Loopback:56789`, background thread, minimal HTTP/1.1 request-line parser.
- Accepts `GET` and `POST` on `/bump`. Responses (JSON, `Connection: close`):
  - `200 {"status":"triggered"}` — while a level is being played (not paused, not on win/lose screen).
  - `409 {"status":"ignored","reason":"not_playing"}` — otherwise.
  - `404` other paths, `405` other methods, `400` malformed.
- Request handling marshals to the main thread via a `ConcurrentQueue` drained in a VContainer `ITickable`.
  The HTTP response for a play-state decision is sent after the main thread answers (use `TaskCompletionSource`), timeout 1 s.
- Server starts at app launch (root scope) and lives for the whole session; disposed on quit.
- Effect: a wave of 6–8 red 3D boxing gloves flies in from random screen edges along arcs (PrimeTween), hits the character,
  CFX impact bursts at contact points, full-screen white flash, Cinemachine impulse, punch SFX.
  Gameplay: `Hit` animation, input locked ~0.5 s, knocked down by 3% of tower height (bump knockdown capped at 6% per 5 s).
- Repeated requests layer new waves on top (max 3 concurrent waves; extra requests queue). Never soft-locks.
- Android: `INTERNET` permission (Player Settings → Internet Access: Require). PC → device: `adb forward tcp:56789 tcp:56789`
  (**not** `adb reverse`, which forwards device → PC). Document both in README.

## 3. Tech stack (locked)

| Area | Choice |
|---|---|
| Engine | Unity 6000.5.3f1, URP (use `Mobile_RPAsset` for Android), Input System |
| DI | VContainer (git URL, MIT) — `RootLifetimeScope` via VContainerSettings + `GameLifetimeScope`, `MenuLifetimeScope` |
| Async | UniTask (git URL, MIT) — no coroutines; use `destroyCancellationToken` / linked tokens |
| Tweens | PrimeTween (OpenUPM, MIT) |
| Camera | Cinemachine 3 (`com.unity.cinemachine`) |
| UI | uGUI + TextMeshPro, OFL/Apache font from Google Fonts in the repo, SafeArea |
| Config | ScriptableObjects: `LevelConfig`, `LevelCatalog`, `VillainEventConfig`, `HeroEventConfig`, `BumpEffectConfig`, `SkyPreset` |
| Tests | Unity Test Framework: EditMode (parser, responses, lane logic, knockdown math) + PlayMode (webhook over real HTTP, per-level autoplay) |
| Build | IL2CPP, ARM64 only, min API 26, portrait locked, 60 fps target, ASTC textures |

Code rules: English comments/docs, no singletons, no dead code (remove `Assets/TutorialInfo`, `Assets/Readme.asset`, `SampleScene`),
asmdefs `GodTower.Runtime`, `GodTower.Editor`, `GodTower.Tests.EditMode`, `GodTower.Tests.PlayMode`. Namespaces `GodTower.*`.

### Project layout

```
Assets/
  _Project/
    Scripts/{Core,Webhook,Gameplay,Events,Effects,UI,Audio,Levels}/
    Scripts/Editor/        # CLI entry points: setup, scene builders, build
    Configs/Levels/        # Level_01..05.asset, LevelCatalog.asset
    Prefabs/  Scenes/{Menu,Game}.unity  Materials/  Audio/Generated/  UI/
  Art/
    Characters/Hero/       # Astra output (see Docs/CharacterPipeline.md)
    Environment/Tower/     # Blender output
    Props/                 # gloves, missile, truck, axe, jetpack, phoenix
  Tests/{EditMode,PlayMode}/
```

## 4. Agents and ownership

Only **one agent runs Unity** (batchmode locks the project). The editor stays closed all night.

| Agent | Owns | Tools | Output |
|---|---|---|---|
| **Unity agent** | All C#, scenes, prefabs, settings, tests, APK, git commits | Unity CLI (`-batchmode -executeMethod`, `-runTests`), MCP only if CLI cannot do it | Everything in `Assets/_Project`, `Assets/Tests`, `Packages`, `ProjectSettings` |
| **Astra** | Player character | per `Docs/CharacterPipeline.md` | `Assets/Art/Characters/Hero/` |
| **Blender agent** | Tower segments (3–4 modular drums/friezes/rings, tileable), top dish platform, glove, missile, truck, axe, jetpack, low-poly phoenix | Blender MCP | FBX + textures in `Assets/Art/Environment/Tower/`, `Assets/Art/Props/` |
| **Audio agent** | SFX (punch, explosion, truck crash, axe whoosh, jetpack, phoenix screech, telegraph beep, win, lose, UI) + one looping upbeat music track | Python via `uv run --with numpy --with scipy`, procedural synthesis | WAV 44.1 kHz in `Assets/_Project/Audio/Generated/` + generator scripts in `Tools/Audio/` |
| **Image agent (GPT Codex)** | Sky gradients/cloud textures, UI panels/buttons, event banner backgrounds, item icons, trophy icon | Image generation | PNG in `Assets/Art/UI/` and `Assets/Art/Environment/Sky/` |

Asset budgets: props ≤ 1.5k tris each, tower segment ≤ 3k tris, textures ≤ 1024², one material per asset where possible.
Style: bright, saturated, Roblox-like chunky shapes; stone column in pale grey-green like the reference.
No third-party IP (no Dragon Ball, Gundam, Roblox logos).

**Coordination:** asset agents only write files in their folders and update `Docs/Status/<agent>.md`
(what is done, file list, known issues). They do **not** run git. The Unity agent commits everything at each milestone
and swaps placeholders for real assets when the status file says an asset is done.
Until then it uses primitive placeholders (capsule hero, cylinder tower, cube props).

## 5. Unity agent — milestones (in order)

Each milestone ends with: compile via CLI, run tests, commit + push to `main` (English conventional commits).

| # | Milestone | Budget | Done when |
|---|---|---|---|
| M0 | Setup: packages, remove template leftovers, folders, asmdefs, Player Settings (portrait, IL2CPP ARM64, `com.andreykopylkov.godtower`, Internet Access: Require), CLI entry points (`BuildTools.SetupProject`, `BuildTools.BuildAndroid`) | 0.5 h | Project compiles in batchmode |
| M1 | Webhook server + main-thread dispatch + play-state gate + EditMode/PlayMode tests | 1.5 h | `curl` against a PlayMode run returns 200/409 correctly; tests green |
| M2 | Core loop: input (touch + mouse in editor), climber state machine (Idle/Climb/Shift/Hit/Fall/Carried/Win/Lose), lanes, level runner (timer, win/lose), camera | 2 h | Level 1 playable with placeholders |
| M3 | Event timeline: 3 villains with telegraphs and hits, 2 hero boosts, side banners | 1 h | All events fire per `LevelConfig` |
| M4 | Bump effect: glove waves, pooling, flash, impulse, SFX, knockdown cap, layering | 1 h | Spamming `/bump` 20× causes no errors and play continues |
| M5 | UI + flow: Menu scene (main menu, level select with locks), HUD (height bar + number, timer), pause, win/lose, progress | 1.5 h | Menu → Level 1 → Win → Next → Level 2 works |
| M6 | 5 levels content + sky presets + asset swap-in + lighting/bloom polish | 1 h | All 5 `LevelConfig`s authored |
| M7 | Autoplay tests for all 5 levels + APK build + README final | 1 h | See §6; `Builds/GodTower.apk` built (not committed) |

If time runs short, cut in this order: phoenix VFX polish → axes event → sky presets beyond tint → level-select locks.
Never cut: webhook, 5 completable levels, bump effect, APK build, README.

## 6. Testing (no device tonight)

- **EditMode:** HTTP parser/responses, lane switching, knockdown and cap math, timeline ordering.
- **PlayMode webhook:** real `HttpClient` requests to `localhost:56789` → 200 while playing, 409 in menu/pause, 404/405.
- **PlayMode level autoplay** (one test per level, run via CLI after each level is done):
  - Simulated player through `InputTestFixture` (add `"testables": ["com.unity.inputsystem"]` to the manifest):
    hold to climb, swipe away from telegraphed lanes (the bot may read the telegraph from the event system — it is a test).
  - Simulated "commentators": a background client sends `/bump` at seeded random intervals (every 4–10 s).
  - Pass: level ends in **Win** before the timer, zero errors/exceptions in the log, every `/bump` during play got 200.
  - Capture screenshots (1080×1920 game view) at start, first villain hit, first bump, hero boost, win →
    `TestResults/Screenshots/Level_0N_*.png` for the morning review (gitignored).
- Batchmode rendering: run PlayMode tests **without** `-nographics` so screenshots render.
- No APK install tonight. On-device tests and the final video are done in the morning by the human.

## 7. Morning (human) — not for tonight's agents

1. Review screenshots, `Docs/Status/*`, play in editor.
2. Install APK on the phone, `adb forward tcp:56789 tcp:56789`, `curl -X POST http://localhost:56789/bump`.
3. Final video, recorded **once** after all tests pass: a PC script drives the real APK with `adb shell input`
   (menu taps, hold, swipes) and sends `/bump` requests as "commentators"; recorded with `scrcpy --record`
   (`adb screenrecord` is limited to 3 minutes). Main menu → levels 1–5 completed.

## 8. CLI reference

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe"
PROJECT="D:/Unity/Projects/GodTower"

# Compile / run an editor method
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod GodTower.Editor.BuildTools.SetupProject -logFile Logs/cli.log

# Tests
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log

# APK
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -buildTarget Android -executeMethod GodTower.Editor.BuildTools.BuildAndroid -logFile Logs/build.log
```

Android SDK/NDK/JDK: bundled with the editor (`.../PlaybackEngines/AndroidPlayer/{SDK,NDK,OpenJDK}`).
Python: use `uv run` (no system Python installed).
