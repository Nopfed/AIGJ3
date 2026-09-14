import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from promo import *

# Every scene takes a time `t` in seconds (0 = the still promo image) so trailer.py can animate it:
# fire and bubbles cycle, ghosts bob, clouds drift, rain falls, the cat fidgets. `title=False` leaves the
# baked-in SPICE WIZARD wordmark off so the trailer can show it once, on the poster.

def jars(cv, x, y, tints=((217,65,47), (242,201,76), (232,118,43), (220,239,255))):
    cv.sprite("shelf", x, y)
    for i, t in enumerate(tints):
        cv.sprite("jar_fill", x + 2 + i * 10, y + 2, tint=t); cv.sprite("jar", x + 2 + i * 10, y + 2)

def shade_sprite(cv, name, x, y, col, a, **kw):
    cv.sprite(name, x, y, **kw)
    rows = SPR[name]
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch != '.': cv.put(x + i, y + j, col + (int(a * 255),))

def frame(t, period, n):
    """Which of n animation frames is showing at time t for a cycle of `period` seconds."""
    return int(t / period * n) % n

def bob(t, amp=1, period=1.6, phase=0.0):
    return int(round(amp * math.sin((t / period + phase) * 2 * math.pi)))

def fire(cv, x, y, t): cv.sprite("fire%d" % frame(t, 0.3, 2), x, y)
def bubbles(cv, x, y, t): cv.sprite("bubbles%d" % frame(t, 0.9, 3), x, y)

def sparkle(cv, x, y, t, col=(252,234,160), phase=0.0):
    """A four-point glint that swells and fades."""
    a = max(0.0, math.sin((t / 1.4 + phase) * 2 * math.pi))
    if a < 0.05: return
    cv.put(x, y, col + (int(240 * a),))
    for dx, dy in ((-1,0),(1,0),(0,-1),(0,1)): cv.put(x+dx, y+dy, col + (int(90 * a),))

def water_drops(cv, x, y, t):
    """Droplets arcing from the watering can spout at (x, y)."""
    for i in range(4):
        f = ((t * 1.6) + i * 0.25) % 1.0
        dx = int(f * 9); dy = int(-4 * f + 10 * f * f)
        cv.put(x + dx, y + dy, (127,184,230, 220)); cv.put(x + dx, y + dy + 1, (59,111,182, 140))

def cat_idle(cv, x, y, t):
    """The cat sits, has a scratch, sits again — sprites all share a baseline at y+12."""
    c = t % 6.0
    if c < 3.6: cv.sprite("cat_sit", x, y)
    else: cv.sprite("cat_scratch%d" % frame(t, 0.36, 2), x - 1, y - 1)

def v1_golden_farm(t=0.0, title=True, save=True):
    """Sunny farmyard: four peppers in fruit, the wizard watering, the tower and its jars."""
    cv = Canvas()
    cv.vgrad(0, 0, W, HORIZON, (86,150,215), (176,214,242))
    cv.rect(24, 10, 14, 8, (252,234,160,46)); cv.rect(27, 7, 8, 14, (252,234,160,46)); cv.sprite("sun", 26, 9)
    for (x, y, k, a) in ((60, 6, "cloud", 0.8), (150, 2, "cloud_big", 0.6), (250, 8, "cloud", 0.85), (110, 22, "cloud", 0.6)):
        cx = (x + int(t * (1.5 if k == "cloud" else 1.0)) + 40) % (W + 80) - 40
        cv.sprite(k, cx, y, alpha=a)
    hills(cv, rgb('F'), lerp(rgb('F'), rgb('W'), 0.25), rgb('H'), lerp(rgb('H'), rgb('u'), 0.5))
    treeline(cv, rgb('D'), rgb('K'))
    meadow(cv, n=700)
    # dirt road wandering in from the left edge
    for x in range(0, 120):
        yc = 118 + int(6 * math.sin(x * 0.03))
        cv.rect(x, yc - 3, 1, 6, rgb('h'))
        if x % 6 == 0: cv.rect(x, yc - 4, 1, 1, rgb('N'))
    tower(cv, 268, 44, h=70)
    jars(cv, 270, 72)
    cv.sprite("tree_big", 302, 34); cv.sprite("tree", 0, 62); cv.sprite("bush", 300, 118); cv.sprite("bush", 20, 96)
    fence_run(cv, 100, 66, 12)
    cv.sprite("well", 72, 70); cv.sprite("bucket", 94, 90)
    plot_row(cv, 116, 96, ["bell", "banana", "bonnet"], wet=True, gap=30)
    cv.sprite("plot_wet", 206, 96); cv.sprite("mature_ghost", 210, 88); cv.sprite("ghost", 213, 80 + bob(t, 2, 2.0))
    cv.sprite("wizard_water", 232, 88 + (1 if frame(t, 1.2, 2) else 0))
    water_drops(cv, 231, 96, t)
    cat_idle(cv, 150, 120, t)
    cv.sprite("cauldron", 236, 112); bubbles(cv, 236, 110, t); fire(cv, 234, 128, t)
    cv.sprite("crate_full", 130, 126); cv.sprite("bottle", 150, 130)
    cv.sprite("stump", 36, 104); cv.sprite("mortar", 38, 94)
    cv.sprite("board", 16, 70)
    scatter(cv, ["flower_pink", "flower_yellow", "flower_white"], 0, W, 70, 140, 26, 21)
    scatter(cv, ["rock"], 0, W, 76, 140, 4, 31)
    # a couple of butterflies over the flowers
    for i, (bx, by, col) in enumerate(((60, 100, (242,201,76)), (200, 130, (229,138,180)))):
        fx = bx + int(6 * math.sin(t * 0.9 + i)); fy = by + int(3 * math.sin(t * 2.3 + i * 2))
        op = frame(t + i * 0.1, 0.24, 2)
        cv.put(fx, fy, rgb('K')); cv.put(fx - 1, fy - op, col); cv.put(fx + 1, fy - op, col)
    if title: centered_title(cv, "SPICE WIZARD", 6, 2, shadow=(46,31,77))
    if save: cv.save("spice-wizard-promo-1-golden-farm")
    return cv

def v2_night_cauldron(t=0.0, title=True, save=True):
    """Midnight brew: the wizard stirs by firelight while ghost peppers drift up out of the pot."""
    cv = Canvas()
    cv.vgrad(0, 0, W, HORIZON, (18,20,52), (46,31,77))
    stars(cv, 170)
    for i in range(12): sparkle(cv, hsh(i, 91) % W, hsh(i, 92) % 36, t, (247,243,232), i * 0.37)
    cv.sprite("moon", 276, 8)
    n = rgb('n')
    hills(cv, lerp(rgb('F'), n, 0.7), lerp(rgb('F'), n, 0.55), lerp(rgb('H'), n, 0.7), lerp(rgb('H'), n, 0.55))
    treeline(cv, lerp(rgb('D'), n, 0.7), lerp(rgb('K'), n, 0.5))
    meadow(cv, base=lerp(rgb('a'), n, 0.7), dark=lerp(rgb('A'), n, 0.7), light=lerp(rgb('u'), n, 0.7), n=500)
    tower(cv, 26, 52, h=62, lit=True, shade=(n, 0.5))
    shade_sprite(cv, "tree_big", 292, 40, n, 0.45); shade_sprite(cv, "tree", 0, 100, n, 0.45)
    fence_run(cv, 200, 76, 9)
    plot_row(cv, 214, 96, ["ghost", "ghost", "ghost"], gap=32)
    for i in range(3): cv.rect(214 + i * 32, 96, 24, 16, n + (90,))
    for i in range(3): cv.sprite("ghost", 221 + i * 32, 80 + bob(t, 1, 1.8, i * 0.3))
    # the fire lights the ground and the wizard from below, breathing with the flames
    fl = 0.9 + 0.1 * math.sin(t * 9.0) + 0.05 * math.sin(t * 23.0)
    cv.glow(150, 128, int(96 * fl), int(34 * fl), (255,150,60), 0.5, 10)
    cv.glow(150, 122, int(50 * fl), int(22 * fl), (255,190,90), 0.35, 6)
    cv.sprite("cauldron", 138, 104); bubbles(cv, 138, 102, t); fire(cv, 136, 120, t)
    cv.sprite("wizard_stir%d" % frame(t, 1.0, 2), 166, 96)
    cv.sprite("cat_loaf", 108, 130)
    # ghosts rising out of the pot on a loop: each climbs, sways and fades
    for i in range(4):
        f = ((t / 5.0) + i * 0.25) % 1.0
        gy = 90 - int(f * 56); gx = 146 + int(10 * math.sin(f * 6.0 + i))
        a = 1.0 if f < 0.5 else max(0.0, 1.0 - (f - 0.5) * 2)
        if f > 0.06: cv.sprite("ghost", gx, gy, alpha=a * min(1.0, (f - 0.06) * 8), flip=i % 2 == 1)
    # embers drifting up from the fire
    for i in range(18):
        f = ((t / 3.2) + hsh(i, 43) % 100 / 100.0) % 1.0
        x = 138 + hsh(i, 41) % 30 + int(4 * math.sin(f * 9 + i)); y = 124 - int(f * 60)
        a = int(210 * (1 - f))
        cv.put(x, y, (252,234,160, a)); cv.put(x+1, y, (252,234,160, a // 3)); cv.put(x, y+1, (232,118,43, a // 3))
    shade_sprite(cv, "bush", 250, 124, n, 0.4); shade_sprite(cv, "bush", 90, 84, n, 0.4)
    vignette(cv, (10,8,20), 0.5)
    if title: centered_title(cv, "SPICE WIZARD", 5, 2, fill=FIRE_FILL, shadow=(46,31,77))
    if save: cv.save("spice-wizard-promo-2-midnight-brew")
    return cv

def v3_poster(t=0.0, title=True, save=True):
    """Poster: deep purple field, stacked two-line title, the whole cast lined up underneath."""
    cv = Canvas((46,31,77))
    pulse = 1.0 + 0.04 * math.sin(t * 1.5)
    cv.glow(W//2, 100, int(170 * pulse), int(70 * pulse), (90,61,138), 0.9, 8)
    cv.glow(W//2, 110, int(90 * pulse), int(40 * pulse), (155,123,209), 0.3, 5)
    for i in range(70):
        x = hsh(i, 61) % W; y = hsh(i, 62) % H
        k = hsh(i, 63) % 5
        if k == 0 and (y < 84 and (x < 40 or x > 280) or y < 4): cv.sprite("ic_peppercorn", x, y, tint=(130,110,110), alpha=0.85, scale=2)
        elif k in (1, 2):
            tw = 0.6 + 0.4 * math.sin(t * 2.0 + i * 1.3)
            cv.put(x, y, (252,234,160,int(220 * tw))); cv.put(x-1,y,(252,234,160,int(90 * tw))); cv.put(x+1,y,(252,234,160,int(90 * tw))); cv.put(x,y-1,(252,234,160,int(90 * tw))); cv.put(x,y+1,(252,234,160,int(90 * tw)))
        else: cv.put(x, y, (190,165,230,150))
    for i, (sx, sy, sc) in enumerate(((26, 14, 2), (278, 40, 2), (12, 60, 1), (296, 10, 1), (60, 82, 1))):
        cv.sprite("ic_star", sx, sy + bob(t, 1, 2.2, i * 0.2), tint=(242,201,76), scale=sc)
    cv.sprite("ic_flame", 4, 104 + (frame(t, 0.4, 2)), tint=(232,118,43), scale=2); cv.sprite("ic_hot", 300, 40, tint=(217,65,47), scale=2)
    # the cast on one baseline
    cv.sprite("mature_bell", 30, 100, scale=2); cv.sprite("mature_banana", 76, 100, scale=2)
    if (t % 5.0) < 3.4: cv.sprite("cat_sit", 118, 110, scale=2)
    else: cv.sprite("cat_scratch%d" % frame(t, 0.36, 2), 116, 108, scale=2)
    cv.sprite("wizard_cheer", 146, 92 - 2 * frame(t, 0.8, 2), scale=2)
    cv.sprite("cauldron", 194, 90, scale=2); cv.sprite("bubbles%d" % frame(t, 0.9, 3), 194, 86, scale=2); cv.sprite("fire%d" % frame(t, 0.3, 2), 190, 122, scale=2)
    cv.sprite("mature_bonnet", 246, 100, scale=2)
    cv.sprite("mature_ghost", 284, 100, scale=2); cv.sprite("ghost", 290, 84 + bob(t, 2, 2.0), scale=2)
    for i in range(6): sparkle(cv, 40 + hsh(i, 95) % 240, 86 + hsh(i, 96) % 50, t, (255,252,230), i * 0.29)
    if title:
        centered_title(cv, "SPICE", 4, 3, shadow=(26,20,35))
        centered_title(cv, "WIZARD", 40, 3, shadow=(26,20,35))
    if save: cv.save("spice-wizard-promo-3-poster")
    return cv

def v4_dusk_market(t=0.0, title=True, save=True):
    """Sunset delivery: the merchant's cart, townsfolk with stars, crates of sauce heading to town."""
    cv = Canvas()
    cv.vgrad(0, 0, W, 26, (90,61,138), (184,96,140), 8)
    cv.vgrad(0, 26, W, 20, (184,96,140), (232,118,43), 8)
    cv.vgrad(0, 46, W, HORIZON - 46, (232,118,43), (252,234,160), 6)
    cv.rect(194, 26, 22, 8, (255,240,180,70)); cv.rect(201, 20, 8, 20, (255,240,180,50)); cv.sprite("sun", 200, 24)
    stars(cv, 30, 0, 22, 0.6)
    for i in range(5): sparkle(cv, hsh(i, 97) % W, hsh(i, 98) % 18, t, (247,243,232), i * 0.41)
    for (x, y, k, a) in ((40, 12, "cloud_big", 0.6), (210, 6, "cloud", 0.7), (270, 20, "cloud_big", 0.55)):
        cx = (x + int(t * 0.8) + 40) % (W + 80) - 40
        cv.sprite(k, cx, y, alpha=a); cv.rect(cx, y, cv.size(k)[0], cv.size(k)[1], (232,118,43,70))
    hills(cv, lerp(rgb('F'), rgb('P'), 0.55), lerp(rgb('F'), rgb('i'), 0.4), lerp(rgb('H'), rgb('v'), 0.55), lerp(rgb('H'), rgb('o'), 0.35))
    for i, hx in enumerate((10, 30, 52, 76, 96)):
        hw = 12 + (i % 2) * 6
        cv.rect(hx, HORIZON - 9, hw, 9, lerp(rgb('M'), rgb('v'), 0.5))
        cv.rect(hx - 1, HORIZON - 13, hw + 2, 4, lerp(rgb('R'), rgb('v'), 0.5))
        cv.rect(hx + 3, HORIZON - 6, 2, 2, rgb('y')); cv.rect(hx + hw - 5, HORIZON - 6, 2, 2, rgb('Z'))
    treeline(cv, lerp(rgb('D'), rgb('v'), 0.5), lerp(rgb('K'), rgb('v'), 0.3))
    meadow(cv, base=lerp(rgb('a'), rgb('O'), 0.25), dark=lerp(rgb('A'), rgb('v'), 0.3), light=lerp(rgb('u'), rgb('o'), 0.3), n=600)
    for x in range(W):
        yc = 104 + int(4 * math.sin(x * 0.02 + 1))
        cv.rect(x, yc - 4, 1, 9, lerp(rgb('h'), rgb('o'), 0.2))
        if x % 9 < 3: cv.rect(x, yc - 1, 1, 1, lerp(rgb('B'), rgb('h'), 0.4)); cv.rect(x, yc + 3, 1, 1, lerp(rgb('B'), rgb('h'), 0.4))
    tower(cv, 262, 52, h=62, lit=True, shade=((176,79,26), 0.2))
    shade_sprite(cv, "tree_big", 226, 34, (90,61,138), 0.25); cv.sprite("bush", 296, 120); cv.sprite("tree", 300, 90)
    cv.sprite("cart", 60, 80); cv.sprite("merchant", 100, 86 + (frame(t, 1.4, 2)))
    cv.sprite("crate_full", 44, 106); cv.sprite("crate_full", 40, 96); cv.sprite("bottle", 66, 100); cv.sprite("bottle", 74, 99)
    cv.sprite("wizard_cheer", 140, 84 - 2 * frame(t, 0.8, 2))
    cv.sprite("townsfolk0", 14, 92 + frame(t + 0.3, 1.2, 2)); cv.sprite("townsfolk1", 176, 90 + frame(t, 1.0, 2), flip=True); cv.sprite("townsfolk2", 196, 96 + frame(t + 0.7, 1.3, 2))
    # the cat trots along the road and back
    cyc = (t / 7.0) % 1.0
    if cyc < 0.5: cx, flip = 96 + int(cyc * 2 * 60), False
    else: cx, flip = 156 - int((cyc - 0.5) * 2 * 60), True
    cv.sprite("cat_walk%d" % frame(t, 0.4, 2), cx, 116, flip=flip)
    for i in range(5):
        cv.sprite("ic_star", 148 + i * 9, 72 + bob(t, 1, 1.4, i * 0.15), tint=(242,201,76))
        sparkle(cv, 152 + i * 9, 70, t, (255,252,230), i * 0.2)
    cv.sprite("crate", 220, 124); cv.sprite("crate_full", 240, 122)
    scatter(cv, ["flower_yellow", "flower_pink"], 0, W, 112, 140, 16, 71)
    vignette(cv, (46,31,77), 0.3)
    if title: centered_title(cv, "SPICE WIZARD", 5, 2, shadow=(84,30,60))
    if save: cv.save("spice-wizard-promo-4-sunset-delivery")
    return cv

def v5_rainy_cottage(t=0.0, title=True, save=True):
    """Rainy day: lit windows, the wizard with a pipe under a little awning, jars fermenting on the shelf."""
    cv = Canvas()
    cv.vgrad(0, 0, W, HORIZON, (110,106,115), (184,180,174))
    for x in range(-8, W, 30):
        y = (hsh(x, 5) % 12) - 2
        cx = (x + int(t * 2.0) + 40) % (W + 80) - 40
        cv.sprite("cloud_big", cx, y, alpha=0.9); cv.rect(max(0, cx), max(0, y), 32, 12, (110,106,115,120))
    hills(cv, lerp(rgb('F'), rgb('G'), 0.5), lerp(rgb('F'), rgb('g'), 0.5), lerp(rgb('H'), rgb('G'), 0.4), lerp(rgb('H'), rgb('g'), 0.3))
    treeline(cv, lerp(rgb('D'), rgb('G'), 0.3), lerp(rgb('K'), rgb('G'), 0.2))
    meadow(cv, base=lerp(rgb('a'), rgb('G'), 0.25), dark=lerp(rgb('A'), rgb('G'), 0.25), light=lerp(rgb('u'), rgb('G'), 0.2), n=600)
    for (px, py, pw) in ((30, 126, 22), (100, 134, 16), (250, 130, 26), (210, 116, 12)):
        cv.rect(px, py, pw, 3, (176,214,242,110)); cv.rect(px + 2, py - 1, pw - 4, 1, (176,214,242,80)); cv.rect(px + 2, py + 3, pw - 4, 1, (110,106,115,90))
        # ripples where the drops land
        for r in range(2):
            f = ((t / 1.1) + r * 0.5 + px * 0.01) % 1.0
            rw = int(f * pw * 0.6)
            cv.rect(px + pw // 2 - rw // 2, py + 1, rw, 1, (230,240,250, int(160 * (1 - f))))
    tx = 136
    tower(cv, tx, 44, h=70, lit=True, shade=((110,106,115), 0.15))
    # a little purple awning off the front wall so the wizard can smoke his pipe in the dry
    ax, aw = tx + 6, 46
    cv.rect(ax, 90, 1, 24, rgb('d')); cv.rect(ax + aw - 1, 90, 1, 24, rgb('d'))
    cv.rect(ax - 1, 86, aw + 2, 1, rgb('K')); cv.rect(ax, 87, aw, 2, rgb('P')); cv.rect(ax, 89, aw, 1, rgb('v'))
    for i in range(0, aw, 4): cv.rect(ax + i, 87, 2, 2, rgb('p'))
    cv.rect(ax, 90, aw, 1, PAL['q'])
    cv.sprite("wizard_pipe", tx + 30, 96)
    for i in range(5):
        f = ((t / 2.4) + i * 0.2) % 1.0
        sx = tx + 44 + int(f * 6 + 1.5 * math.sin(f * 12 + i)); sy = 93 - int(f * 12)
        a = int(200 * (1 - f))
        cv.put(sx, sy, (230,216,189, a)); cv.put(sx + 1, sy, (230,216,189, a // 2))
    cv.sprite("cat_sleep", tx + 8, 106)
    zf = (t / 2.0) % 1.0
    if zf < 0.75:
        zx = tx + 20 + int(zf * 5); zy = 103 - int(zf * 8)
        for dx, dy in ((0,0),(1,0),(2,0),(1,1),(0,2),(1,2),(2,2)): cv.put(zx + dx, zy + dy, (247,243,232, int(220 * (1 - zf))))
    jars(cv, tx + 50, 66); cv.sprite("pantry", 236, 106)
    for i in range(4): sparkle(cv, tx + 56 + i * 10, 70, t, (252,234,160), i * 0.31)
    cv.sprite("stump", 100, 118); cv.sprite("mortar", 102, 108)
    cv.sprite("well", 60, 74); cv.sprite("bucket", 82, 94)
    fence_run(cv, 4, 66, 4); fence_run(cv, 252, 66, 5)
    plot_row(cv, 8, 96, ["bell", "banana"], wet=True); plot_row(cv, 40, 122, ["bonnet"], wet=True)
    cv.sprite("tree_big", 0, 20); cv.sprite("tree_big", 296, 30); cv.sprite("tree", 280, 100); cv.sprite("bush", 180, 130)
    scatter(cv, ["flower_white", "flower_pink"], 0, W, 76, 140, 10, 81)
    rain(cv, n=260, skip=(ax, 86, aw, 32), fall=int(t * 90))
    gl = 0.25 + 0.05 * math.sin(t * 3.0)
    cv.glow(tx + 14, 65, 16, 12, (255,190,90), gl, 3); cv.glow(tx + 34, 81, 16, 12, (255,190,90), gl, 3)
    vignette(cv, (46,42,42), 0.35)
    if title: centered_title(cv, "SPICE WIZARD", 10, 2, shadow=(84,51,28))
    if save: cv.save("spice-wizard-promo-5-rainy-day")
    return cv

SCENES = {"1": v1_golden_farm, "2": v2_night_cauldron, "3": v3_poster, "4": v4_dusk_market, "5": v5_rainy_cottage}

if __name__ == "__main__":
    which = sys.argv[1:] or ["1", "2", "3", "4", "5"]
    for w in which: SCENES[w]()
