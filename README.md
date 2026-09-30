# God Tower — Unity Technical Test

This repository is my submission for a **Unity technical test** (Mid/Senior Game Developer position).
The goal is to reproduce a reference tower-climbing game in Unity within a 24-hour window and ship an Android APK.

> Status: **work in progress**. This README will be finalized together with the build.

## Main feature — webhook-triggered event

While a level is being played, the game runs a local HTTP listener:

```
POST/GET http://localhost:56789/bump
```

Each request immediately triggers a full-screen "boxing gloves" hit event: several animated gloves strike the
climbing character from different directions, with screen shake, impact flashes and SFX.
Gameplay continues normally afterwards (no soft-lock).

Trigger instructions (editor and Android device) will be documented here once implemented.

## Other features from the brief

- 5 distinct, playable levels with a clear win condition, reachable from a level-select screen.
- Climbing character: hold to climb, swipe left / right to change lane; idle, climb, hit reaction, win / lose states.
- Level-scripted "villain" events (knock the character down unless dodged) and "hero" events (carry the character up),
  mirroring the reference game.
- Lose condition: level timer runs out before reaching the top.
- UI: main menu, level select, HUD (height bar, timer), pause, win / lose screens.
- Camera follow, zoom and screen shake.
- Basic SFX and music.
- Target platform: Android (APK).

## Tech

- Unity 6000.5.3f1, Universal Render Pipeline
- Input System

## Third-party assets

Only free-to-use or AI-generated assets are used. No paid assets, no human artist contributions.

| Asset | Author | Used for | License | Source | In repo |
|---|---|---|---|---|---|
| Cartoon FX Remaster Free | Jean Moreno (JMO) | Hit / explosion VFX | Standard Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) | No — import manually |
| Magic Effects FREE | Hovl Studio | Hero boost / aura VFX | Standard Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933) | No — import manually |
| FREE Casual Game SFX Pack | Dustyroom | UI and gameplay SFX | CC0 | [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/free-casual-game-sfx-pack-54116) | Yes |
| Climbing character + animations *(planned)* | AI-generated (see [Docs/CharacterPipeline.md](Docs/CharacterPipeline.md)) | Player character — original design inspired by the reference | AI-generated, no third-party rights | — | Yes |
| Tower segments and top platform *(planned)* | Made for this project in Blender | Level geometry | Original work | — | Yes |
| Textures, icons, sky images *(planned)* | AI-generated | UI, tower ornaments, backgrounds | AI-generated, no third-party rights | — | Yes |
| Additional SFX and music *(planned)* | AI-generated | Impacts, whooshes, music | AI-generated, no third-party rights | — | Yes |

### Importing the Asset Store packages

The Standard Unity Asset Store EULA allows these packages in a shipped game (they are included in the APK),
but not public redistribution of their source files, so they are excluded from this repository.
To open the project with all effects:

1. Add both free packages to your Unity account via the links above.
2. Open the project, then **Window → Package Manager → My Assets**, and import:
   - *Cartoon FX Remaster Free* (into `Assets/JMO Assets`)
   - *Magic Effects FREE* (into `Assets/Hovl Studio`)
3. Project prefabs reference the effects by GUID, so they relink automatically after import.

## Assumptions

Ambiguities in the brief and how they were resolved will be listed here.
