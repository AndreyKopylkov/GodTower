#!/usr/bin/env bash
# Rebuild the hero end to end (atlas -> model/rig/clips/FBX -> verification -> preview renders).
# Run from the project root:  bash Tools/Blender/Hero/build_all.sh
set -euo pipefail
BLENDER="${BLENDER:-D:/Games/Steam/steamapps/common/Blender/blender.exe}"
D=Tools/Blender/Hero
uv run --with pillow python $D/make_atlas.py
"$BLENDER" -b --factory-startup --python $D/build_hero.py 2>&1 | grep -E "HERO_|CLIP|BUILD_OK|Error|Traceback|line [0-9]"
"$BLENDER" -b --factory-startup --python $D/verify_fbx.py 2>&1 | grep -E "VERIFY|Error|Traceback|line [0-9]"
"$BLENDER" -b Art/Source/Character/hero.blend --python $D/render_previews.py 2>&1 | grep -E "RENDER_OK|Error|Traceback"
uv run --with pillow python $D/make_sheets.py
