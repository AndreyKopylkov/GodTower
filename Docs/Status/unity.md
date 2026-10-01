# Status — lane unity

Updated by the lane agent. Format: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked.

## Tasks

### M0 — Project setup
- [x] U-01 Packages: VContainer 1.19.0 (git), UniTask 2.5.11 (git), PrimeTween 1.3.3 (OpenUPM), Cinemachine 3.1.7, `testables: com.unity.inputsystem`.
- [x] U-02 Template leftovers removed (`TutorialInfo`, `Readme.asset`, `Scenes/SampleScene`).
- [x] U-03 Folders + asmdefs `GodTower.Runtime`, `GodTower.Editor`, `GodTower.Tests.EditMode`, `GodTower.Tests.PlayMode`.
- [x] U-04 `GodTower.Editor.BuildTools.SetupProject`: portrait only, IL2CPP, ARM64, min API 26, `com.andreykopylkov.godtower`, "God Tower", Internet permission forced, Android quality = Mobile (`Mobile_RPAsset`), ASTC, frame pacing; 60 fps set at runtime by `AppBootstrap`.
- [x] U-05 `GodTower.Editor.BuildTools.BuildAndroid` → `Builds/GodTower.apk` (compiles; not run yet).
- [x] U-06 `RootLifetimeScope` prefab via `VContainerSettings` (preloaded asset); `Menu.unity` / `Game.unity` with `MenuLifetimeScope` / `GameLifetimeScope`; build list = Menu, Game. PlayMode smoke test green.

### M1 — Webhook `/bump`
- [x] U-10 `BumpRequestParser` (+ `HttpRequestHead`, `HttpRequestReader` for stream I/O).
- [x] U-11 `BumpResponse` (200/409/400/404/405/500/503, JSON, `Connection: close`, `Allow` on 405) + `BumpEndpoint` routing.
- [x] U-12 `BumpHttpServer`: `TcpListener` on 127.0.0.1 and [::1] port 56789, accept thread(s) + thread-pool handlers, 5 s connection timeout, port-in-use → error log, game continues; root-scope entry point, disposed with the root container.
- [x] U-13 `MainThreadDispatcher` (`ITickable`, `ConcurrentQueue`, `TaskCompletionSource`, 1 s timeout, abandoned work never runs).
- [x] U-14 `PlayState` / `IPlayState` / `PlayStateService` + `BumpGate` (200 vs 409) + `IBumpSignal.BumpRequested`.
- [x] U-15 EditMode tests (parser, endpoint, responses, reader, dispatcher, gate, play state): **77/77 passed**.
- [x] U-16 PlayMode tests with real `HttpClient` (200 playing, 409 menu/paused/result, 404, 405, 400 raw, POST with body, 10 parallel) + scene smoke tests: **10/10 passed**.
  Manual check: `curl` against a live PlayMode run → 409 in menu, 200 while playing, 404, 405; ~1–7 ms per request on 127.0.0.1 and [::1].

### M2 — Core loop
- [x] U-20 `PointerClimbInput` (code-defined Input System actions on `<Pointer>`: touch + mouse) + pure `SwipeDetector` (≥ 8% width in a sliding 0.35 s window, several swipes per hold). Template `InputSystem_Actions` removed (`InputSetup`).
- [x] U-21 `LaneModel` (−35°/0°/+35°, clamps at edges).
- [x] U-22 `KnockdownModel` (villain 8%, bump 3% capped at 6% per rolling 5 s, floor 0) + `KnockdownSettings`.
- [x] U-23 `ClimberMotor` state machine (Idle/Climb/Shift/Hit/Fall/Carried/Win/Lose, 6 m/s, hop 0.2 s, hit 0.4 s → fall, carry) + `ClimberView` (column arc placement, animator cross-fade by state name via `ClimberAnimation`), capsule placeholder.
- [x] U-24 `TowerSet` + `TowerLayout` + `TowerBuilder`: lane B segments/top are done → used directly (`TowerImportSetup` remaps materials, normal maps, no rig); cylinder fallback if the FBXs are missing.
- [x] U-25 `LevelConfig`, `LevelCatalog`, `SelectedLevel` (root scope); `Level_01..05` generated from the Plan table by `GameAssetsBuilder`.
- [x] U-26 `LevelRunner`: timer, win at top (hop onto deck), lose on timeout, pause/resume (`timeScale` 0), drives `PlayState` (Playing/Paused/Result, Menu on scene unload).
- [x] U-27 `CameraRig`: Cinemachine 3 follow with Y damping, impulse source/listener (`Shake`), FOV zoom-out hook (`SetZoomedOut`).
  Tests: EditMode 106/106, PlayMode 13/13 (incl. `LevelPlayModeTests`: hold climbs, release hangs, swipes, pause → 409, timeout → Lose + 409, `/bump` 200 while playing).

### Integrations
- [x] U-62 Hero: `HeroImportSetup` (Humanoid avatar valid, 16 bones; clips copy the avatar with `preserveHierarchy`; loops on ClimbUp/HangIdle/Fall/Carried; root motion baked), `M_Hero` URP Lit, `Animation/Hero.controller` (9 states named like `ClimberAnimation.States`, default HangIdle), `Prefabs/Hero.prefab`; `SceneBuilder` uses it instead of the capsule when present. Unity logs one benign "animation import warnings" note for `Hero_Animations` (no avatar errors).
- [x] U-63 (done in U-24): tower segments + top platform used through `TowerSet` (no static batching — SRP Batcher with one shared material).
- [x] U-64 (events props): missile, truck, axe, jetpack, phoenix (+ glove and trophy import settings) via `PropImportSetup` (`M_Props` palette material, smoothness from alpha; phoenix Generic rig + `Phoenix.controller` + `Prefabs/Props/Phoenix.prefab`). Glove/trophy imported but not used yet (M4/M5).

### M3 — Level events
- [x] U-30 `EventTimeline` (pure, seeded): `VillainStrike` (impact/telegraph time, lane mask), `HeroBoost`; skips villains around hero boosts; back-to-back pairs. EditMode tests.
- [x] U-31 Telegraph: pulsing red lane strips behind the climber (`EventStage`) + red banner on the right.
- [x] U-32 Missile (from the lane's screen side, CFXR explosion), U-33 Truck (drops from above, crash FX + shake), U-34 Axes (two spinning axes, one safe lane).
- [x] U-35 Jetpack (+10% / 2 s, prop on the back, flame), U-36 Phoenix (+20% / 3 s, phoenix under the climber, Hovl burst, camera zoom-out).
- [x] U-37 `EventBannerPanel`: blue left / red right stacks, slide in/out, push down, "Name ×1" with the event icon in the badge.
  `EventDirector` (logic: telegraph → land → hit/dodge → `ClimberMotor.Knockdown`; boosts → `Carry`), `EventPresenter` (banners + stage), `EventStage` (world visuals on the level clock).
  Tests: EditMode 115/115, PlayMode 15/15 (incl. `LevelEventsPlayModeTests`: bot dodges telegraphs and wins Level 1 at 4× speed with every `/bump` = 200; staying in the targeted lane → Hit + exactly 8% knockdown).

### M4 — Bump effect
- [x] U-40 `BumpEffectConfig` SO (`Configs/BumpEffectConfig.asset`, generated by `GameAssetsBuilder.BuildBumpEffectConfig`; tuning defaults live in the class — delete the asset to regenerate after changing them).
- [x] U-41 `BumpStage`: pooled gloves (prewarm 8×3), 4 screen edges, `GloveArc` (pure, tested) bent arcs + punch-in roll, PrimeTween `Tween.Custom` tracking the falling climber, recoil.
- [x] U-42 Contact: CFXR star bursts per glove (parented to the climber), CFXR POW + `ScreenFlash` (HUD overlay) + `CameraRig.Shake` on the first contact, 3 punch SFX per wave via `IAudioService`.
- [x] U-43 `BumpDirector`: first contact → `ClimberMotor.Knockdown(meters, HitReaction 0.25 s / min fall 0.25 s)` via `KnockdownModel.TakeBumpKnockdown`; capped or boosted → `ClimberView.PlayFlinch` (visual-only Hit).
- [x] U-44 `BumpQueue` (pure, tested): max 3 concurrent waves, 24 queued, watchdog; new waves only while `LevelRunner.IsRunning`.
  Tests: EditMode 129/129 (`BumpQueueTests`, `GloveArcTests`, motor stacking/reaction tests), PlayMode 18/18 (`BumpEffectPlayModeTests`: single bump → Hit, 3% knockdown, ≈0.55 s lock; storm 20 requests in 2 s → all 200, max 3 waves, queue drains, Level 1 still won).

### M5 — UI and flow
- [x] U-50 Font: Lilita One (OFL) + `FontSetup` TMP asset (outline + shadow material); `UiImportSetup.Font` falls back to LiberationSans if missing.
- [x] U-51 `ProgressService` (+`IProgressStore`, `PlayerPrefsProgressStore`), root scope. EditMode tests.
- [x] U-52 Menu scene: `MenuUiBuilder` (logo, Play, Levels, level-select popup with 5 tiles: lock / star), `MenuView`, `MenuPresenter` (menu music, Play = first unfinished level), `MenuBackdrop` (runtime column) + hero in close-up.
- [x] U-53 HUD (`GameHudBuilder`): `HeightBarView` (anchor-relative fill + hero-portrait marker), `TimerView`, `HudView` (level title, pause button), SafeArea (`SafeAreaFitter`), `HudPresenter`. EventSystem (Input System UI module) in both scenes; `PointerClimbInput` ignores presses that start over UI (`UiPointerFilter`).
- [x] U-54 `PausePanelView` (Resume / Restart / Menu); pause → `PlayState.Paused` → `/bump` 409, gameplay sounds paused.
- [x] U-55 `ResultPanelView` (win: trophy, Next / Menu; lose: Retry / Menu), `WinStage` (3D trophy + firework on the deck), `GameFlowPresenter` (pause, result, progress, music).
- [x] U-56 `SceneFlow` (`ISceneFlow`, root) + `ScreenFader`: fade out → load → fade in, `IsLoading` guard.
  Tests: EditMode 134/134 (`ProgressServiceTests`), PlayMode 19/19 (`GameFlowPlayModeTests`: Menu → Level 1 → Win → Next → Level 2 → Pause → Menu; double click = one load; pause press does not climb; 409 while paused and in the menu).

### Integrations (run 3)
- [x] U-65 Audio: `AudioService` (root, `IAudioService`: one-shots on 16 pooled voices, loops, music cross-fade, gameplay pause via `AudioListener.pause`), `AudioLibrary` (`Configs/AudioLibrary.asset`, built by `AudioSetup`: import settings — music Streaming/Vorbis, SFX Decompress On Load mono — and the `SoundId` → clip map; assigned to the root scope prefab). `GameAudioPresenter` (telegraph beep, missile fly-by, explosion on hit, truck crash, axe whoosh, jetpack loop for the boost, phoenix, climb steps every 0.3 s, lane hop, fall); punches in `BumpStage`; UI click/open/locked, win/lose, level/menu music in the presenters; countdown tick in `HudPresenter`. Loop flag lives on the AudioSource (music, jetpack).
- [x] U-66 UI sprites: kit (panel, buttons, round buttons, height bar), banners, icons (play/pause/home/retry/next/lock/star/trophy + event icons), logo — all used by `GameHudBuilder` / `MenuUiBuilder` / event configs. App icon: `PlayerSettingsSetup.ApplyIcon` (default icon for all platforms from `Art/UI/Logo/app_icon_1024.png`). Clouds (`Art/Environment/Sky`) are left for M6 sky presets.
  Tests: EditMode 153/153 (+`AudioLibraryTests`), PlayMode 19/19.

### M6 — Content and polish (run 4)
- [x] U-60 Level table verified against Plan §1 (`GameAssetsBuilder.Levels`); EditMode `ProjectValidationTests` checks 5 levels, increasing difficulty, sky per level. Tuning for M7 below.
- [x] U-61 `SkyPreset` ×5 (`Configs/Sky`, built by `SkySetup`): gradient skybox (`Shaders/SkyGradient.shader`), fog, sun, ambient, cloud tint; `SkyView` + `CloudField` (3 parallax layers of lane I's clouds, wrapping, drifting) + `SkyPresenter` in both scopes (menu = preset 1).
- [x] Camera framing: far follow camera (58 m, FOV 28°/38°), column ≈ 20% of the width, hero visual 1.75× (≈ 11% of the height), effects/props rescaled.
- [x] U-67 `RenderingSetup` (post-processing profile: bloom, colour adjustments, vignette; HDR, SRP Batcher, shadow distance; cameras: post-processing + FXAA), `TextureImportPolicy` (≤1024, Android ASTC), phoenix visual pass (rides under the climber, fire aura, orange flash).
- [x] U-68 `ProjectValidator` (`BuildTools.ValidateProject` + EditMode test): no missing scripts/references in scenes, prefabs, configs; no Asset Store demo content in the build.
  Tests: EditMode 156/156, PlayMode 20/20.

### M7 — Autoplay (run 4)
- [x] U-70 `LevelAutoplayPlayModeTests` (InputTestFixture): `ClimbBot` holds and swipes away from telegraphed lanes; commentators post `/bump` over real HTTP at seeded 4–10 s (level clock); 4× time scale; asserts Win before the timer, every in-play `/bump` = 200, bumps hit the climber, all hero events fired, no error/exception logged. Screenshots `TestResults/Screenshots/Level_0N_{Start,FirstVillain_Dodged,VillainHit,FirstBump,HeroBoost,Win}.png`.
- [x] U-71 Level 1 — won at 64 s / 80 s (9 bumps, 0 villain hits).
- [x] U-72 Level 2 — won at 97 s / 110 s (14 bumps, 1 hit).
- [x] U-73 Level 3 — won at 81–91 s / 115 s (10–11 bumps, 0–1 hit).
- [x] U-74 Level 4 — won at 136 s / 155 s (20 bumps, 1 hit).
- [x] U-75 Level 5 — won at 138–155 s / 180 s (17–19 bumps, 1–2 hits).
  Tuning (decisions.md U-71..U-75): per-level bump knockdown share (~9 m per bump on every tower) + longer time limits; nothing else changed. Fix found by autoplay: banner dismiss tween error when leaving mid-slide.
  Tests: EditMode 157/157, PlayMode 25/25 (incl. 5 autoplay, ~2.5 min).

## How to run (Unity Editor must be closed)

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe"
P="D:/Unity/Projects/GodTower"
# Settings, art import settings, font, hero portrait, audio library, configs, prefabs, scenes, build list
# (rerun after changing any builder; needs a graphics device — no -nographics — for the hero portrait)
"$UNITY" -batchmode -quit -projectPath "$P" -executeMethod GodTower.Editor.BuildTools.SetupProject -logFile Logs/setup.log
# Tests: ALWAYS pass -assemblyNames, otherwise Input System's own tests (testables) run too
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform EditMode -assemblyNames GodTower.Tests.EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform PlayMode -assemblyNames GodTower.Tests.PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log
# Autoplay of all 5 levels with bumps (~2.5 min) -> TestResults/Screenshots/Level_0N_*.png (also part of the full PlayMode run)
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform PlayMode -assemblyNames GodTower.Tests.PlayMode -testFilter GodTower.Tests.PlayMode.LevelAutoplayPlayModeTests -testResults TestResults/autoplay.xml -logFile Logs/autoplay.log
# Screenshot tour for visual review (explicit tests, ~1 min) -> TestResults/Screenshots/*.png
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform PlayMode -assemblyNames GodTower.Tests.PlayMode -testFilter GodTower.Tests.PlayMode.ScreenshotTourTests -testResults TestResults/tour.xml -logFile Logs/tour.log
# Asset hygiene (missing scripts/references, Asset Store demo content); exit code 1 on problems
"$UNITY" -batchmode -quit -projectPath "$P" -executeMethod GodTower.Editor.BuildTools.ValidateProject -logFile Logs/validate.log
# APK (switches platform to Android, long reimport the first time)
"$UNITY" -batchmode -quit -projectPath "$P" -buildTarget Android -executeMethod GodTower.Editor.BuildTools.BuildAndroid -logFile Logs/build.log
```

After `SetupProject`, run `git checkout -- Assets/_Project/Animation/` (the Hero/Phoenix controllers are regenerated with new
fileIDs every run, identical content) and only commit `Scenes/*.unity` when a builder changed (every rebuild rewrites all
scene fileIDs, a ~5k-line diff) — otherwise `git checkout -- Assets/_Project/Scenes/` too.

## Files delivered

- Runtime (`Assets/_Project/Scripts`, asmdef `GodTower.Runtime`)
  - `Core/`: `PlayState`, `IPlayState`, `PlayStateService`, `MainThreadDispatcher`, `AppBootstrap`, `ProgressService` (+`IProgressStore`, `PlayerPrefsProgressStore`), `SceneFlow` (`ISceneFlow`), `ScreenFader`
  - `Webhook/`: `BumpHttpServer`, `BumpServerOptions`, `HttpRequestReader`, `BumpRequestParser`, `HttpRequestHead`, `BumpEndpoint`, `BumpResponse`, `BumpGate`, `IBumpSignal`
  - `Gameplay/`: `ClimbInput` (`IClimbInput`, `PointerClimbInput`, `ClimbInputFrame`), `SwipeDetector`, `LaneModel`, `KnockdownModel` (+`KnockdownSettings.WithBumpFraction`), `ClimberSettings`, `ClimberMotor` (+`ClimberState`, `HitReaction`), `ClimberAnimation`, `ClimberView` (`Scale`, `CenterHeight`), `CameraRig`, `GameplayConfig`
  - `Levels/`: `LevelConfig` (+bump knockdown share, sky preset), `LevelCatalog`, `SelectedLevel`, `TowerSet`, `TowerLayout`, `TowerBuilder`, `LevelRunner` (+`LevelOutcome`)
  - `Events/`: `EventKinds`, `EventTimeline` (+`VillainStrike`, `HeroBoost`, `TimelineParameters`), `VillainEventConfig`, `HeroEventConfig` (+aura, start flash), `EventsConfig`, `EventDirector`, `EventPresenter`, `EventStage`
  - `Effects/`: `BumpEffectConfig`, `BumpQueue`, `GloveArc`, `BumpStage`, `BumpDirector`, `ScreenFlash`, `WinStage`
  - `Environment/`: `SkyPreset`, `SkyView`, `CloudField` (+`CloudLayer`), `SkyPresenter`
  - `Audio/`: `AudioLibrary` (+`SoundId`, `MusicId`, `SoundEntry`), `AudioService` (`IAudioService`), `GameAudioPresenter`
  - `UI/`: `EventBannerPanel` (+`BannerSide`), `EventBannerView`, `HudView`, `HeightBarView`, `TimerView`, `HudPresenter`, `PopupView`, `PausePanelView`, `ResultPanelView`, `GameFlowPresenter`, `MenuView`, `LevelButtonView`, `MenuPresenter`, `MenuBackdrop`, `SafeAreaFitter`, `ButtonPressScale`, `UiPointerFilter`
  - `Scopes/`: `RootLifetimeScope` (audio library field), `MenuLifetimeScope`, `GameLifetimeScope` (both with `_sky`)
- Shader: `Assets/_Project/Shaders/SkyGradient.shader` (`GodTower/Sky Gradient`, screen-space gradient skybox).
- Editor (`Scripts/Editor`): `BuildTools` (CLI: `SetupProject`, `ValidateProject`, `BuildAndroid`), `PlayerSettingsSetup` (+app icon), `VContainerRootSetup`, `InputSetup`, `TowerImportSetup`, `HeroImportSetup`, `HeroIconRenderer`, `PropImportSetup`, `UiImportSetup`, `FontSetup`, `AudioSetup`, `SkySetup` (cloud import, sky/cloud materials, sky preset table), `TextureImportPolicy`, `RenderingSetup` (URP assets, post-processing profile, camera options), `ProjectValidator`, `MaterialFactory`, `GameAssetsBuilder` (level table, event configs, tower set, bump config), `SceneBuilder` (+`GameSceneAssets`, `HeroScale`), `GameHudBuilder`, `MenuUiBuilder`, `UiFactory`, `ProjectPaths`, `AssetFolders`
- Generated assets: `Configs/{GameplayConfig,TowerSet,BumpEffectConfig,AudioLibrary}.asset`, `Configs/Levels/*`, `Configs/Events/*`, `Configs/Sky/Sky_0N_*.asset`, `Materials/*` (+`Environment/M_Sky`, `M_Cloud`), `Settings/PostProcessProfile.asset`, `Animation/{Hero,Phoenix}.controller`, `Prefabs/{Hero,Props/Phoenix,RootLifetimeScope}.prefab`, `UI/Generated/hero_marker.png`, `Scenes/{Menu,Game}.unity`; font `Assets/Art/Fonts/LilitaOne/{LilitaOne-Regular.ttf, OFL.txt, LilitaOne SDF.asset}`
- Third-party in repo: `Assets/TextMesh Pro` (TMP essential resources), Lilita One (OFL).
- Tests: EditMode `{SwipeDetector,LaneModel,KnockdownModel,ClimberMotor,TowerLayout,EventTimeline,BumpQueue,GloveArc,ProgressService,AudioLibrary,ProjectValidation}Tests` + M1 tests (157); PlayMode `LevelAutoplayPlayModeTests` (5 levels), `LevelPlayModeTests`, `LevelEventsPlayModeTests`, `BumpEffectPlayModeTests`, `GameFlowPlayModeTests` (all `InputTestFixture`), `BumpWebhookPlayModeTests`, `SceneScopeSmokeTests` (25) + explicit `ScreenshotTourTests` (`BumpWave`, `SkyPresetsAndPhoenix`); helpers `ClimbBot`, `ScreenshotCapture`, `TestScenes`.

## Notes for the next U run (run 5: full flow, APK, record script, README, tag)

- **U-76 full-flow test.** Combine `GameFlowPlayModeTests` (menu clicks via EventSystem raycast + `ExecuteEvents`, mouse added *after* the first scene load, PlayerPrefs progress saved/restored in TearDown) with the loop of `LevelAutoplayPlayModeTests` (bot + commentators on the level clock, 4× time scale, `UniTask.Delay(..., ignoreTimeScale: true)` around panels). Menu → Level 1 → Win → Next … → Level 5 → Win ("CHAMPION!" panel). Budget ~3 min real time at 4×; give it `[Timeout(600000)]`. Check `LevelButtonView.IsUnlocked` after each win; reset `SelectedLevel.Select(0)` and progress in TearDown.
- **Level numbers** (source of truth `GameAssetsBuilder.Levels`, tuned in run 4): time 80/110/115/155/180 s, bump knockdown 3/2.25/1.8/1.5/1.2% (~9 m per bump). Bot win times 64/97/81–91/136/138–155 s. Any gameplay change → rerun `LevelAutoplayPlayModeTests` and keep a ≥10% margin.
- **U-77 APK.** Not built yet. `RenderingSetup` already prepares `Mobile_RPAsset` (HDR, SRP Batcher, bloom without HQ filtering, shadow distance 110, render scale 0.8) and our textures have Android ASTC overrides; scenes keep fog on (URP fog variants are not stripped). The first Android build switches the platform and reimports everything (long). Record the APK size here. Watch the build log for shader errors from `GodTower/Sky Gradient` on GLES3/Vulkan (plain HLSL + URP `Core.hlsl`).
- **U-78 record script** (`Tools/Video/record_playthrough.py`, write only): coordinates relative to `adb shell wm size`; button anchors in `MenuUiBuilder` / `GameHudBuilder`; hold = `adb shell input swipe x y x y <ms>`; a lane swipe needs ≥ 8% of the width within 0.35 s; `adb forward tcp:56789 tcp:56789`; bumps every 4–10 s.
- **U-79 README.** Include the framing/hero-scale decision, sky presets, M7 tuning (bump knockdown per level + time limits, with the reason), autoplay at 4× time scale, the validator; third-party: Lilita One (OFL), CFXR Remaster + Hovl Magic effects (Asset Store, gitignored — import steps), TMP essentials; procedural/AI assets per lane (`decisions.md` O-01, I-*, A-*, B-*, S-*).
- **Visual review loop.** Run `SetupProject`, the screenshot tour and the autoplay, then LOOK at `TestResults/Screenshots` (a Pillow contact sheet via `uv run --with pillow` saves tokens). The batch-mode screen is 640×480 landscape; captures render the camera at 540×960 with canvases switched to Screen Space - Camera (so in captures the HUD moves with camera shakes — on devices the overlay HUD does not). `CloudField` clamps the aspect of its wrap band to 0.5–0.8 so the density is right in both.
- **UI clicks in tests.** Dispatch with `ExecuteEvents` after an EventSystem raycast (`GameFlowPlayModeTests.ClickAsync`); the Input System UI module's shared default actions break across `InputTestFixture` resets.

## Known issues

- APK build not executed yet (U-77).
- `Hero_Animations.fbx` import logs "has animation import warnings" (importer info; avatar and clips are valid and play correctly in captures).
- Side lanes (±35°) put the climber near the column's silhouette edge as seen from the camera (Plan-locked angles); still readable at the new framing.
- The jetpack trail (CFXR Fire turned downwards) leaves a dark smoke puff below the climber.
- Bump waves: gloves spawn just outside the screen edges, so the first ~0.2 s of a wave shows nothing; the first contact lands ≈0.35 s after the request.
- The lose panel shows the retry glyph as its icon (no "sad" icon in the kit).
- The bot is sometimes hit by a villain right after a bump's input lock (0–2 hits per level) — honest gameplay, covered by the time margins.
