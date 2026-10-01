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

## How to run (Unity Editor must be closed)

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe"
P="D:/Unity/Projects/GodTower"
# Settings + VContainer root + regenerate scenes/build list
"$UNITY" -batchmode -quit -projectPath "$P" -executeMethod GodTower.Editor.BuildTools.SetupProject -logFile Logs/setup.log
# Tests — ALWAYS pass -assemblyNames, otherwise Input System's own tests (testables) run too
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform EditMode -assemblyNames GodTower.Tests.EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log
"$UNITY" -batchmode -projectPath "$P" -runTests -testPlatform PlayMode -assemblyNames GodTower.Tests.PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log
# APK (switches platform to Android → long reimport the first time)
"$UNITY" -batchmode -quit -projectPath "$P" -buildTarget Android -executeMethod GodTower.Editor.BuildTools.BuildAndroid -logFile Logs/build.log
```

## Files delivered

- Runtime (`Assets/_Project/Scripts`, asmdef `GodTower.Runtime`)
  - `Core/`: `PlayState`, `IPlayState`, `PlayStateService`, `MainThreadDispatcher`, `AppBootstrap`
  - `Webhook/`: `BumpHttpServer`, `BumpServerOptions`, `HttpRequestReader`, `BumpRequestParser`, `HttpRequestHead`, `BumpEndpoint` (+ `BumpRoute`), `BumpResponse`, `BumpGate`, `IBumpSignal`
  - `Scopes/`: `RootLifetimeScope`, `MenuLifetimeScope`, `GameLifetimeScope`
  - `UI/`: `MenuEntryPoint` (sets `PlayState.Menu` when the menu scene starts)
- Editor (`Scripts/Editor`, asmdef `GodTower.Editor`): `BuildTools` (CLI entry points), `PlayerSettingsSetup`, `VContainerRootSetup`, `SceneBuilder`, `ProjectPaths`, `AssetFolders`
- Assets: `Configs/VContainerSettings.asset` (preloaded), `Prefabs/RootLifetimeScope.prefab`, `Scenes/Menu.unity`, `Scenes/Game.unity` (generated)
- Tests: `Assets/Tests/EditMode/*Tests.cs`, `Assets/Tests/PlayMode/{BumpWebhookPlayModeTests,SceneScopeSmokeTests,TestScenes}.cs`

## Notes for the next U run (M2+)

- **Scopes.** `RootLifetimeScope` (prefab referenced by `VContainerSettings`, auto-created before the first scene, `DontDestroyOnLoad`) registers: `AppBootstrap`, `PlayStateService` (as self + `IPlayState`), `MainThreadDispatcher` (entry point, ticks every frame), `BumpGate` (self + `IBumpSignal`), `BumpServerOptions`, `BumpHttpServer` (entry point). Scene scopes have no parent reference set → VContainer parents them to the root automatically.
- **Play-state gate.** `BumpGate.TryTrigger()` (main thread) returns true only when `PlayStateService.Current == Playing`. The real writer is missing: in M2 the `LevelRunner` (Game scope) must call `PlayStateService.Set(Playing)` when the level starts, `Paused`/`Playing` on pause/resume, `Result` on win/lose. `MenuEntryPoint` sets `Menu`. Also set `Menu` (or `Result`) before leaving the Game scene so a scene transition never answers 200.
- **Bump effect hook.** Subscribe to `IBumpSignal.BumpRequested` (main thread) from the Game scope (M4 `BumpEffect`); unsubscribe on dispose — the signal lives in the root scope and outlives scenes. Queuing/layering (max 3 waves) belongs to the effect, not the gate.
- **Scenes are generated.** `SceneBuilder.BuildAll()` recreates `Menu.unity` and `Game.unity` from scratch each `SetupProject` run — add new scene content (camera rig, tower, UI canvases) in the builder, not by hand.
- **Adding tests.** EditMode: plain NUnit in `Assets/Tests/EditMode` (asmdef already references Runtime, UniTask, VContainer). PlayMode: `Assets/Tests/PlayMode`, async tests as `[UnityTest] IEnumerator X() => UniTask.ToCoroutine(async () => { ... })`; the asmdef already references `Unity.InputSystem.TestFramework` for `InputTestFixture`. Root container in tests: `VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container`.
- `Assets/InputSystem_Actions.inputactions` (template) is still the project-wide actions asset — replace or delete it in U-20.

## Known issues

- APK build not executed yet (U-77). First Android build triggers a full platform switch/reimport.
- Unity generated `.meta` files for other lanes' assets under `Assets/Art/**` while importing; they are left unstaged for the lanes/orchestrator (not committed by M0/M1).
