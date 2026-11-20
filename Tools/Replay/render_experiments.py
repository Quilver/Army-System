"""Renders the engagement experiments that EngagementFootageTests.RecordThreeEngagements writes.

One panel per experiment, side by side, each with its own camera following its two troops. Drawn from the simulation's
own data (soldier places, facings and states; troop footprints, battle lines and states), not by the game's renderer.

    python3 render_experiments.py <engagement_footage folder> <out.mp4> [--speed 1.0] [--title TEXT]

ffmpeg must be on the PATH.
"""
import argparse
import csv
import math
import os
import subprocess
from collections import defaultdict, deque

from PIL import Image, ImageDraw, ImageFont

W, H = 1920, 1080
SS = 2                       # supersampling
FPS = 30
PANEL_W = 640
HEADER = 76
VIEW_TOP, VIEW_H = HEADER + 44, 686
STATUS_TOP = VIEW_TOP + VIEW_H
SOLDIER_R = 0.3
MIN_VIEW_W = 11.0            # world units across a panel at the closest zoom
MARGIN = 2.5
CAMERA_TAU = 0.9             # camera easing (s)
TRAIL = 12                   # frames of trail behind a charging soldier

BG = (226, 220, 204)
GRID = (212, 205, 186)
GRID5 = (196, 188, 166)
INK = (34, 34, 40)
MUTED = (110, 106, 98)
TEAM = {0: (46, 84, 214), 1: (204, 52, 62)}
TEAM_DARK = {0: (24, 46, 140), 1: (130, 24, 32)}
YELLOW = (255, 200, 0)
ORANGE = (255, 140, 30)
CYAN = (40, 190, 210)
TITLES = {
    1: ("1  Aligned", "Attacker (blue) attacks an idle troop square in front of it"),
    2: ("2  Angled", "Attacker starts 45° off the idle troop's front"),
    3: ("3  Mutual", "Both troops attack each other"),
}

FONT_DIRS = ["/usr/share/fonts/truetype/lato/", "/usr/share/fonts/truetype/dejavu/"]


def font(size, weight="Regular"):
    for d, name in ((FONT_DIRS[0], f"Lato-{weight}.ttf"), (FONT_DIRS[1], "DejaVuSans-Bold.ttf" if weight != "Regular" else "DejaVuSans.ttf")):
        p = os.path.join(d, name)
        if os.path.exists(p):
            return ImageFont.truetype(p, size * SS)
    return ImageFont.load_default()


def fnum(s):
    return float("nan") if s == "nan" else float(s)


def load(folder):
    troops = {}
    with open(os.path.join(folder, "troops.csv"), newline="") as f:
        for r in csv.DictReader(f):
            troops[int(r["index"])] = dict(name=r["name"], team=int(r["team"]), exp=int(r["experiment"]), role=r["role"],
                                           character=r["character"])
    soldiers = defaultdict(list)
    times = {}
    with open(os.path.join(folder, "soldiers.csv"), newline="") as f:
        for r in csv.DictReader(f):
            fr = int(r["frame"])
            times[fr] = float(r["time"])
            soldiers[fr].append((int(r["troop"]), int(r["soldier"]), float(r["x"]), float(r["y"]), float(r["fx"]),
                                 float(r["fy"]), int(r["flags"])))
    states = defaultdict(dict)
    with open(os.path.join(folder, "troop_states.csv"), newline="") as f:
        for r in csv.DictReader(f):
            states[int(r["frame"])][int(r["troop"])] = {k: (fnum(v) if k not in ("frame", "troop") else int(v)) for k, v in r.items()}
    return troops, soldiers, times, states


def camera_targets(troops, soldiers, frames, exp):
    aspect = VIEW_H / PANEL_W
    out = {}
    for fr in frames:
        xs = [s[2] for s in soldiers[fr] if troops[s[0]]["exp"] == exp]
        ys = [s[3] for s in soldiers[fr] if troops[s[0]]["exp"] == exp]
        cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
        w = max(MIN_VIEW_W, max(xs) - min(xs) + 2 * MARGIN, (max(ys) - min(ys) + 2 * MARGIN) / aspect)
        out[fr] = (cx, cy, w)
    # ease
    eased = {}
    prev = None
    a = 1 - math.exp(-1 / FPS / CAMERA_TAU)
    for fr in frames:
        t = out[fr]
        if prev is None:
            prev = t
        else:
            # zoom out at once if the troops would leave the view, otherwise ease
            w = t[2] if t[2] > prev[2] else prev[2] + (t[2] - prev[2]) * a
            prev = (prev[0] + (t[0] - prev[0]) * a * 2, prev[1] + (t[1] - prev[1]) * a * 2, w)
        eased[fr] = prev
    return eased


class View:
    def __init__(self, ox, cam):
        self.ox = ox
        self.cx, self.cy, self.w = cam
        self.ppu = PANEL_W / self.w

    def p(self, x, y):
        return ((self.ox + PANEL_W / 2 + (x - self.cx) * self.ppu) * SS, (VIEW_TOP + VIEW_H / 2 - (y - self.cy) * self.ppu) * SS)

    def r(self, d):
        return d * self.ppu * SS


def draw_grid(d, v):
    x0 = v.cx - v.w / 2
    x1 = v.cx + v.w / 2
    hh = v.w * VIEW_H / PANEL_W / 2
    y0, y1 = v.cy - hh, v.cy + hh
    for gx in range(math.floor(x0), math.ceil(x1) + 1):
        a, _ = v.p(gx, 0)
        d.line([(a, VIEW_TOP * SS), (a, (VIEW_TOP + VIEW_H) * SS)], fill=GRID5 if gx % 5 == 0 else GRID, width=SS)
    for gy in range(math.floor(y0), math.ceil(y1) + 1):
        _, b = v.p(0, gy)
        d.line([((v.ox) * SS, b), ((v.ox + PANEL_W) * SS, b)], fill=GRID5 if gy % 5 == 0 else GRID, width=SS)


def footprint_poly(st):
    kind = int(st["kind"])
    cx, cy = st["cx"], st["cy"]
    ax, ay = st["ax"], st["ay"]
    fx, fy = st["ffx"], st["ffy"]
    hl, hw, rr = st["hl"], st["hw"], st["rr"]
    if kind == 0:
        pts = []
        for sa, sf in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
            pts.append((cx + ax * hl * sa + fx * hw * sf, cy + ay * hl * sa + fy * hw * sf))
        return pts
    # circle or capsule: sample
    pts = []
    n = 40
    for i in range(n):
        ang = 2 * math.pi * i / n
        ux, uy = math.cos(ang), math.sin(ang)
        side = 1 if ux * ax + uy * ay >= 0 else -1
        bx = cx + ax * hl * side if kind == 2 else cx
        by = cy + ay * hl * side if kind == 2 else cy
        pts.append((bx + ux * rr, by + uy * rr))
    return pts


def ring(d, c, rad, color, width):
    d.ellipse([c[0] - rad, c[1] - rad, c[0] + rad, c[1] + rad], outline=color, width=width)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("out")
    ap.add_argument("--speed", type=float, default=1.0)
    ap.add_argument("--title", default="Engagement experiments")
    args = ap.parse_args()

    troops, soldiers, times, states = load(args.folder)
    frames = sorted(times)
    exps = sorted({t["exp"] for t in troops.values()})
    cams = {e: camera_targets(troops, soldiers, frames, e) for e in exps}

    # per-experiment contact time and per-frame charge counts (for the timeline)
    contact = {}
    counts = defaultdict(lambda: defaultdict(lambda: [0, 0, 0]))  # exp -> frame -> per team [assault, charge, feint] packed
    team_counts = defaultdict(dict)
    for fr in frames:
        for e in exps:
            team_counts[e][fr] = {0: [0, 0, 0], 1: [0, 0, 0]}
        for (ti, si, x, y, fx, fy, fl) in soldiers[fr]:
            e = troops[ti]["exp"]
            c = team_counts[e][fr][troops[ti]["team"]]
            if fl & 16: c[0] += 1
            elif fl & 1: c[1] += 1
            elif fl & 8: c[2] += 1
        for ti, st in states[fr].items():
            e = troops[ti]["exp"]
            if e not in contact and st["engaged"] == 1:
                contact[e] = times[fr]
    t_end = times[frames[-1]]

    title_f = font(26, "Bold")
    head_f = font(22, "Bold")
    sub_f = font(16)
    text_f = font(16)
    small_f = font(14)
    tiny_f = font(12)

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    out_frames = int(round(len(frames) / args.speed))
    ff = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}",
                           "-r", str(FPS), "-i", "-", "-c:v", "libx264", "-preset", "veryfast", "-crf", "20",
                           "-pix_fmt", "yuv420p", "-movflags", "+faststart", args.out], stdin=subprocess.PIPE)
    trails = defaultdict(lambda: deque(maxlen=TRAIL))
    last_fr = -1
    for of in range(out_frames):
        fr = frames[min(len(frames) - 1, int(of * args.speed))]
        time = times[fr]
        if fr != last_fr:
            for (ti, si, x, y, fx, fy, fl) in soldiers[fr]:
                key = (ti, si)
                if fl & (1 | 8 | 16 | 64):
                    trails[key].append((x, y, fl))
                elif trails[key]:
                    trails[key].popleft()
            last_fr = fr
        img = Image.new("RGB", (W * SS, H * SS), (246, 243, 236))
        d = ImageDraw.Draw(img)
        # global header
        d.text((24 * SS, 16 * SS), args.title, font=title_f, fill=INK)
        tt = f"t = {time:5.2f} s" + (f"   ({args.speed:g}× speed)" if args.speed != 1 else "")
        tw = d.textlength(tt, font=title_f)
        d.text((W * SS - tw - 24 * SS, 16 * SS), tt, font=title_f, fill=INK)
        for pi, e in enumerate(exps):
            ox = pi * PANEL_W
            v = View(ox, cams[e][fr])
            # panel header
            t1, t2 = TITLES.get(e, (f"Experiment {e}", ""))
            d.text(((ox + 18) * SS, (HEADER - 2) * SS), t1, font=head_f, fill=INK)
            d.text(((ox + 18) * SS, (HEADER + 22) * SS), t2, font=small_f, fill=MUTED)
            # view
            d.rectangle([ox * SS, VIEW_TOP * SS, (ox + PANEL_W) * SS, (VIEW_TOP + VIEW_H) * SS], fill=BG)
            draw_grid(d, v)
            # footprints and lines
            for ti, st in states[fr].items():
                if troops[ti]["exp"] != e:
                    continue
                team = troops[ti]["team"]
                poly = [v.p(x, y) for x, y in footprint_poly(st)]
                d.polygon(poly, outline=TEAM[team], width=SS)
                if not math.isnan(st["lsx"]):
                    d.line([v.p(st["lsx"], st["lsy"]), v.p(st["lex"], st["ley"])], fill=TEAM_DARK[team], width=3 * SS)
                px, py = v.p(st["px"], st["py"])
                k = 5 * SS
                d.line([(px - k, py), (px + k, py)], fill=TEAM_DARK[team], width=SS)
                d.line([(px, py - k), (px, py + k)], fill=TEAM_DARK[team], width=SS)
            # trails
            for (ti, si), tr in trails.items():
                if troops[ti]["exp"] != e or len(tr) < 2:
                    continue
                pts = [v.p(x, y) for x, y, _ in tr]
                fl = tr[-1][2]
                col = YELLOW if fl & 16 else (180, 180, 180) if fl & 8 else (255, 255, 255) if fl & 1 else (200, 190, 170)
                d.line(pts, fill=col, width=2 * SS)
            # soldiers
            rad = v.r(SOLDIER_R)
            for (ti, si, x, y, fx, fy, fl) in soldiers[fr]:
                if troops[ti]["exp"] != e:
                    continue
                team = troops[ti]["team"]
                c = v.p(x, y)
                if fl & 2:
                    g = rad * 1.9
                    d.ellipse([c[0] - g, c[1] - g, c[0] + g, c[1] + g], fill=ORANGE)
                fill = TEAM[team] if not fl & 4 else (90, 90, 90)
                d.ellipse([c[0] - rad, c[1] - rad, c[0] + rad, c[1] + rad], fill=fill)
                n = math.hypot(fx, fy) or 1
                d.line([c, (c[0] + fx / n * rad * 1.15, c[1] - fy / n * rad * 1.15)], fill=(255, 255, 255), width=max(SS, int(rad * 0.28)))
                if fl & 16:
                    ring(d, c, rad * 1.45, YELLOW, max(2 * SS, int(rad * 0.3)))
                elif fl & 1:
                    ring(d, c, rad * 1.45, (255, 255, 255), max(2 * SS, int(rad * 0.3)))
                elif fl & 8:
                    ring(d, c, rad * 1.4, (150, 150, 150), max(SS, int(rad * 0.2)))
                elif fl & 32:
                    ring(d, c, rad * 1.35, CYAN, max(SS, int(rad * 0.18)))
            # scale bar
            sx, sy = (ox + 18) * SS, (VIEW_TOP + VIEW_H - 22) * SS
            d.line([(sx, sy), (sx + v.r(2), sy)], fill=INK, width=2 * SS)
            d.text((sx + v.r(2) + 6 * SS, sy - 9 * SS), "2 units", font=tiny_f, fill=INK)
            # panel separator
            if pi > 0:
                d.line([(ox * SS, (HEADER - 4) * SS), (ox * SS, H * SS)], fill=(200, 194, 180), width=2 * SS)
            # status
            y = (STATUS_TOP + 12) * SS
            ce = contact.get(e)
            line = f"Contact at {ce:.2f} s" if ce is not None else "No contact yet"
            if ce is not None and time >= ce:
                line += f"   ({time - ce:+.2f} s)"
            d.text(((ox + 18) * SS, y), line, font=text_f, fill=INK if ce is not None and time >= ce else MUTED)
            y += 26 * SS
            for ti in sorted(k for k in troops if troops[k]["exp"] == e):
                st = states[fr].get(ti)
                if st is None:
                    continue
                team = troops[ti]["team"]
                d.rectangle([(ox + 18) * SS, y + 4 * SS, (ox + 30) * SS, y + 16 * SS], fill=TEAM[team])
                tags = []
                if st["routed"] == 1: tags.append("ROUTED")
                elif st["retreating"] == 1: tags.append("retreating")
                if st["dazed"] == 1: tags.append("dazed")
                if st["engaged"] == 1: tags.append("engaged")
                if st["assault"] == 1: tags.append("ASSAULT")
                if st["wheeling"] == 1: tags.append("wheeling")
                name = troops[ti]["name"] + ("" if troops[ti]["role"] == "attacker" else " (no orders)")
                d.text(((ox + 38) * SS, y), name, font=text_f, fill=INK)
                d.text(((ox + 38) * SS, y + 20 * SS), "  ·  ".join(tags) if tags else "—", font=small_f, fill=TEAM_DARK[team])
                stats = f"Assault charges {int(st['assaultcharges'])}   alive {int(st['alive'])}   shock {st['shock']:.0f}"
                if not math.isnan(st["gap"]):
                    stats += f"   gap {st['gap']:.2f}"
                d.text(((ox + 38) * SS, y + 38 * SS), stats, font=small_f, fill=MUTED)
                y += 62 * SS
            # timeline: soldiers charging per frame, blue above, red below
            tl_x0, tl_x1 = (ox + 18) * SS, (ox + PANEL_W - 18) * SS
            tl_mid = (H - 46) * SS
            scale = 2.2 * SS
            d.text((tl_x0, tl_mid - 40 * SS), "Soldiers charging (yellow: Assault charge)", font=tiny_f, fill=MUTED)
            d.line([(tl_x0, tl_mid), (tl_x1, tl_mid)], fill=(190, 184, 170), width=SS)
            for f2 in frames:
                if f2 > fr:
                    break
                xx = tl_x0 + (tl_x1 - tl_x0) * times[f2] / t_end
                for team, sgn in ((0, -1), (1, 1)):
                    a, c_, fe = team_counts[e][f2][team]
                    if a:
                        d.line([(xx, tl_mid), (xx, tl_mid + sgn * a * scale)], fill=YELLOW, width=SS)
                    if c_:
                        d.line([(xx, tl_mid + sgn * a * scale), (xx, tl_mid + sgn * (a + c_) * scale)], fill=TEAM[team], width=SS)
                    if fe:
                        d.line([(xx, tl_mid + sgn * (a + c_) * scale), (xx, tl_mid + sgn * (a + c_ + fe) * scale)], fill=(170, 170, 170), width=SS)
            if ce is not None:
                xx = tl_x0 + (tl_x1 - tl_x0) * ce / t_end
                d.line([(xx, tl_mid - 24 * SS), (xx, tl_mid + 24 * SS)], fill=MUTED, width=SS)
            xx = tl_x0 + (tl_x1 - tl_x0) * time / t_end
            d.line([(xx, tl_mid - 28 * SS), (xx, tl_mid + 28 * SS)], fill=INK, width=2 * SS)
        # legend
        lg = "Rings: yellow = Assault charge, white = ordinary charge, grey = feint, cyan = following.  Orange flash = clash.  Thick line = the troop's front rank (battle line), outline = footprint, cross = pivot."
        d.text((24 * SS, 50 * SS), lg, font=tiny_f, fill=MUTED)
        frame_img = img.reduce(SS)
        ff.stdin.write(frame_img.tobytes())
    ff.stdin.close()
    ff.wait()
    print(f"wrote {args.out}: {out_frames} frames")


if __name__ == "__main__":
    main()
