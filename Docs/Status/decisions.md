# Decisions log

Autonomous decisions made by agents (feeds README "Assumptions").

- O-01: Environment OK (Unity 6000.5.3f1 + Android SDK/NDK/JDK, uv 0.12.8, Asset Store packs and Reference present, git clean).
- O-01: Blender MCP tools are not exposed to the orchestrator/subagents — asset lanes drive Blender 5.2 headless (`D:/Games/Steam/steamapps/common/Blender/blender.exe -b --python <script>`); generator scripts kept in `Tools/Blender/` so assets are reproducible.
- O-01: No image-generation tool (GPT Codex) available to the agents — lane I renders UI/sky images procedurally in Python (Pillow via `uv run`); generator scripts in `Tools/Images/`.
- O-01: No image-to-3D tool available — lane A builds the Roblox-style blocky hero procedurally in Blender Python (mesh, rig, keyframed clips); concept images are Blender renders of the model instead of AI turnarounds.
- O-01: ffmpeg is not installed — reference frames/audio are extracted with `uv run --with imageio-ffmpeg`.
- S-11: UI sounds generated procedurally instead of picking CC0 samples — consistent timbre with the rest, no licensing notes needed.
- S-02: skipped (optional) — no time spent on reference audio analysis.
- I-01..I-06: all 2D art rendered procedurally with Pillow (no AI image tool); logo font Arial Black; hero/villain banner badges left blank for icons — O-01 follow-up, simplest option
- B-02: Tower segments share one 1024² atlas material `M_TowerStone` (2×2 tiles, one per segment; texture repeats 4× around via UV islands) — one material for all segments = static/SRP batching, smaller APK.
- B-02: Carvings are normal-map + baked cavity in albedo on a lathed 40-side column; rings/ledges/flutes are real geometry — keeps segments ≤ 2.4k tris.
- B-02: Ledge plates stick out to r = 1.80 m (column 1.5 m); the climber should hang with its body centre at r ≥ ~1.95 m to avoid clipping the ledges.
- B-03: Top platform `Tower_Top` is 5.3k tris (one-off hero piece, segment budget does not apply); walkable deck at y = 3.40 m above its pivot, deck radius 6.0 m, rim lip up to y = 3.66 m.
- A-02: No AI turnaround — the "concept turnaround" is Eevee renders of the procedural model (`Art/Source/Character/Concept/hero_tpose_*.png`, transparent) — no image tool available (O-01).
- A-03: Hero modelled procedurally in Blender Python (`Tools/Blender/Hero/build_hero.py`): bevelled boxes + curved hair spikes, 1 material `M_Hero`, flat-colour atlas `Hero_Albedo.png` (swatches + painted face / gi front / original tower emblem on the back) — reproducible, no image-to-3D tool.
- A-03: Height 1.80 m measured to the top of the hair spikes (top of the head ≈ 1.54 m, the rest is hair) — chunky Roblox-like proportions with a big head.
- A-04: Bones named exactly like Unity `HumanBodyBones` (Hips, Spine, Chest, Neck, Head, Left/RightShoulder, …UpperArm, …LowerArm, …Hand, …UpperLeg, …LowerLeg, …Foot, …Toes) so Humanoid auto-mapping is unambiguous; rigid skinning (every block weighted 100% to one bone).
- A-06: FBX exported with Blender's standard Unity settings (forward -Z, up Y, FBX_SCALE_ALL, no leaf bones) but WITHOUT the experimental "Apply Transform" (bake_space_transform) because Blender documents it as broken with armatures/animations; object transforms are applied in Blender instead. Unity may show a -90° X on the rig root node — harmless for Humanoid; enable "Bake Axis Conversion" in the model importer if wanted.
- A-05: Clips are exported as separate FBX takes from NLA strips, so Unity clip names are exactly `Hero_<Clip>` (no `Armature|` prefix). `Hero_Win` turns the hero 180° (Hips rotation) to face the camera before the fist-pump; keep Root Transform Rotation "Bake Into Pose" on.
- U-01: Cinemachine 3.1.4 does not compile on Unity 6000.5 (`GetInstanceID` obsolete-as-error) — pinned 3.1.7 (latest). VContainer 1.19.0 and UniTask 2.5.11 pinned by git tag, PrimeTween 1.3.3 from OpenUPM — reproducible resolves.
- U-01: `testables: com.unity.inputsystem` pulls Input System's own test assemblies into the runner — CLI test runs always pass `-assemblyNames GodTower.Tests.EditMode|GodTower.Tests.PlayMode`.
- U-02: Kept `Assets/InputSystem_Actions.inputactions` (registered as project-wide actions; M2 decides if it is replaced) and `Assets/Settings/SampleSceneProfile.asset` (default volume profile referenced by both URP assets).
- U-03: LifetimeScopes live in `Scripts/Scopes/` (namespace `GodTower.Scopes`) — the root scope wires Core + Webhook, so it sits above both.
- U-06: Scenes are generated from code (`SceneBuilder`, run by `BuildTools.SetupProject`) — CLI-only workflow, editor stays closed; extend the builder instead of hand-editing scenes.
- U-04: Active build target stays Standalone (fast tests); `BuildAndroid` is meant to be run with `-buildTarget Android` (one platform switch/reimport per build).
- U-10: Lenient parser: lowercase methods accepted, absolute-form targets (`http://host/bump`) accepted, query/fragment ignored, LF-only line endings accepted; header lines without a colon or a bad Content-Length → 400; `/bump/` (trailing slash) → 404.
- U-11: Error bodies are JSON `{"status":"error","reason":"<bad_request|not_found|method_not_allowed|internal_error|main_thread_timeout>"}`; 405 adds `Allow: GET, POST`.
- U-12: Server also listens on IPv6 loopback `[::1]:56789` (best effort, warning if unavailable) — Windows clients resolve `localhost` to ::1 first and a refused connection costs ~2.2 s (measured) before IPv4 fallback. Still loopback-only (not exposed to the LAN).
- U-12: Brief §5.1 suggests `adb reverse`; for a PC-side curl reaching the phone the correct command is `adb forward tcp:56789 tcp:56789` (Plan §2) — README must document forward and explain why not reverse.
- U-13: Main thread not answering within 1 s (app suspended) → 503 `main_thread_timeout`; the abandoned work item is guaranteed never to run later (no late bump).
