#!/usr/bin/env python3
"""Verify the built macOS player's smoke contract without modifying the user's save."""

import argparse
import json
from pathlib import Path
import re
import subprocess
import tempfile


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    repo = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, default=repo / "VillageClash/Builds/VillageClash.app/Contents/MacOS/Village Clash")
    args = parser.parse_args()
    require(args.player.is_file(), "Build the macOS player before running verification.")
    normal_save = Path.home() / "Library/Application Support/VillageClash/Village Clash/village.json"

    def player_save_files():
        return {path.name: (path.read_bytes(), path.stat().st_mtime_ns)
                for path in normal_save.parent.glob(normal_save.name + "*") if path.is_file()}

    before = player_save_files()
    output = Path(tempfile.mkdtemp(prefix="village-clash-verification-"))
    print(f"Verification logs: {output}", flush=True)

    def run(name, options, expected_exit):
        log = output / f"{name}.log"
        completed = subprocess.run(
            [str(args.player.resolve()), "-batchmode", "-nographics", "-logFile", str(log), *options],
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=200,
        )
        (output / f"{name}.stdout").write_text(completed.stdout)
        text = (log.read_text() if log.exists() else "") + completed.stdout
        require(completed.returncode != 0 if expected_exit is None else completed.returncode == expected_exit,
                f"{name}: expected exit {expected_exit if expected_exit is not None else 'nonzero'}, got {completed.returncode}; see {log}")
        require(("ALL CHECKS PASSED" in text) == (expected_exit == 0),
                f"{name}: incorrect success marker; see {log}")
        require(expected_exit == 0 or "[Smoke] FAIL" in text,
                f"{name}: missing failure explanation; see {log}")
        require(expected_exit is not None or "runner timed out" in text,
                f"{name}: watchdog did not report its deadline; see {log}")
        match = re.search(r"\[Smoke\] isolated save: (.+)", text)
        require(match is not None, f"{name}: no isolated save was reported; see {log}")
        save = Path(match.group(1).strip())
        require(save.resolve() != normal_save.resolve(), f"{name}: smoke used the player save")
        print(f"PASS {name} (exit {completed.returncode})", flush=True)
        return save

    try:
        first = run("gameplay-first", ["-smoke-test"], 0)
        second = run("gameplay-repeat", ["-smoke-test"], 0)
        require(first != second, "Repeated runs reused a save path")
        require(json.loads(first.read_text()) == json.loads(second.read_text()),
                "Repeated runs produced different saved states")
        print("PASS independent runs produce identical game state", flush=True)
        for save in (first, second):
            backup = Path(str(save) + ".bak")
            require(backup.is_file(), "Gameplay did not create a backup save")
            require(json.loads(backup.read_text())["version"] == 1, "Backup is not a readable save")
            require(not Path(str(save) + ".tmp").exists(), "Successful save left a temporary file")
        print("PASS gameplay saves have readable backups and no uncommitted temporary files", flush=True)
        run("missing-shots-directory", ["-shots"], 1)
        run("headless-screenshots-rejected", ["-shots", str(output / "shots")], 1)
        run("watchdog", ["-smoke-test", "-smoke-timeout", "1"], None)
    finally:
        require(before == player_save_files(), "Normal player save or recovery files changed during verification")
        print("PASS normal player save and recovery files unchanged", flush=True)
    print("All smoke-runner verification checks passed.")


if __name__ == "__main__":
    main()
