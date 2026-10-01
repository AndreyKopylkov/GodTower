# /// script
# requires-python = ">=3.10"
# dependencies = ["imageio-ffmpeg>=0.5"]
# ///
"""Record the God Tower playthrough video from a real Android device (brief §6, deliverable 2).

Drives the installed APK from the PC and records the screen:

* waits for the device (``adb wait-for-device``), forwards the webhook port (``adb forward tcp:56789 tcp:56789``),
* starts ``scrcpy`` recording (Matroska, survives an abrupt stop; remuxed to MP4 at the end),
* launches the app, opens the level select, starts Level 1 and plays Levels 1 -> 5 in order with ``adb shell input``
  (hold = ``input swipe x y x y <ms>`` on one point, lane change = a quick horizontal swipe),
* dodges villains using the level timelines exported from Unity (``level_timelines.json``, same seeded
  ``EventTimeline`` the game uses), synchronised on the ``[LevelRunner]`` log lines read from ``adb logcat``,
* plays "commentators": ``POST http://localhost:56789/bump`` at random 4-10 s intervals while a level is played,
* taps Next on every win (Retry after a loss), shows the "CHAMPION!" panel, returns to the menu and stops recording.

Usage (from the repository root, the phone connected over USB with USB debugging on and the APK installed)::

    uv run Tools/Video/record_playthrough.py                 # record to Recordings/godtower_playthrough.mp4
    uv run Tools/Video/record_playthrough.py --dry-run       # print the planned actions, no device needed
    uv run Tools/Video/record_playthrough.py --reset-progress --output Recordings/take2.mp4

Requirements: ``adb`` (PATH, ``$ANDROID_HOME/platform-tools`` or the Android SDK bundled with Unity 6000.5.3f1) and
``scrcpy`` 2.x (Windows: ``winget install Genymobile.scrcpy`` or ``scoop install scrcpy``; macOS: ``brew install scrcpy``;
Linux: ``apt install scrcpy``). Regenerate the timelines after changing the level table:
``Unity.exe -batchmode -quit -projectPath . -executeMethod GodTower.Editor.BuildTools.ExportLevelTimelines``.
"""

from __future__ import annotations

import argparse
import json
import os
import queue
import random
import re
import shutil
import signal
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from pathlib import Path

PACKAGE = "com.andreykopylkov.godtower"
PORT = 56789
BUMP_URL = f"http://localhost:{PORT}/bump"
SCRIPT_DIR = Path(__file__).resolve().parent
TIMELINES = SCRIPT_DIR / "level_timelines.json"
UNITY_ADB = Path("C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe")

# UI layout (Assets/_Project/Scripts/Editor/MenuUiBuilder.cs, GameHudBuilder.cs): CanvasScaler reference resolution
# 1080x1920, match width/height 0.5. Positions are button centres in reference units: (anchor x, anchor y) in
# normalised screen space (y = 0 at the bottom, like Unity) + offset (y up).
REFERENCE = (1080.0, 1920.0)
MENU_PLAY = ((0.5, 0.0), (0.0, 500.0))          # PLAY, bottom centre, centre 500 above the bottom
MENU_LEVELS = ((0.5, 0.0), (0.0, 280.0))        # LEVELS: 520x160, bottom edge at 200
LEVEL_SELECT_BACK = ((0.5, 0.5), (0.0, -355.0))  # popup 900x1000 centred; BACK 150 tall, 70 above the window bottom
RESULT_NEXT = ((0.5, 0.5), (0.0, -195.0))        # result 800x1120 centred; NEXT / RETRY 150 tall, top at -680 from the top
RESULT_MENU = ((0.5, 0.5), (0.0, -385.0))        # MENU, top at -870 from the window top
TILE_PITCH = 260.0                               # level tiles 220x220 + 40 spacing; row 0 centre +150, row 1 centre -110


def level_tile(index: int) -> tuple[tuple[float, float], tuple[float, float]]:
    """Centre of level tile ``index`` (0-based): 3 tiles on the first row, 2 centred on the second."""
    row, column = divmod(index, 3)
    in_row = 3 if row == 0 else 2
    x = (column - (in_row - 1) * 0.5) * TILE_PITCH
    y = 150.0 - row * TILE_PITCH
    return (0.5, 0.5), (x, y)


# Gestures. The game reads a swipe as >= 8% of the screen width within 0.35 s; a lane hop takes 0.2 s (swipes during
# a hop, a hit, a fall or a hero carry are ignored, so moves are planned away from those).
HOLD_CHUNK = 1.2           # s; a hold is split so the script can react to the end of the level
SWIPE_TRAVEL = 0.30        # of the screen width
SWIPE_MS = 120
SWIPE_GAP = 0.12           # s between swipes (lets the 0.2 s hop finish together with the adb round trip)
STEP_TIME = 0.42           # s budget per swipe incl. adb overhead (for planning)
MIN_LEAD = 1.1             # s; moves start at least this long before the impact
BUMP_QUIET_BEFORE = 1.4    # s; no bump right before a planned move (its ~0.55 s input lock would eat the swipe)
BUMP_QUIET_AFTER = 0.6
RESULT_DELAY = 2.4         # s from the end of a level to a tappable result panel (1.4 s delay + pop-in)
LANES = 3
CENTRE_LANE = 1

LEVEL_LINE = re.compile(r"\[LevelRunner\] Level (\d+) (started|Won|Lost)")


# --------------------------------------------------------------------------------------------------------------------
# Planning (pure; used by --dry-run as well)
# --------------------------------------------------------------------------------------------------------------------

@dataclass
class Move:
    time: float               # level clock
    target: int
    swipes: list[int]          # +1 = swipe right (lane index up), -1 = left
    reason: str


@dataclass
class LevelPlan:
    number: int
    time_limit: float
    moves: list[Move] = field(default_factory=list)
    bumps: list[float] = field(default_factory=list)


def plan_moves(level: dict) -> list[Move]:
    """Lane changes that keep the climber out of every villain's lanes, starting in the centre lane."""
    boosts = [(b["time"], b["time"] + b["duration"]) for b in level["boosts"]]
    strikes = sorted(level["strikes"], key=lambda s: s["impact"])
    moves: list[Move] = []
    lane = CENTRE_LANE
    previous_impact = 0.0
    previous_mask = 0
    for strike in strikes:
        mask = strike["laneMask"]
        if not mask & (1 << lane):
            previous_impact, previous_mask = strike["impact"], mask
            continue

        safe = [candidate for candidate in range(LANES) if not mask & (1 << candidate)]
        target = min(safe, key=lambda candidate: (abs(candidate - lane), candidate == CENTRE_LANE))
        direction = 1 if target > lane else -1
        swipes = [direction] * abs(target - lane)
        if target in (0, LANES - 1):
            swipes.append(direction)  # one extra swipe into the edge re-syncs the lane if a swipe was ever lost

        lead = max(level["telegraph"], MIN_LEAD, STEP_TIME * len(swipes) + 0.3)
        start = strike["impact"] - lead
        if previous_mask & (1 << target):
            start = max(start, previous_impact + 0.15)  # do not step into the previous villain's lane
        for boost_start, boost_end in boosts:
            if boost_start - 0.1 <= start <= boost_end:
                start = boost_end + 0.15  # carried: lane changes are ignored
        moves.append(Move(round(max(start, 0.5), 2), target, swipes, f'{strike["kind"]} @{strike["impact"]:.1f}s'))
        lane = target
        previous_impact, previous_mask = strike["impact"], mask
    return moves


def plan_bumps(time_limit: float, moves: list[Move], rng: random.Random) -> list[float]:
    """Commentator bumps every 4-10 s (level clock), shifted out of the quiet window around each planned move."""
    bumps: list[float] = []
    t = rng.uniform(4.0, 10.0)
    while t < time_limit:
        for move in moves:
            if move.time - BUMP_QUIET_BEFORE <= t <= move.time + STEP_TIME * len(move.swipes) + BUMP_QUIET_AFTER:
                t = move.time + STEP_TIME * len(move.swipes) + BUMP_QUIET_AFTER
        bumps.append(round(t, 2))
        t += rng.uniform(4.0, 10.0)
    return bumps


def build_plans(timelines: dict, seed: int) -> list[LevelPlan]:
    rng = random.Random(seed)
    plans = []
    for level in timelines["levels"]:
        moves = plan_moves(level)
        plans.append(LevelPlan(level["number"], level["timeLimit"], moves, plan_bumps(level["timeLimit"], moves, rng)))
    return plans


# --------------------------------------------------------------------------------------------------------------------
# Device
# --------------------------------------------------------------------------------------------------------------------

def find_adb(explicit: str | None) -> str:
    candidates = [explicit, shutil.which("adb")]
    for env in ("ANDROID_HOME", "ANDROID_SDK_ROOT"):
        if os.environ.get(env):
            candidates.append(str(Path(os.environ[env]) / "platform-tools" / ("adb.exe" if os.name == "nt" else "adb")))
    candidates.append(str(UNITY_ADB))
    for candidate in candidates:
        if candidate and Path(candidate).exists() or (candidate and shutil.which(candidate)):
            return candidate
    sys.exit("adb not found: pass --adb, add it to PATH or set ANDROID_HOME.")


class Device:
    def __init__(self, adb: str, serial: str | None, dry_run: bool, width: int = 1080, height: int = 1920):
        self.adb = adb
        self.serial = serial
        self.dry_run = dry_run
        self.width, self.height = width, height

    def cmd(self, *args: str) -> list[str]:
        return [self.adb, *(["-s", self.serial] if self.serial else []), *args]

    def run(self, *args: str, check: bool = True) -> str:
        if self.dry_run:
            print("   adb " + " ".join(args))
            return ""
        result = subprocess.run(self.cmd(*args), capture_output=True, text=True)
        if check and result.returncode != 0:
            raise RuntimeError(f"adb {' '.join(args)} failed: {result.stderr.strip()}")
        return result.stdout

    def read_screen_size(self) -> None:
        if self.dry_run:
            return
        output = self.run("shell", "wm", "size")
        sizes = re.findall(r"(Physical|Override) size: (\d+)x(\d+)", output)
        if not sizes:
            raise RuntimeError(f"Unexpected 'wm size' output: {output!r}")
        _, w, h = sizes[-1]  # the override wins when present
        self.width, self.height = sorted((int(w), int(h)))  # portrait

    def point(self, ui: tuple[tuple[float, float], tuple[float, float]], safe_top: int = 0) -> tuple[int, int]:
        """Screen pixel (y down) of a UI point given as (anchor, offset in reference units)."""
        (ax, ay), (dx, dy) = ui
        scale = ((self.width / REFERENCE[0]) * (self.height / REFERENCE[1])) ** 0.5  # CanvasScaler match 0.5
        usable = self.height - safe_top
        x = ax * self.width + dx * scale
        y = safe_top + (1.0 - ay) * usable - dy * scale
        return round(x), round(y)

    def tap(self, ui, label: str, safe_top: int = 0) -> None:
        x, y = self.point(ui, safe_top)
        log(f"tap {label} at ({x}, {y})")
        self.run("shell", "input", "tap", str(x), str(y))

    def hold(self, seconds: float) -> None:
        x, y = self.width // 2, int(self.height * 0.55)
        if self.dry_run:
            return
        self.run("shell", "input", "swipe", str(x), str(y), str(x), str(y), str(int(seconds * 1000)))

    def swipe(self, direction: int) -> None:
        y = int(self.height * 0.55)
        x0 = self.width // 2
        x1 = int(x0 + direction * SWIPE_TRAVEL * self.width)
        self.run("shell", "input", "swipe", str(x0), str(y), str(x1), str(y), str(SWIPE_MS))


class LevelLog:
    """``adb logcat`` reader: queues ``(level, event, host time)`` for ``[LevelRunner]`` lines."""

    def __init__(self, device: Device):
        self.events: queue.Queue[tuple[int, str, float]] = queue.Queue()
        device.run("logcat", "-c", check=False)
        self.process = subprocess.Popen(device.cmd("logcat", "-v", "brief", "Unity:I", "*:S"),
                                        stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, errors="replace")
        threading.Thread(target=self._read, daemon=True).start()

    def _read(self) -> None:
        for line in self.process.stdout:
            match = LEVEL_LINE.search(line)
            if match:
                self.events.put((int(match.group(1)), match.group(2), time.monotonic()))

    def wait(self, kinds: tuple[str, ...], timeout: float) -> tuple[int, str, float] | None:
        deadline = time.monotonic() + timeout
        while (left := deadline - time.monotonic()) > 0:
            try:
                event = self.events.get(timeout=left)
            except queue.Empty:
                return None
            if event[1] in kinds:
                return event
        return None

    def poll(self) -> tuple[int, str, float] | None:
        try:
            return self.events.get_nowait()
        except queue.Empty:
            return None

    def close(self) -> None:
        self.process.terminate()


class Commentators:
    """Sends the planned bumps on the level clock from a background thread (fire and forget, logs the status)."""

    def __init__(self, start: float, bumps: list[float]):
        self.start = start
        self.bumps = bumps
        self.stopped = threading.Event()
        self.sent: list[int] = []
        threading.Thread(target=self._run, daemon=True).start()

    def _run(self) -> None:
        for at in self.bumps:
            if self.stopped.wait(max(0.0, self.start + at - time.monotonic())):
                return
            self.sent.append(post_bump())

    def stop(self) -> None:
        self.stopped.set()


def post_bump() -> int:
    request = urllib.request.Request(BUMP_URL, method="POST", data=b"")
    try:
        with urllib.request.urlopen(request, timeout=3) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code
    except OSError:
        return -1


# --------------------------------------------------------------------------------------------------------------------
# Recording
# --------------------------------------------------------------------------------------------------------------------

class Recorder:
    def __init__(self, scrcpy: str, adb: str, serial: str | None, output: Path, dry_run: bool):
        self.output = output
        self.raw = output.with_suffix(".mkv")
        self.process = None
        args = [scrcpy, "--record", str(self.raw), "--video-bit-rate", "16M", "--max-fps", "60", "--no-audio-playback",
                "--stay-awake", *(["--serial", serial] if serial else [])]
        log("start recording: " + " ".join(args))
        if dry_run:
            return
        self.raw.parent.mkdir(parents=True, exist_ok=True)
        env = dict(os.environ, ADB=adb)  # same adb server as this script
        flags = subprocess.CREATE_NEW_PROCESS_GROUP if os.name == "nt" else 0
        self.process = subprocess.Popen(args, env=env, creationflags=flags)
        time.sleep(3.0)
        if self.process.poll() is not None:
            raise RuntimeError("scrcpy exited right away (is it installed and is the device authorised?)")

    def stop(self) -> None:
        if self.process is None:
            return
        log("stop recording")
        try:
            self.process.send_signal(signal.CTRL_BREAK_EVENT if os.name == "nt" else signal.SIGINT)
            self.process.wait(timeout=10)
        except (subprocess.TimeoutExpired, OSError, ValueError):
            self.process.terminate()  # Matroska stays readable even when cut
            self.process.wait(timeout=10)
        self._remux()

    def _remux(self) -> None:
        if self.output.suffix.lower() == ".mkv" or not self.raw.exists():
            return
        import imageio_ffmpeg

        ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
        result = subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(self.raw), "-c", "copy", str(self.output)])
        if result.returncode == 0:
            self.raw.unlink()
            log(f"saved {self.output}")
        else:
            log(f"remux failed, keeping {self.raw}")


# --------------------------------------------------------------------------------------------------------------------
# Script
# --------------------------------------------------------------------------------------------------------------------

def log(message: str) -> None:
    print(f"[{time.strftime('%H:%M:%S')}] {message}", flush=True)


def play_level(device: Device, events: LevelLog | None, plan: LevelPlan, dry_run: bool) -> str:
    """Plays one level from its start line to its end line; returns 'Won' or 'Lost'."""
    log(f"Level {plan.number}: {len(plan.moves)} planned lane changes, {len(plan.bumps)} commentator bumps")
    if dry_run:
        for move in plan.moves:
            arrows = "".join("R" if s > 0 else "L" for s in move.swipes)
            print(f"   t={move.time:6.2f}s  swipe {arrows:<3} -> lane {move.target}   (dodge {move.reason})")
        print("   bumps at t = " + ", ".join(f"{t:.1f}" for t in plan.bumps))
        print(f"   hold to climb in {HOLD_CHUNK:.1f} s chunks between moves until the Won line")
        return "Won"

    started = events.wait(("started",), timeout=20.0) if events else None
    if started and started[0] != plan.number:
        log(f"warning: expected Level {plan.number}, the game started Level {started[0]}")
    start = started[2] if started else time.monotonic() - 1.0
    if not started:
        log("warning: no start line in logcat, timing estimated from the tap")

    commentators = Commentators(start, plan.bumps)
    moves = list(plan.moves)
    outcome = None
    while outcome is None:
        now = time.monotonic() - start
        if moves and now >= moves[0].time:
            move = moves.pop(0)
            for direction in move.swipes:
                device.swipe(direction)
                time.sleep(SWIPE_GAP)
            continue

        next_move = moves[0].time - now if moves else HOLD_CHUNK
        chunk = min(HOLD_CHUNK, next_move)
        if chunk > 0.15:
            device.hold(chunk)
        else:
            time.sleep(max(chunk, 0.0))

        event = events.poll() if events else None
        if event and event[1] in ("Won", "Lost"):
            outcome = event[1]
        elif now > plan.time_limit + 5.0:
            outcome = "Lost"  # no end line seen; the timer has run out anyway

    commentators.stop()
    log(f"Level {plan.number} {outcome}; bump responses {commentators.sent}")
    return outcome


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--dry-run", action="store_true", help="print the planned actions, no device needed")
    parser.add_argument("--output", type=Path, default=Path("Recordings/godtower_playthrough.mp4"))
    parser.add_argument("--adb", help="adb executable (default: PATH, ANDROID_HOME or the Unity-bundled SDK)")
    parser.add_argument("--scrcpy", default="scrcpy", help="scrcpy executable")
    parser.add_argument("--serial", help="device serial when several are connected")
    parser.add_argument("--seed", type=int, default=2026, help="seed of the commentator schedule")
    parser.add_argument("--reset-progress", action="store_true", help="clear the app data first (level select starts locked)")
    parser.add_argument("--safe-top", type=int, default=0, help="top safe-area inset in px (notch) if taps land too high")
    parser.add_argument("--no-record", action="store_true", help="drive the game without scrcpy")
    args = parser.parse_args()

    if not TIMELINES.exists():
        sys.exit(f"{TIMELINES} is missing; export it with BuildTools.ExportLevelTimelines.")
    plans = build_plans(json.loads(TIMELINES.read_text(encoding="utf-8")), args.seed)

    adb = find_adb(args.adb) if not args.dry_run else (args.adb or "adb")
    device = Device(adb, args.serial, args.dry_run)
    if args.dry_run:
        log(f"dry run, layout for a {device.width}x{device.height} screen")
    else:
        if not shutil.which(args.scrcpy) and not args.no_record:
            sys.exit("scrcpy not found (winget install Genymobile.scrcpy / brew install scrcpy) or pass --no-record.")
        log("waiting for the device (USB debugging on, authorised)")
    device.run("wait-for-device")
    device.read_screen_size()
    log(f"screen {device.width}x{device.height}")
    device.run("forward", f"tcp:{PORT}", f"tcp:{PORT}")
    if args.reset_progress:
        device.run("shell", "pm", "clear", PACKAGE)
    device.run("shell", "am", "force-stop", PACKAGE)

    recorder = None if args.no_record else Recorder(args.scrcpy, adb, args.serial, args.output, args.dry_run)
    events = None if args.dry_run else LevelLog(device)
    try:
        log("launch the app")
        device.run("shell", "monkey", "-p", PACKAGE, "-c", "android.intent.category.LAUNCHER", "1")
        pause(6.0, args.dry_run)                       # splash + menu fade-in, show the menu for a moment
        log(f"menu: /bump -> {post_bump() if not args.dry_run else 409} (409 expected outside a level)")
        device.tap(MENU_LEVELS, "LEVELS")
        pause(2.0, args.dry_run)                       # show the level select (locks / stars)
        device.tap(level_tile(0), "Level 1 tile", args.safe_top)

        for index, plan in enumerate(plans):
            while True:
                outcome = play_level(device, events, plan, args.dry_run)
                pause(RESULT_DELAY, args.dry_run)
                if outcome == "Won":
                    break
                device.tap(RESULT_NEXT, "RETRY", args.safe_top)  # Retry sits where Next is
            if index + 1 < len(plans):
                device.tap(RESULT_NEXT, "NEXT", args.safe_top)
            else:
                pause(3.0, args.dry_run)               # the CHAMPION! panel
                device.tap(RESULT_MENU, "MENU", args.safe_top)
                pause(4.0, args.dry_run)               # back in the menu, everything unlocked
                device.tap(MENU_LEVELS, "LEVELS")
                pause(3.0, args.dry_run)
                device.tap(LEVEL_SELECT_BACK, "BACK", args.safe_top)
                pause(2.0, args.dry_run)
    finally:
        if events:
            events.close()
        if recorder:
            recorder.stop()
    log("done")
    return 0


def pause(seconds: float, dry_run: bool) -> None:
    if dry_run:
        print(f"   wait {seconds:.1f} s")
    else:
        time.sleep(seconds)


if __name__ == "__main__":
    sys.exit(main())
