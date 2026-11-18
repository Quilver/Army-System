"""Renders a battle replay video from the CSV files that LevelAIvsAITests.RecordReplay writes.

Top-down view of the battle map with every soldier drawn where the simulation put it, at 15 frames a second of battle
time. It is drawn from the simulation's own data, not by the game's renderer, so it shows positions, facings and
charges faithfully but not the game's sprites, borders or effects.

    python3 render_replay.py <folder with replay_*.csv> <map image> <out.mp4> [--title TEXT] [--speed 1.0]

The map image is the scene's map sprite (replay_map.csv gives its size and place); ffmpeg must be on the PATH.
"""
import argparse
import csv
import math
import os
import subprocess
from collections import defaultdict

from PIL import Image, ImageDraw, ImageFont

WIDTH, HEIGHT = 1600, 900
SUPERSAMPLE = 2
FPS = 15
MAP_PPU = 40                # map image resampled to this many pixels per world unit
MIN_VIEW_HEIGHT = 26.0      # the camera never shows less than this (world units, top to bottom)
VIEW_MARGIN = 5.0           # around the fighting troops
CAMERA_SECONDS = 1.5        # camera easing time

TEAM = {
    0: {"name": "South", "Spearman": (40, 70, 225), "Huskarl": (20, 40, 150), "Skirmisher": (110, 140, 240)},
    1: {"name": "North", "Spearman": (220, 40, 55), "Huskarl": (150, 20, 30), "Skirmisher": (240, 120, 120)},
}
SIZE = {"Spearman": 0.56, "Huskarl": 0.64, "Skirmisher": 0.5}


def font(size, bold=False):
    for path in ("/usr/share/fonts/opentype/inter/Inter-SemiBold.otf" if bold else "/usr/share/fonts/opentype/inter/Inter-Regular.otf",
                 "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"):
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def load(folder):
    troops = {}
    with open(os.path.join(folder, "replay_troops.csv"), newline="") as f:
        for row in csv.DictReader(f):
            troops[int(row["index"])] = (row["name"], int(row["team"]), row["kind"])
    soldiers = defaultdict(list)
    times = {}
    with open(os.path.join(folder, "replay_soldiers.csv"), newline="") as f:
        for row in csv.DictReader(f):
            frame = int(row["frame"])
            times[frame] = float(row["time"])
            soldiers[frame].append((int(row["troop"]), float(row["x"]), float(row["y"]), float(row["fx"]),
                                    float(row["fy"]), int(row["flags"])))
    states = defaultdict(dict)
    with open(os.path.join(folder, "replay_troop_states.csv"), newline="") as f:
        for row in csv.DictReader(f):
            states[int(row["frame"])][int(row["troop"])] = (float(row["x"]), float(row["y"]), int(row["routed"]),
                                                             int(row["engaged"]), int(row["alive"]))
    with open(os.path.join(folder, "replay_map.csv"), newline="") as f:
        sprites = [r for r in csv.DictReader(f) if r["enabled"] == "1"]
    return troops, soldiers, times, states, sprites


def world_map(map_path, sprite):
    """The map sprite resampled to MAP_PPU, with the world rectangle it covers (left, top, width, height)."""
    image = Image.open(map_path).convert("RGB")
    ppu = float(sprite["ppu"])
    sx, sy = float(sprite["scalex"]), float(sprite["scaley"])
    world_w = float(sprite["rw"]) / ppu * sx
    world_h = float(sprite["rh"]) / ppu * sy
    left = float(sprite["x"]) - float(sprite["pivotx"]) / ppu * sx
    top = float(sprite["y"]) - float(sprite["pivoty"]) / ppu * sy + world_h
    image = image.resize((round(world_w * MAP_PPU), round(world_h * MAP_PPU)), Image.LANCZOS)
    return image, (left, top, world_w, world_h)


class Camera:
    def __init__(self):
        self.cx = self.cy = self.h = None

    def follow(self, points, dt):
        xs = [p[0] for p in points]
        ys = [p[1] for p in points]
        cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
        h = max(MIN_VIEW_HEIGHT, max(ys) - min(ys) + 2 * VIEW_MARGIN,
                (max(xs) - min(xs) + 2 * VIEW_MARGIN) * HEIGHT / WIDTH)
        if self.h is None:
            self.cx, self.cy, self.h = cx, cy, h
            return
        k = 1 - math.exp(-dt / CAMERA_SECONDS)
        self.cx += (cx - self.cx) * k
        self.cy += (cy - self.cy) * k
        self.h += (h - self.h) * k

    @property
    def scale(self):
        return HEIGHT * SUPERSAMPLE / self.h

    def to_px(self, x, y):
        return ((x - self.cx) * self.scale + WIDTH * SUPERSAMPLE / 2,
                (self.cy - y) * self.scale + HEIGHT * SUPERSAMPLE / 2)

    def background(self, image, rect):
        """The part of the map in view, scaled to the output (dark beyond the map's edges)."""
        left, top, _, _ = rect
        view_w = self.h * WIDTH / HEIGHT
        x0 = (self.cx - view_w / 2 - left) * MAP_PPU
        y0 = (top - (self.cy + self.h / 2)) * MAP_PPU
        box = (round(x0), round(y0), round(x0 + view_w * MAP_PPU), round(y0 + self.h * MAP_PPU))
        canvas = Image.new("RGB", (box[2] - box[0], box[3] - box[1]), (34, 32, 28))
        canvas.paste(image.crop((max(0, box[0]), max(0, box[1]), min(image.width, box[2]), min(image.height, box[3]))),
                     (max(0, -box[0]), max(0, -box[1])))
        return canvas.resize((WIDTH * SUPERSAMPLE, HEIGHT * SUPERSAMPLE), Image.BILINEAR)


def view_points(frame_soldiers, frame_states):
    """Soldiers the camera keeps in view: those of troops that are not routed."""
    points = [(x, y) for troop, x, y, _, _, _ in frame_soldiers if not frame_states.get(troop, (0, 0, 0))[2]]
    return points or [(x, y) for _, x, y, _, _, _ in frame_soldiers] or [(0.0, 0.0)]


def square(cx, cy, fx, fy, half):
    rx, ry = fy, -fx  # right of facing
    return [(cx + (fx + rx) * half, cy - (fy + ry) * half), (cx + (fx - rx) * half, cy - (fy - ry) * half),
            (cx + (-fx - rx) * half, cy - (-fy - ry) * half), (cx + (-fx + rx) * half, cy - (-fy + ry) * half)]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder")
    parser.add_argument("map")
    parser.add_argument("out")
    parser.add_argument("--title", default="AI vs AI level")
    parser.add_argument("--speed", type=float, default=1.0, help="playback speed (2 = twice as fast)")
    parser.add_argument("--still", type=float, default=None, help="save one PNG at this battle time instead of a video")
    args = parser.parse_args()

    troops, soldiers, times, states, sprites = load(args.folder)
    map_image, map_rect = world_map(args.map, sprites[0])
    camera = Camera()
    title_font, info_font, small_font = font(30, True), font(26), font(20)
    start_alive = {0: 0, 1: 0}
    for t, (_, team, _) in troops.items():
        start_alive[team] += states[0].get(t, (0, 0, 0, 0, 0))[4]

    out_fps = FPS
    step = max(1, round(args.speed))
    ffmpeg = None if args.still is not None else subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s",
                               f"{WIDTH}x{HEIGHT}", "-r", str(out_fps), "-i", "-", "-c:v", "libx264", "-preset",
                               "veryfast", "-crf", "23", "-pix_fmt", "yuv420p", "-movflags", "+faststart", args.out],
                              stdin=subprocess.PIPE)
    frames = sorted(soldiers)
    if args.still is not None:
        # Run the camera up to the still, then draw only that frame.
        target = min(frames, key=lambda f: abs(times[f] - args.still))
        for f in frames:
            if f > target:
                break
            camera.follow(view_points(soldiers[f], states.get(f, {})), 1 / FPS)
        frames = [target]
    for frame in frames[::step]:
        frame_states = states.get(frame, {})
        if args.still is None:
            camera.follow(view_points(soldiers[frame], frame_states), step / FPS)
        scale = camera.scale
        to_px = camera.to_px
        img = camera.background(map_image, map_rect)
        draw = ImageDraw.Draw(img)
        for troop, x, y, fx, fy, flags in soldiers[frame]:
            name, team, kind = troops[troop]
            routed = frame_states.get(troop, (0, 0, 0, 0, 0))[2]
            colour = TEAM[team][kind]
            if routed:
                colour = tuple(int(c * 0.45 + 200 * 0.55) for c in colour)
            if flags & 4:
                colour = tuple(int(c * 0.5 + 128 * 0.5) for c in colour)
            n = math.hypot(fx, fy)
            fx, fy = (fx / n, fy / n) if n > 1e-4 else ((0.0, 1.0) if team == 0 else (0.0, -1.0))
            cx, cy = to_px(x, y)
            half = SIZE[kind] * 0.5 * scale
            outline = (255, 255, 255) if flags & 2 else ((255, 220, 60) if flags & 1 else (15, 15, 20))
            width = 3 if flags & 3 else 1
            if kind == "Skirmisher":
                draw.ellipse([cx - half, cy - half, cx + half, cy + half], fill=colour, outline=outline, width=width)
            else:
                draw.polygon(square(cx, cy, fx, fy, half), fill=colour, outline=outline, width=width)
            # facing tick
            draw.line([(cx, cy), (cx + fx * half * 1.25, cy - fy * half * 1.25)], fill=(245, 245, 245), width=3)
        img = img.resize((WIDTH, HEIGHT), Image.BOX)
        draw = ImageDraw.Draw(img)

        # Header: title, battle time, soldiers left per side.
        alive = {0: 0, 1: 0}
        for t, (_, team, _) in troops.items():
            alive[team] += frame_states.get(t, (0, 0, 0, 0, 0))[4]
        seconds = times[frame]
        draw.rectangle([0, 0, WIDTH, 52], fill=(18, 18, 22))
        draw.text((16, 10), args.title, font=title_font, fill=(240, 240, 240))
        info = (f"{int(seconds // 60)}:{int(seconds % 60):02d}    South {alive[0]}/{start_alive[0]} left    "
                f"North {alive[1]}/{start_alive[1]} left")
        tw = draw.textlength(info, font=info_font)
        draw.text((WIDTH - tw - 16, 12), info, font=info_font, fill=(240, 240, 240))
        legend = ("square: spearmen   dark square: huskarls   circle: skirmishers   yellow edge: charging   "
                  "white edge: clash   pale: routed")
        draw.rectangle([0, HEIGHT - 30, WIDTH, HEIGHT], fill=(18, 18, 22))
        draw.text((16, HEIGHT - 27), legend, font=small_font, fill=(200, 200, 200))
        if args.still is not None:
            img.save(args.out)
            return
        ffmpeg.stdin.write(img.tobytes())
    ffmpeg.stdin.close()
    ffmpeg.wait()


if __name__ == "__main__":
    main()
