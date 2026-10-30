"""Synthesizes every sound effect for Roll-and-Move Adventure (no samples used)."""
import sys, os
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, lfilter

OUT = sys.argv[1]
SR = 44100
rng = np.random.default_rng(3)

def t(dur):
    return np.arange(int(SR * dur)) / SR

def env(x, attack=0.005, decay=8.0):
    n = len(x)
    tt = np.arange(n) / SR
    a = np.clip(tt / attack, 0, 1)
    return x * a * np.exp(-tt * decay)

def bandpass(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], btype="band")
    return lfilter(b, a, x)

def highpass(x, f):
    b, a = butter(2, f / (SR / 2), btype="high")
    return lfilter(b, a, x)

def bell(freq, dur, decay=6.0):
    tt = t(dur)
    x = (np.sin(2 * np.pi * freq * tt) + 0.35 * np.sin(2 * np.pi * freq * 2.76 * tt) * np.exp(-tt * 10)
         + 0.2 * np.sin(2 * np.pi * freq * 5.4 * tt) * np.exp(-tt * 18))
    return env(x, 0.002, decay)

def pluck(freq, dur, decay=9.0):
    tt = t(dur)
    x = np.sign(np.sin(2 * np.pi * freq * tt)) * 0.3 + np.sin(2 * np.pi * freq * tt)
    x = lfilter(*butter(2, min(0.99, freq * 4 / (SR / 2))), x)
    return env(x, 0.002, decay)

def place(total, parts):
    out = np.zeros(int(SR * total))
    for start, sig in parts:
        i = int(start * SR)
        n = min(len(sig), len(out) - i)
        out[i:i + n] += sig[:n]
    return out

def save(name, x, peak=0.8):
    x = np.asarray(x, dtype=float)
    fade = min(len(x), int(0.01 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    m = np.max(np.abs(x)) or 1
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (x / m * peak * 32767).astype(np.int16))

def note(n):  # MIDI note to Hz
    return 440 * 2 ** ((n - 69) / 12)

# Dice rattling in a cup: many small clicks that thin out
parts = []
time = 0.0
while time < 0.65:
    click = env(bandpass(rng.standard_normal(int(SR * 0.03)), 1800, 5200), 0.0005, 160)
    click += 0.4 * bell(rng.uniform(900, 1500), 0.03, 120)
    parts.append((time, click * rng.uniform(0.5, 1.0)))
    time += rng.uniform(0.025, 0.07) * (1 + time * 1.5)
save("dice_rattle", place(0.8, parts), 0.6)

# Die landing on the board
tt = t(0.25)
thock = env(np.sin(2 * np.pi * 150 * tt) + 0.5 * np.sin(2 * np.pi * 310 * tt), 0.001, 22)
thock += env(bandpass(rng.standard_normal(len(tt)), 400, 2500), 0.0005, 60) * 0.6
save("dice_land", thock)

# Token step: short wooden tap (pitch is varied in code)
tt = t(0.12)
tap = env(np.sin(2 * np.pi * 720 * tt) + 0.4 * np.sin(2 * np.pi * 1450 * tt), 0.001, 45)
tap += env(bandpass(rng.standard_normal(len(tt)), 1500, 6000), 0.0005, 200) * 0.5
save("step", tap, 0.55)

# Coins
save("coin", place(0.6, [(0, bell(note(83), 0.5, 9)), (0.08, bell(note(88), 0.5, 7))]))
save("coin_spend", place(0.5, [(0, bell(note(88), 0.4, 10)), (0.07, bell(note(79), 0.4, 9))]), 0.7)

# Ladder climb: rising plucks
seq = [72, 76, 79, 84, 88]
save("ladder", place(0.9, [(i * 0.08, pluck(note(n), 0.35)) for i, n in enumerate(seq)]))

# Snake: hiss then a falling slide whistle
tt = t(0.45)
hiss = highpass(rng.standard_normal(len(tt)), 4000) * np.sin(np.pi * tt / 0.45) ** 0.7
tt2 = t(0.8)
f = 1100 * np.exp(-tt2 * 1.9) + 220
f *= 1 + 0.02 * np.sin(2 * np.pi * 7 * tt2)
whistle = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.clip(tt2 / 0.03, 0, 1) * np.exp(-tt2 * 1.2)
save("snake", place(1.3, [(0, hiss * 0.5), (0.4, whistle)]))

# Snake escape: whoosh up plus a happy chime
tt = t(0.4)
noise = rng.standard_normal(len(tt))
whoosh = np.zeros_like(noise)
for k in range(8):
    seg = slice(k * len(tt) // 8, (k + 1) * len(tt) // 8)
    lo = 500 + k * 400
    whoosh[seg] = bandpass(noise, lo, lo * 2)[seg]
whoosh *= np.sin(np.pi * tt / 0.4)
save("escape", place(0.9, [(0, whoosh * 0.5), (0.3, bell(note(84), 0.5, 7)), (0.38, bell(note(91), 0.5, 6))]))

# Card flip: short swish plus a paper tick
tt = t(0.2)
swish = bandpass(rng.standard_normal(len(tt)), 2000, 7000) * np.sin(np.pi * tt / 0.2) ** 2
tick = env(bandpass(rng.standard_normal(int(SR * 0.03)), 3000, 8000), 0.0005, 200)
save("card_flip", place(0.3, [(0, swish * 0.6), (0.17, tick)]), 0.6)

# UI click
tt = t(0.06)
save("click", env(np.sin(2 * np.pi * 1800 * tt) + 0.5 * np.sin(2 * np.pi * 2700 * tt), 0.0005, 90), 0.5)

# Good and bad event stingers
save("good", place(0.8, [(0, bell(note(84), 0.5)), (0.1, bell(note(88), 0.5)), (0.2, bell(note(91), 0.6, 4))]))
parts = []
for i, n in enumerate([67, 66, 65]):
    tt = t(0.32 if i < 2 else 0.7)
    f = note(n) * (1 - (0.03 * tt / tt[-1] if i == 2 else 0 * tt))
    x = lfilter(*butter(2, 1800 / (SR / 2)), np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)))
    x *= 1 + 0.15 * np.sin(2 * np.pi * 6 * tt)
    parts.append((i * 0.34, env(x, 0.02, 2.5 if i == 2 else 3)))
save("bad", place(1.5, parts), 0.42)

# Skip turn buzzer
tt = t(0.22)
buzz = lfilter(*butter(2, 1200 / (SR / 2)), np.sign(np.sin(2 * np.pi * 130 * tt))) * np.clip(tt / 0.01, 0, 1)
save("skip", place(0.55, [(0, buzz), (0.28, buzz)]), 0.3)

# Lucky die shimmer
parts = [(i * 0.045, bell(note(n), 0.3, 12) * 0.7) for i, n in enumerate([84, 86, 88, 91, 93, 96, 98, 100])]
save("lucky", place(0.8, parts), 0.6)

# Turn change chime (soft)
save("turn", place(0.6, [(0, bell(note(79), 0.4, 8)), (0.1, bell(note(86), 0.5, 7))]), 0.45)

# Victory fanfare
mel = [(0.00, 72, 0.14), (0.15, 76, 0.14), (0.30, 79, 0.14), (0.45, 84, 0.3), (0.80, 79, 0.14), (0.95, 84, 0.9)]
parts = []
for start, n, dur in mel:
    tt = t(dur + 0.3)
    x = np.sin(2 * np.pi * note(n) * tt) + 0.3 * np.sign(np.sin(2 * np.pi * note(n) * tt))
    x = lfilter(*butter(2, 3500 / (SR / 2)), x)
    parts.append((start, env(x, 0.01, 3.0 / (dur + 0.2))))
for n in (72, 76, 79):  # final chord under the last note
    parts.append((0.95, bell(note(n), 1.2, 2.5) * 0.5))
save("win", place(2.4, parts))

print("sfx done")
