#!/usr/bin/env python3
# procedural sfx for midnight lockdown
import os
import wave
import numpy as np

SR = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "..", "Assets", "Lockdown", "Resources", "Audio", "sfx")
rng = np.random.default_rng(4)


# basic helpers

def tt(dur):
    return np.arange(int(round(dur * SR))) / SR


def silence(dur):
    return np.zeros(int(round(dur * SR)))


def noise(dur):
    return rng.standard_normal(int(round(dur * SR)))


def phase_of(freq, loop=False):
    # integrate frequency
    cyc = np.cumsum(freq) / SR
    if loop:
        total = cyc[-1]
        cyc = cyc * (max(1, round(total)) / total)
    return 2 * np.pi * np.concatenate(([0.0], cyc[:-1]))


def decay(n, tau, attack=0.002):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-5), 0, 1)
    return a * np.exp(-t / tau)


def fade(x, fin=0.003, fout=0.01):
    x = x.copy()
    a, b = int(fin * SR), int(fout * SR)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def fft_band(x, lo, hi, soft=0.15):
    # circular bandpass, loop safe
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    g = np.ones_like(f)
    if lo > 0:
        g *= 1 / (1 + (lo / np.maximum(f, 1e-3)) ** (2 / soft * 0.3))
    if hi < SR / 2:
        g *= 1 / (1 + (f / hi) ** (2 / soft * 0.3))
    return np.fft.irfft(spec * g, len(x))


def svf(x, fc, q=0.7, mode="bp"):
    # state variable filter, per sample cutoff
    fc = np.broadcast_to(np.asarray(fc, float), x.shape)
    out = np.empty_like(x)
    lp = bp = 0.0
    damp = 1.0 / q
    for i in range(len(x)):
        f = 2 * np.sin(np.pi * min(fc[i], SR * 0.22) / SR)
        hp = x[i] - lp - damp * bp
        bp += f * hp
        lp += f * bp
        out[i] = bp if mode == "bp" else (lp if mode == "lp" else hp)
    return out


def partials(dur, freqs, taus, amps=None):
    t = tt(dur)
    amps = amps or [1.0] * len(freqs)
    y = np.zeros_like(t)
    for f, tau, a in zip(freqs, taus, amps):
        y += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t / tau)
    return y


def click(dur, lo, hi, tau):
    n = noise(dur)
    return fft_band(n, lo, hi) * decay(len(n), tau, 0.0005)


def place(buf, x, at):
    i = int(at * SR)
    j = min(len(buf), i + len(x))
    buf[i:j] += x[: j - i]
    return buf


def thump(dur, f0, f1, tau):
    t = tt(dur)
    f = f1 + (f0 - f1) * np.exp(-t / (tau * 0.4))
    return np.sin(phase_of(f)) * decay(len(t), tau, 0.003)


def bell(dur, f, tau, attack=0.003):
    t = tt(dur)
    y = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * f * 2.01 * t) * np.exp(-t / (tau * 0.4))
    return y * decay(len(t), tau, attack)


# sound definitions

def alarm_whoop():
    t = tt(2.0)
    p = (t % 1.0) / 1.0
    f = 420 + 780 * p ** 0.6
    ph = phase_of(f, loop=True)
    y = np.tanh(2.5 * np.sin(ph)) + 0.3 * np.sin(2 * ph)
    amp = 0.55 + 0.45 * np.sin(np.pi * np.clip(p / 0.97, 0, 1)) ** 0.5
    return y * amp


def room_hum_loop():
    t = tt(4.0)
    y = (np.sin(2 * np.pi * 60 * t) + 0.5 * np.sin(2 * np.pi * 120 * t)
         + 0.25 * np.sin(2 * np.pi * 180 * t) + 0.1 * np.sin(2 * np.pi * 240 * t))
    pulses = np.sign(np.sin(2 * np.pi * 120 * t)) * (np.abs(np.sin(2 * np.pi * 120 * t)) > 0.97)
    buzz = fft_band(pulses.astype(float), 2500, 6500)
    buzz /= np.max(np.abs(buzz)) + 1e-9
    air = fft_band(rng.standard_normal(len(t)), 100, 900)
    air /= np.max(np.abs(air)) + 1e-9
    wob = 1 + 0.08 * np.sin(2 * np.pi * 0.5 * t)
    return y * wob + 0.12 * buzz + 0.05 * air


def vent_wind_loop():
    t = tt(4.0)
    lo = fft_band(rng.standard_normal(len(t)), 60, 500)
    mid = fft_band(rng.standard_normal(len(t)), 300, 1500)
    lo /= np.max(np.abs(lo))
    mid /= np.max(np.abs(mid))
    g1 = 0.7 + 0.3 * np.sin(2 * np.pi * 0.25 * t)
    g2 = 0.25 + 0.2 * np.sin(2 * np.pi * 0.5 * t + 1.3) + 0.1 * np.sin(2 * np.pi * 0.75 * t)
    return lo * g1 + mid * g2


def spark():
    buf = silence(0.4)
    for _ in range(26):
        at = rng.uniform(0, 0.36) ** 1.4 / 0.36 ** 0.4
        c = click(0.02, 1800, 9000, rng.uniform(0.001, 0.004)) * rng.uniform(0.3, 1.0)
        place(buf, c, at)
    buzz = np.sign(np.sin(2 * np.pi * 95 * tt(0.4))) * decay(len(buf), 0.08)
    buf += 0.15 * fft_band(buzz, 300, 4000)
    return buf * np.exp(-tt(0.4) / 0.2)


def fuse_insert():
    buf = silence(0.25)
    place(buf, click(0.02, 2500, 8000, 0.002), 0.0)
    place(buf, 0.8 * thump(0.2, 220, 110, 0.04), 0.03)
    place(buf, 0.6 * click(0.03, 600, 3000, 0.004), 0.03)
    return buf


def lever_clunk():
    buf = silence(0.5)
    hit = partials(0.45, [310, 742, 1230, 1870], [0.12, 0.08, 0.05, 0.03], [1, 0.6, 0.4, 0.25])
    hit = hit * decay(len(hit), 0.2, 0.001)
    place(buf, hit, 0.0)
    place(buf, 1.2 * thump(0.4, 140, 60, 0.09), 0.0)
    place(buf, 0.7 * click(0.04, 400, 5000, 0.006), 0.0)
    place(buf, 0.35 * partials(0.2, [520, 1310], [0.04, 0.03]), 0.12)
    return buf


def power_up():
    t = tt(1.5)
    f = 60 + 900 * (np.clip(t / 1.1, 0, 1) ** 2) * (1 - np.clip((t - 1.1) / 0.4, 0, 1)) ** 0.5
    f = np.maximum(f, 60)
    ph = phase_of(f)
    whine = np.sin(ph) + 0.3 * np.sin(2 * ph)
    wamp = np.clip(t / 1.1, 0, 1) * (1 - np.clip((t - 1.1) / 0.35, 0, 1))
    hum = (np.sin(2 * np.pi * 60 * t) + 0.5 * np.sin(2 * np.pi * 120 * t)) * np.clip((t - 1.0) / 0.3, 0, 1)
    return fade(0.6 * whine * wamp + 0.7 * hum, 0.01, 0.15)


def power_down():
    t = tt(1.0)
    f = 50 + 850 * np.exp(-t / 0.3)
    ph = phase_of(f)
    y = np.sin(ph) + 0.3 * np.sin(2 * ph) + 0.15 * np.sin(3 * ph)
    return fade(y * np.exp(-t / 0.45), 0.005, 0.1)


def dial_tick():
    y = partials(0.03, [3100, 5200, 7400], [0.006, 0.004, 0.003], [1, 0.6, 0.3])
    return y + 0.4 * click(0.03, 3000, 9000, 0.0015)


def dial_catch():
    buf = partials(0.15, [1800, 2900, 4400, 320], [0.04, 0.03, 0.02, 0.03], [1, 0.7, 0.4, 0.6])
    place(buf, 0.6 * click(0.02, 1500, 8000, 0.003), 0.0)
    place(buf, 0.3 * partials(0.05, [2500, 3700], [0.01, 0.008]), 0.012)
    return buf


def safe_open():
    buf = silence(0.9)
    sl = noise(0.32)
    sl = svf(sl, np.linspace(700, 1800, len(sl)), 3.0) * np.sin(np.pi * np.linspace(0, 1, len(sl))) ** 0.6
    place(buf, 0.6 * sl, 0.0)
    place(buf, partials(0.2, [880, 1650, 2700], [0.05, 0.03, 0.02]), 0.32)
    place(buf, 0.8 * thump(0.3, 160, 70, 0.06), 0.32)
    # creak from jittered pulse train
    t = tt(0.5)
    f = 70 + 40 * np.sin(2 * np.pi * 1.6 * t) + 15 * rng.standard_normal(len(t)).cumsum() / np.sqrt(len(t))
    ph = phase_of(np.maximum(f, 30))
    pulse = (np.sin(ph) > 0.92).astype(float)
    cr = svf(pulse, 900, 6.0) + 0.5 * svf(pulse, 2100, 8.0)
    cr *= np.sin(np.pi * np.linspace(0, 1, len(t))) ** 0.8
    place(buf, 0.9 * cr / (np.max(np.abs(cr)) + 1e-9), 0.4)
    return fade(buf, 0.002, 0.03)


def clock_tick():
    return partials(0.05, [2200, 3500, 5100], [0.008, 0.006, 0.004], [1, 0.5, 0.3]) + 0.3 * click(0.05, 2000, 7000, 0.002)


def clock_open():
    t = tt(0.6)
    f = 260 * (1 + 0.25 * np.sin(2 * np.pi * 18 * t) * np.exp(-t / 0.15)) * (1 + 0.3 * np.exp(-t / 0.05))
    y = np.sin(phase_of(f)) * decay(len(t), 0.18)
    y += 0.4 * np.sin(phase_of(f * 2.7)) * decay(len(t), 0.08)
    buf = 0.8 * y
    place(buf, click(0.03, 1000, 7000, 0.003), 0.0)
    return fade(buf, 0.001, 0.05)


def laser_hum_loop():
    t = tt(2.0)
    ph = 2 * np.pi * 220 * t
    saw = sum(np.sin(k * ph) / k for k in range(1, 12))
    am = 0.8 + 0.2 * np.sin(2 * np.pi * 50 * t)
    shimmer = 0.15 * np.sin(2 * np.pi * 1760 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 3 * t))
    return saw * am + shimmer


def laser_hit():
    t = tt(0.3)
    sz = fft_band(noise(0.3), 2500, 9000) * (0.6 + 0.4 * rng.random(len(t)))
    tone = np.sin(2 * np.pi * 1200 * t + 3 * np.sin(2 * np.pi * 60 * t))
    return fade((sz + 0.4 * tone) * decay(len(t), 0.1, 0.002), 0.001, 0.03)


def mirror_step():
    buf = silence(0.06)
    for i, at in enumerate([0.0, 0.018, 0.036]):
        place(buf, (1 - 0.2 * i) * partials(0.02, [2600, 4100], [0.004, 0.003]), at)
        place(buf, 0.4 * click(0.015, 2000, 8000, 0.0015), at)
    return buf


def shutter_dissolve():
    t = tt(2.0)
    env = np.sin(np.pi * np.clip(t / 2.0, 0, 1)) ** 1.5
    fc = 400 + 3500 * (t / 2.0) ** 1.2
    air = svf(noise(2.0), fc, 1.2)
    sh = np.zeros_like(t)
    for f in [2093, 2637, 3136, 3951, 4699]:
        sh += np.sin(2 * np.pi * f * t) * (0.5 + 0.5 * np.sin(2 * np.pi * rng.uniform(5, 11) * t + rng.uniform(0, 6)))
    sh *= np.clip((t - 0.3) / 1.0, 0, 1)
    return fade((air / np.max(np.abs(air)) + 0.08 * sh) * env, 0.01, 0.1)


def card_insert():
    buf = silence(0.3)
    sl = svf(noise(0.22), np.linspace(1200, 3000, len(tt(0.22))), 2.0)
    sl *= np.linspace(0.2, 1, len(sl))
    place(buf, 0.5 * sl / np.max(np.abs(sl)), 0.0)
    place(buf, partials(0.06, [2400, 3800], [0.01, 0.008]), 0.23)
    place(buf, 0.6 * click(0.03, 1500, 8000, 0.002), 0.23)
    return fade(buf, 0.005, 0.01)


def reboot_charge():
    t = tt(3.0)
    f = 150 * (8 ** (t / 3.0))
    ph = phase_of(f)
    y = np.sin(ph) + 0.4 * np.sin(2 * ph) + 0.2 * np.sin(3 * ph)
    trem = 0.75 + 0.25 * np.sin(phase_of(3 + 20 * (t / 3.0) ** 2))
    amp = 0.3 + 0.7 * (t / 3.0)
    return fade(y * trem * amp, 0.02, 0.04)


def win_stinger():
    buf = silence(2.5)
    notes = [523.25, 659.25, 783.99, 1046.5]
    for i, f in enumerate(notes):
        place(buf, 0.5 * bell(2.5 - i * 0.13, f, 0.5), i * 0.13)
    for f in [523.25, 659.25, 783.99, 1046.5, 1318.5]:
        place(buf, 0.3 * bell(1.9, f, 0.7, 0.01), 0.6)
    return fade(buf, 0.001, 0.2)


def lose_stinger():
    buf = silence(2.5)
    for i, f in enumerate([196.0, 155.56, 130.81, 65.41]):
        dur = 2.5 - i * 0.45
        t = tt(dur)
        ph = 2 * np.pi * f * t + 0.004 * np.sin(2 * np.pi * 5 * t) * 2 * np.pi
        saw = sum(np.sin(k * ph) / k for k in range(1, 8))
        place(buf, 0.45 * saw * decay(len(t), 0.5 if i < 3 else 1.2, 0.02), i * 0.45)
    return fade(buf, 0.001, 0.3)


def beep_ok():
    buf = silence(0.2)
    for f, at in [(880, 0.0), (1320, 0.1)]:
        t = tt(0.09)
        place(buf, fade(np.sin(2 * np.pi * f * t) + 0.2 * np.sin(4 * np.pi * f * t), 0.003, 0.02), at)
    return buf


def beep_error():
    buf = silence(0.35)
    for at in [0.0, 0.19]:
        t = tt(0.15)
        y = np.tanh(3 * np.sin(2 * np.pi * 150 * t)) + 0.5 * np.tanh(3 * np.sin(2 * np.pi * 157 * t))
        place(buf, fade(y, 0.004, 0.02), at)
    return buf


def chip_pickup():
    buf = silence(0.5)
    for i, f in enumerate([1567.98, 2093.0, 2637.0, 3136.0]):
        place(buf, 0.45 * bell(0.5 - i * 0.05, f, 0.12), i * 0.05)
    sp = fft_band(noise(0.5), 5000, 10000) * decay(len(tt(0.5)), 0.12)
    return fade(buf + 0.08 * sp, 0.001, 0.05)


def ui_click():
    return partials(0.05, [1500, 3200], [0.006, 0.004]) + 0.3 * click(0.05, 1500, 6000, 0.0015)


def grab():
    return fade(click(0.08, 200, 1200, 0.015), 0.002, 0.02)


def release():
    return fade(0.7 * click(0.08, 500, 2500, 0.01), 0.002, 0.02)


def climb_grip():
    buf = partials(0.12, [520, 1340, 2210, 3150], [0.05, 0.03, 0.02, 0.015], [1, 0.6, 0.4, 0.2])
    place(buf, 0.6 * thump(0.1, 200, 90, 0.02), 0.0)
    place(buf, 0.4 * click(0.03, 800, 6000, 0.003), 0.0)
    return fade(buf, 0.001, 0.02)


def teleport_whoosh():
    n = len(tt(0.3))
    x = svf(noise(0.3), np.linspace(4000, 500, n), 1.5)
    x /= np.max(np.abs(x))
    t = tt(0.3)
    tone = 0.25 * np.sin(phase_of(1400 * np.exp(-t / 0.12) + 200))
    env = np.sin(np.pi * np.linspace(0, 1, n)) ** 1.2
    return (x + tone) * env


def door_slide():
    buf = silence(1.0)
    hiss = fft_band(noise(0.25), 3000, 9000) * np.exp(-tt(0.25) / 0.1)
    place(buf, 0.5 * hiss, 0.0)
    n = len(tt(0.75))
    rum = svf(noise(0.75), 250, 0.8, "lp") + 0.3 * svf(noise(0.75), 900, 3.0)
    rum *= np.sin(np.pi * np.linspace(0, 1, n)) ** 0.7
    place(buf, 0.6 * rum / np.max(np.abs(rum)), 0.08)
    place(buf, 0.9 * thump(0.15, 150, 70, 0.04), 0.84)
    place(buf, 0.3 * click(0.03, 500, 4000, 0.004), 0.84)
    return fade(buf, 0.003, 0.02)


def timer_tick():
    t = tt(0.05)
    return fade(np.sin(2 * np.pi * 2000 * t) * decay(len(t), 0.01, 0.001), 0.0005, 0.01)


def drawer_slide():
    buf = silence(0.5)
    n = len(tt(0.42))
    t = tt(0.42)
    roll = svf(noise(0.42), 500, 1.5) * (0.7 + 0.3 * np.sin(2 * np.pi * 37 * t))
    roll *= np.sin(np.pi * np.linspace(0, 1, n)) ** 0.5
    place(buf, 0.6 * roll / np.max(np.abs(roll)), 0.0)
    place(buf, thump(0.08, 180, 90, 0.025), 0.42)
    place(buf, 0.4 * click(0.03, 600, 4000, 0.004), 0.42)
    return fade(buf, 0.005, 0.01)


def hint_chime():
    buf = 0.6 * bell(0.4, 1046.5, 0.15, 0.015)
    place(buf, 0.4 * bell(0.32, 1567.98, 0.12, 0.015), 0.08)
    return fade(buf, 0.001, 0.05)


def heartbeat_loop():
    buf = silence(1.0)
    place(buf, thump(0.25, 90, 45, 0.07), 0.0)
    place(buf, 0.7 * thump(0.25, 80, 42, 0.06), 0.28)
    return fade(buf, 0.0, 0.05)


LOOPS = {
    "alarm_whoop": alarm_whoop, "room_hum_loop": room_hum_loop,
    "vent_wind_loop": vent_wind_loop, "laser_hum_loop": laser_hum_loop,
    "heartbeat_loop": heartbeat_loop,
}
SHOTS = {
    "spark": spark, "fuse_insert": fuse_insert, "lever_clunk": lever_clunk,
    "power_up": power_up, "power_down": power_down, "dial_tick": dial_tick,
    "dial_catch": dial_catch, "safe_open": safe_open, "clock_tick": clock_tick,
    "clock_open": clock_open, "laser_hit": laser_hit, "mirror_step": mirror_step,
    "shutter_dissolve": shutter_dissolve, "card_insert": card_insert,
    "reboot_charge": reboot_charge, "win_stinger": win_stinger,
    "lose_stinger": lose_stinger, "beep_ok": beep_ok, "beep_error": beep_error,
    "chip_pickup": chip_pickup, "ui_click": ui_click, "grab": grab,
    "release": release, "climb_grip": climb_grip,
    "teleport_whoosh": teleport_whoosh, "door_slide": door_slide,
    "timer_tick": timer_tick, "drawer_slide": drawer_slide,
    "hint_chime": hint_chime,
}


def write_wav(name, x, peak_db):
    x = np.nan_to_num(x)
    x = x - np.mean(x) if name in LOOPS else x
    x = x / (np.max(np.abs(x)) + 1e-12) * 10 ** (peak_db / 20)
    pcm = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    return len(pcm) / SR


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in LOOPS.items():
        print(f"{name}: {write_wav(name, fn(), -12.0):.3f} s loop")
    for name, fn in SHOTS.items():
        print(f"{name}: {write_wav(name, fn(), -3.0):.3f} s")


if __name__ == "__main__":
    main()
