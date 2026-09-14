"""Assembles the promo trailer: animated versions of the painted promo scenes (scenes.py) cut with
recorded gameplay clips (record.js), captions in the game's own pixel font, and the game's own
synthesised music, ambience and effects (dumped to WAV by tools/audiodump).

    python promo/tools/trailer.py --work <dir> [--out promo/spice-wizard-trailer.mp4] [--preview]

<dir>/clips/<name>/f0000.png + times.json come from record.js; <dir>/audio/*.wav from audiodump.
Frames are piped raw into ffmpeg; the audio bed is one ffmpeg filter graph built from EVENTS below.
"""
import argparse, json, math, os, subprocess, sys
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
import promo
from promo import Canvas, W, H, SCALE, text5, text5_width
import scenes

FPS = 24
OUT_W, OUT_H = 1920, 1080
FRAME_W, FRAME_H = W * SCALE, H * SCALE          # 1920x864 letterboxed picture
BAR = (OUT_H - FRAME_H) // 2                     # 108px bars above and below
BG = (26, 20, 35)                                # palette K
XFADE = 0.6
ART_TICK = 1 / 12                                # the painted scenes animate at 12fps, like the game's sprites

# ---- Timeline ----------------------------------------------------------------------------------
# kind, source, duration, caption, extra. Art shots name a scene function and a camera move
# (zoom start, zoom end, centre x, centre y in scene pixels); clip shots name a recorded clip and where in it to start.
SHOTS = [
    ("art",  "v1_golden_farm",    5.0, "Grow four peculiar peppers",                 dict(zoom=(1.0, 1.10), at=(150, 90))),
    ("clip", "yard",              7.5, "Water them. Talk to them. Harvest them.",     dict(offset=0.6)),
    ("art",  "v2_night_cauldron", 4.0, "Brew by firelight",                          dict(zoom=(1.14, 1.0), at=(150, 100))),
    ("clip", "cook",              7.0, "Cook hot sauces and curries",                dict(offset=0.6)),
    ("clip", "blend",             6.8, "Mix blends the town has never tasted",       dict(offset=0.9)),
    ("art",  "v4_dusk_market",    4.0, "Ship them to town",                          dict(zoom=(1.08, 1.08), at=(120, 90), pan=(80, 0))),
    ("clip", "levelup",           6.8, "Earn stars, fame and peppercorns",           dict(offset=2.6)),
    ("clip", "rain",              3.4, "Rain or shine...",                           dict(offset=0.6)),
    ("art",  "v5_rainy_cottage",  3.6, "...the jars keep bubbling",                  dict(zoom=(1.0, 1.08), at=(170, 80))),
    ("art",  "v3_poster",         7.0, "Play free in your browser",                  dict(zoom=(1.0, 1.0), at=(160, 72), title=True)),
]
FADE_IN, FADE_OUT = 0.8, 1.6

def shot_starts():
    """Absolute start time of each shot: consecutive shots overlap by XFADE."""
    starts, t = [], 0.0
    for kind, src, dur, cap, extra in SHOTS:
        starts.append(t)
        t += dur - XFADE
    return starts, t + XFADE

# ---- Audio events -------------------------------------------------------------------------------
# (wav name, absolute start, gain, length or None for one-shot, fade in, fade out). Loops are cut to length.
def audio_events(starts, total):
    S = starts
    clips = {name: json.load(open(os.path.join(ARGS.work, "clips", name, "times.json"))) for name in ("yard", "cook", "blend", "levelup")}
    def click(clip, i): return S[[s[1] for s in SHOTS].index(clip)] + clips[clip]["clicks"][i]["t"] - SHOTS[[s[1] for s in SHOTS].index(clip)][4]["offset"]
    ev = [
        ("music_simmering_pot", 0.0, 0.55, total, 0.0, 3.0),
        ("amb_bird1", S[0] + 0.8, 0.5, None, 0, 0), ("amb_bird2", S[0] + 2.4, 0.4, None, 0, 0), ("amb_bird0", S[0] + 3.6, 0.4, None, 0, 0),
        ("amb_wind", S[0], 0.25, SHOTS[1][2] + SHOTS[0][2], 1.0, 1.0),
        ("sfx_step0", click("yard", 0) + 0.3, 0.5, None, 0, 0), ("sfx_step0", click("yard", 0) + 0.6, 0.5, None, 0, 0),
        ("sfx_harvest", click("yard", 1), 0.9, None, 0, 0), ("sfx_sparkle", click("yard", 1) + 0.1, 0.7, None, 0, 0),
        ("amb_crickets", S[2], 0.5, SHOTS[2][2], 0.6, 0.6), ("amb_cauldron", S[2], 0.7, SHOTS[2][2] + 1.0, 0.4, 0.8),
        ("sfx_bubble", S[2] + 1.2, 0.6, None, 0, 0), ("sfx_blorp0", S[2] + 2.5, 0.5, None, 0, 0),
        ("amb_cauldron", S[3] + 1.0, 0.4, SHOTS[3][2] - 1.0, 0.5, 0.5),
        ("sfx_cook", click("cook", 1), 1.0, None, 0, 0), ("sfx_sparkle", click("cook", 1) + 0.4, 0.8, None, 0, 0),
        ("sfx_bubble", click("cook", 2) + 0.4, 0.7, None, 0, 0),
        ("sfx_jar", click("blend", 0), 0.7, None, 0, 0), ("sfx_jar", click("blend", 1), 0.7, None, 0, 0),
        ("sfx_jar", click("blend", 2), 0.7, None, 0, 0), ("sfx_jar", click("blend", 3), 0.7, None, 0, 0),
        ("sfx_grind", click("blend", 4), 0.9, None, 0, 0), ("sfx_blend", click("blend", 4) + 0.5, 0.9, None, 0, 0),
        ("sfx_cart", S[5] + 0.3, 0.8, None, 0, 0), ("sfx_coin", S[5] + 2.2, 0.8, None, 0, 0), ("sfx_coin", S[5] + 2.6, 0.7, None, 0, 0),
        ("sfx_chime", S[6] + 1.6, 0.7, None, 0, 0), ("sfx_cart", S[6] + 1.7, 0.4, None, 0, 0),
        ("sfx_levelup", S[6] + 4.6, 1.0, None, 0, 0), ("sfx_coin", S[6] + 5.6, 0.7, None, 0, 0),
        ("amb_rain", S[7], 0.7, SHOTS[7][2] + SHOTS[8][2], 0.8, 1.0), ("sfx_purr", S[8] + 1.0, 0.5, None, 0, 0),
        ("sfx_fanfare", S[9] + 0.3, 0.9, None, 0, 0), ("sfx_cheer", S[9] + 0.6, 0.6, None, 0, 0),
    ]
    return ev

# ---- Picture ------------------------------------------------------------------------------------
_art_cache = {}
def art_frame(src, t_local, extra):
    """Animated scene at 12fps, upscaled 6x, then a slow camera move (crop + resample) for a little life."""
    tick = int(t_local / ART_TICK)
    key = (src, tick)
    if key not in _art_cache:
        if len(_art_cache) > 4: _art_cache.clear()
        cv = getattr(scenes, src)(t=tick * ART_TICK, title=extra.get("title", False), save=False)
        _art_cache[key] = cv.im.convert("RGB").resize((FRAME_W, FRAME_H), Image.NEAREST)
    big = _art_cache[key]
    z0, z1 = extra["zoom"]
    dur = next(s[2] for s in SHOTS if s[1] == src)
    f = min(1.0, t_local / dur)
    f = f * f * (3 - 2 * f)                                       # smoothstep
    z = z0 + (z1 - z0) * f
    cx, cy = extra["at"]
    if "pan" in extra: cx += extra["pan"][0] * (f - 0.5); cy += extra["pan"][1] * (f - 0.5)
    if abs(z - 1.0) < 1e-3 and "pan" not in extra: return big
    w, h = FRAME_W / z, FRAME_H / z
    x0 = min(max(0, cx * SCALE - w / 2), FRAME_W - w); y0 = min(max(0, cy * SCALE - h / 2), FRAME_H - h)
    return big.crop((int(x0), int(y0), int(x0 + w), int(y0 + h))).resize((FRAME_W, FRAME_H), Image.BILINEAR)

_clip_meta = {}
_clip_cache = {}
def clip_frame(name, t_local, extra):
    if name not in _clip_meta:
        _clip_meta[name] = json.load(open(os.path.join(ARGS.work, "clips", name, "times.json")))["times"]
    times = _clip_meta[name]
    want = extra.get("offset", 0.0) + t_local
    # nearest recorded frame at or before `want` (the screencast runs at a variable ~28fps)
    lo, hi = 0, len(times) - 1
    while lo < hi:
        mid = (lo + hi + 1) // 2
        if times[mid] <= want: lo = mid
        else: hi = mid - 1
    key = (name, lo)
    if key not in _clip_cache:
        if len(_clip_cache) > 8: _clip_cache.clear()
        im = Image.open(os.path.join(ARGS.work, "clips", name, "f%04d.png" % lo)).convert("RGB")
        if im.size != (FRAME_W, FRAME_H): im = im.resize((FRAME_W, FRAME_H), Image.NEAREST)
        _clip_cache[key] = im
    return _clip_cache[key]

_caption_cache = {}
def caption_strip(text):
    """The caption rendered once in the 5x7 font at 1px, scaled 6x like the picture."""
    if text not in _caption_cache:
        promo.W, promo.H = text5_width(text) + 4, 9
        cv = Canvas(BG)
        text5(cv, text, 1, 1, col=(247, 243, 232), shadow=(46, 31, 77))
        promo.W, promo.H = W, H
        _caption_cache[text] = cv.im.convert("RGB").resize((cv.im.width * SCALE, cv.im.height * SCALE), Image.NEAREST)
    return _caption_cache[text]

def shot_picture(i, t_local):
    kind, src, dur, cap, extra = SHOTS[i]
    pic = art_frame(src, t_local, extra) if kind == "art" else clip_frame(src, t_local, extra)
    frame = Image.new("RGB", (OUT_W, OUT_H), BG)
    frame.paste(pic, (0, BAR))
    if cap:
        # captions fade in after a beat and out before the shot hands over
        a = min(1.0, max(0.0, (t_local - 0.35) / 0.4)) * min(1.0, max(0.0, (dur - XFADE - t_local) / 0.4))
        if a > 0:
            strip = caption_strip(cap)
            x = (OUT_W - strip.width) // 2; y = OUT_H - BAR + (BAR - strip.height) // 2
            if a < 1: strip = Image.blend(Image.new("RGB", strip.size, BG), strip, a)
            frame.paste(strip, (x, y))
    return frame

def frame_at(t, starts):
    """Composite for absolute time t: the current shot, cross-faded with the next during the overlap."""
    cur = max(i for i, s in enumerate(starts) if s <= t + 1e-6)
    img = shot_picture(cur, t - starts[cur])
    if cur + 1 < len(SHOTS) and t >= starts[cur + 1]:
        f = (t - starts[cur + 1]) / XFADE
        img = Image.blend(img, shot_picture(cur + 1, t - starts[cur + 1]), min(1.0, f))
    return img

# ---- Assembly -----------------------------------------------------------------------------------
def build_audio(events, total, path):
    """One ffmpeg call: every event is an input, delayed/looped/faded, then mixed."""
    args = ["ffmpeg", "-y", "-loglevel", "error"]
    filters, labels = [], []
    for i, (name, start, gain, length, fi, fo) in enumerate(events):
        wav = os.path.join(ARGS.work, "audio", name + ".wav")
        args += ["-i", wav]
        chain = "[%d:a]" % i
        if length is not None:
            chain += "aloop=loop=-1:size=2147483647,atrim=0:%.3f," % length
            if fi > 0: chain += "afade=t=in:st=0:d=%.3f," % fi
            if fo > 0: chain += "afade=t=out:st=%.3f:d=%.3f," % (max(0, length - fo), fo)
        chain += "volume=%.3f,adelay=%d:all=1[a%d]" % (gain, int(start * 1000), i)
        filters.append(chain); labels.append("[a%d]" % i)
    filters.append("".join(labels) + "amix=inputs=%d:normalize=0:dropout_transition=0,atrim=0:%.3f,loudnorm=I=-16:TP=-1.5:LRA=11,aformat=sample_fmts=fltp:channel_layouts=stereo[out]" % (len(events), total))
    args += ["-filter_complex", ";".join(filters), "-map", "[out]", "-ar", "44100", path]
    subprocess.run(args, check=True)

def main():
    starts, total = shot_starts()
    n = int(round(total * FPS))
    audio = os.path.join(ARGS.work, "trailer-audio.wav")
    build_audio(audio_events(starts, total), total, audio)
    print("audio mixed; rendering %d frames (%.1fs)" % (n, total))
    if ARGS.preview:
        for t in ARGS.preview:
            frame_at(float(t), starts).save(os.path.join(ARGS.work, "preview-%05.1f.png" % float(t)))
        return
    cmd = ["ffmpeg", "-y", "-loglevel", "error", "-stats",
           "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (OUT_W, OUT_H), "-r", str(FPS), "-i", "-",
           "-i", audio, "-map", "0:v", "-map", "1:a",
           "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p", "-tune", "animation",
           "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-shortest", ARGS.out]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    for k in range(n):
        t = k / FPS
        img = frame_at(t, starts)
        # fade from and to black
        a = min(1.0, t / FADE_IN, max(0.0, (total - t) / FADE_OUT))
        if a < 1: img = Image.blend(Image.new("RGB", img.size, (0, 0, 0)), img, a)
        proc.stdin.write(img.tobytes())
        if k % (FPS * 5) == 0: print("  %5.1fs" % t, flush=True)
    proc.stdin.close()
    proc.wait()
    if proc.returncode: sys.exit("ffmpeg failed")
    print("wrote", ARGS.out)

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--work", required=True, help="folder holding clips/ and audio/")
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "spice-wizard-trailer.mp4"))
    ap.add_argument("--preview", nargs="*", help="only write still frames at these times (seconds)")
    ARGS = ap.parse_args()
    main()
