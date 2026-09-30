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

Only free-to-use or AI-generated assets are used. Full list with licenses and source links will be added here.

## Assumptions

Ambiguities in the brief and how they were resolved will be listed here.
