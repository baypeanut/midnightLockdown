#!/usr/bin/env python3
# synthetic ai voice filter
import sys
import wave
import numpy as np


def robotize(src, dst, ring_hz=40.0, mix=0.5, bits=10):
    with wave.open(src, "rb") as w:
        sr = w.getframerate()
        ch = w.getnchannels()
        x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(float) / 32768
    if ch > 1:
        x = x.reshape(-1, ch).mean(axis=1)
    t = np.arange(len(x)) / sr
    # ring mod blend
    y = (1 - mix) * x + mix * x * np.sin(2 * np.pi * ring_hz * t)
    # light bit crush
    step = 2.0 ** (1 - bits)
    y = np.round(y / step) * step
    y = y / (np.max(np.abs(y)) + 1e-12) * 10 ** (-3 / 20)
    pcm = np.clip(np.round(y * 32767), -32768, 32767).astype("<i2")
    with wave.open(dst, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())


if __name__ == "__main__":
    robotize(sys.argv[1], sys.argv[2])
