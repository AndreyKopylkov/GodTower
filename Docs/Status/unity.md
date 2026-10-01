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

## How to run (Unity Editor must be closed)

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe"
P="D:/Unity/Projects/GodTower"
# Settings, art import settings, configs, prefabs, scenes, build list (rerun after changing any builder)
"$UNITY" -batchmode -quit -projectPath "$P" -executeMethod GodTower.Editor.BuildTools.SetupProject -logFile Logs/setup.log
# Tests: ALWAYS pass -assemblyNames, otherwise Input System's own tests (testables) run too
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform EditMode -assemblyNames GodTower.Tests.EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform PlayMode -assemblyNames GodTower.Tests.PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log
# APK (switches platform to Android, long reimport the first time)
"$UNITY" -batchmode -quit -projectPath "$P" -buildTarget Android -executeMethod GodTower.Editor.BuildTools.BuildAndroid -logFile Logs/build.log
```

## Files delivered

- Runtime (`Assets/_Project/Scripts`, asmdef `GodTower.Runtime`)
  - `Core/`: `PlayState`, `IPlayState`, `PlayStateService`, `MainThreadDispatcher`, `AppBootstrap`
  - `Webhook/`: `BumpHttpServer`, `BumpServerOptions`, `HttpRequestReader`, `BumpRequestParser`, `HttpRequestHead`, `BumpEndpoint`, `BumpResponse`, `BumpGate`, `IBumpSignal`
  - `Gameplay/`: `ClimbInput` (`IClimbInput`, `PointerClimbInput`, `ClimbInputFrame`), `SwipeDetector`, `LaneModel`, `KnockdownModel` (+`KnockdownSettings`), `ClimberSettings`, `ClimberMotor` (+`ClimberState`), `ClimberAnimation`, `ClimberView`, `CameraRig`, `GameplayConfig`
  - `Levels/`: `LevelConfig`, `LevelCatalog`, `SelectedLevel`, `TowerSet`, `TowerLayout`, `TowerBuilder`, `LevelRunner` (+`LevelOutcome`)
  - `Events/`: `EventKinds` (`VillainKind`, `HeroKind`, `HeroEventEntry`), `EventTimeline` (+`VillainStrike`, `HeroBoost`, `TimelineParameters`), `VillainEventConfig`, `HeroEventConfig`, `EventsConfig`, `EventDirector`, `EventPresenter`, `EventStage`
  - `Scopes/`: `RootLifetimeScope`, `MenuLifetimeScope`, `GameLifetimeScope`
  - `UI/`: `MenuEntryPoint`, `EventBannerPanel` (+`BannerSide`), `EventBannerView`
- Editor (`Scripts/Editor`): `BuildTools` (CLI), `PlayerSettingsSetup`, `VContainerRootSetup`, `InputSetup`, `TowerImportSetup`, `HeroImportSetup`, `PropImportSetup`, `UiImportSetup`, `MaterialFactory`, `GameAssetsBuilder` (level table, event configs, tower set), `SceneBuilder` (+`GameSceneAssets`), `GameHudBuilder`, `ProjectPaths`, `AssetFolders`
- Generated assets: `Configs/{GameplayConfig,TowerSet}.asset`, `Configs/Levels/{Level_01..05,LevelCatalog}.asset`, `Configs/Events/{Villain_*,Hero_*,EventsConfig}.asset`, `Materials/{Environment,Characters,Props,Events}/*`, `Animation/{Hero,Phoenix}.controller`, `Prefabs/{Hero,Props/Phoenix,RootLifetimeScope}.prefab`, `Scenes/{Menu,Game}.unity`
- Third-party in repo: `Assets/TextMesh Pro` (TMP essential resources).
- Tests: EditMode `{SwipeDetector,LaneModel,KnockdownModel,ClimberMotor,TowerLayout,EventTimeline}Tests` + M1 tests; PlayMode `LevelPlayModeTests`, `LevelEventsPlayModeTests` (both `InputTestFixture`), `BumpWebhookPlayModeTests`, `SceneScopeSmokeTests`.

## Notes for the next U run (run 3: M4 bump effect + M5 UI)

- **Scene wiring.** Everything in `Game.unity` is created by `SceneBuilder` / `GameHudBuilder` and injected into `GameLifetimeScope` serialized fields. Add new scene objects there (HUD height bar/timer/pause in `GameHudBuilder`, a flash overlay image, ...). Assets referenced by scenes must be loaded by path *after* `EditorSceneManager.NewScene` (it unloads unreferenced assets, leaving stale references; see `GameSceneAssets`).
- **Game scope order.** `LevelRunner` (ticks first) → `EventDirector` → `EventPresenter`. `LevelRunner` exposes `Climber` (`ClimberMotor`), `Knockdown` (`KnockdownModel`), `Elapsed`, `TimeLeft`, `IsRunning`, `IsPaused`, `Outcome`, `Ended`, `Pause()/Resume()`. Gate gameplay on `runner.IsRunning`; use the level clock (`Elapsed`) for anything that must freeze on pause.
- **M4 bump.** Subscribe to `IBumpSignal.BumpRequested` in a Game-scope service (unsubscribe in `Dispose`). Knockdown: `runner.Climber.Knockdown(runner.Knockdown.TakeBumpKnockdown(runner.Elapsed))` (rolling cap implemented + tested; `Knockdown(0)` still plays the Hit reaction — decide whether a capped bump still plays Hit). Input lock = Hit state (0.4 s) + fall; `ClimberMotor.AcceptsInput` is false meanwhile. Glove prop: `PropImportSetup.BoxingGlove` (right glove, punches along +Z; mirror scale.x for a left one). Shake: `CameraRig.Shake(force)`. CFXR prefab paths: see `GameAssetsBuilder.BuildEventsConfig`.
- **M5 UI.** HUD canvas `HUD` (overlay, 1080×1920, match 0.5) exists with `EventBanners`; the hero banner column is flush with the left edge — move it right if the height bar goes on the left. No `EventSystem` in the Game scene yet (add one for the pause button, and make `PointerClimbInput` ignore presses that start over UI). `SelectedLevel.Select(index)` (root scope) picks the level before loading `Game`. The Menu scene is still empty (scope + camera).
- **Fonts.** Banner labels use TMP LiberationSans SDF (`UiImportSetup.Font`); U-50 should replace it (Google font + TMP asset).
- **Screenshots in batch mode.** `WaitForEndOfFrame` is never invoked under `-batchmode` and `ScreenCapture` misses overlay UI: switch canvases to Screen Space - Camera, render `Camera.main` into a RenderTexture, `ReadPixels` (worked for the M3 visual check).
- **Autoplay.** `LevelEventsPlayModeTests` has a reusable bot (`SafeLane` + `SwipeAsync`, which re-presses at the centre so re-centring is not read as a swipe back). M7 per-level autoplay can extend it (pick the level via `SelectedLevel`, add bump spam once M4 knocks down). Level 5 (750 m / 120 s) is tight: ~87 s of pure climbing after boosts, plus bump knockdowns — may need tuning.
- **Audio** (U-65) not wired yet; clips are in `Audio/Generated/`.

## Known issues

- APK build not executed yet (U-77).
- `Hero_Animations.fbx` import logs "has animation import warnings" (importer info; avatar and clips are valid and play correctly in captures).
- Side lanes (±35°) put the climber near the column's silhouette edge as seen from the camera (Plan-locked angles).
- Phoenix boost: the phoenix sits below/in front of the climber; worth a visual polish pass in M6 (full-screen fire look of the reference).
