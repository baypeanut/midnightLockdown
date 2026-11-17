#!/usr/bin/env python3
# narration via macos say
import json
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Lockdown", "Resources", "Audio", "vo")
sys.path.insert(0, HERE)
from robotize import robotize

LINES = [
    ("warden_intro_a", "WARDEN", "Intrusion detected on floor four. Kestrel Dynamics is now in lockdown."),
    ("warden_intro_b", "WARDEN", "All exits are sealed. Fire suppression will flood this floor in ten minutes."),
    ("dee_intro", "Dee", "Hey, it's Dee from building ops. WARDEN has you flagged as the intruder. Get the power back first. The breaker panel is on the back wall, and someone pulled its fuses."),
    ("dee_power", "Dee", "Lights are back. Kowalski's office is sealed, but the maintenance catwalk runs right over it. Climb the ladder by the storage shelves."),
    ("dee_catwalk", "Dee", "Grab the rungs with your grip buttons and pull yourself up. Try not to look down."),
    ("dee_office", "Dee", "You're in. Kowalski keeps the override card in that office, and his safe has the vault battery."),
    ("dee_clock", "Dee", "That wall clock has been stopped for weeks. He was always strange about that clock."),
    ("dee_safe", "Dee", "Old dial safe. Turn it slowly. You will feel it catch when you land on the right number."),
    ("dee_vault", "Dee", "The vault lock needs a laser on its sensor. Power the emitter, then turn the mirrors."),
    ("dee_final", "Dee", "Card in the slot, then hold the reboot button until it finishes. Do not let go."),
    ("warden_five", "WARDEN", "Five minutes until suppression."),
    ("warden_one", "WARDEN", "One minute until suppression."),
    ("warden_denied", "WARDEN", "Access denied."),
    ("warden_win", "WARDEN", "Reboot complete. Lockdown lifted. Have a pleasant night."),
    ("warden_lose", "WARDEN", "Suppression protocol engaged."),
]

VOICES = {"WARDEN": ("Daniel", "150"), "Dee": ("Samantha", "185")}


def speak(key, speaker, text, tmp):
    voice, rate = VOICES[speaker]
    # spoken text says warden as a word
    spoken = text.replace("WARDEN", "Warden")
    aiff = os.path.join(tmp, key + ".aiff")
    wav = os.path.join(OUT, key + ".wav")
    subprocess.run(["say", "-v", voice, "-r", rate, "-o", aiff, spoken], check=True)
    subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", aiff, wav], check=True)
    if speaker == "WARDEN":
        robotize(wav, wav)
    return wav


def main():
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        for key, speaker, text in LINES:
            wav = speak(key, speaker, text, tmp)
            print(key, os.path.getsize(wav))
    data = {"lines": [{"key": k, "speaker": s, "text": t} for k, s, t in LINES]}
    with open(os.path.join(OUT, "lines.json"), "w") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


if __name__ == "__main__":
    main()
