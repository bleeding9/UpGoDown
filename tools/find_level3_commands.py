"""BFS: winning commands for level 3 (seed, enemy, HP) — mirrors GameEngine + LevelScenarioService."""
from collections import deque
import json
import random
from pathlib import Path

W, H = 14, 8
DELTA = {0: (1, 0), 90: (0, 1), 180: (-1, 0), 270: (0, -1)}
ANGLES = [0, 90, 180, 270]


def norm(a):
    return a % 360


def delta(angle):
    return DELTA[norm(angle)]


def diagonal_delta(angle):
    fx, fy = delta(angle)
    px, py = delta(norm(angle + 90))
    return fx + px, fy + py


def compare_cell(a, b):
    return a[0] - b[0] if a[0] != b[0] else a[1] - b[1]


def nearest_chair(pos, a, b):
    def dist(c):
        return abs(c[0] - pos[0]) + abs(c[1] - pos[1])

    da, db = dist(a), dist(b)
    if da < db:
        return a, b
    if db < da:
        return b, a
    return (a, b) if compare_cell(a, b) <= 0 else (b, a)


def build_level3(seed: int):
    rng = random.Random(seed)
    cells = [(x, y) for x in range(W) for y in range(H)]
    rng.shuffle(cells)
    spawn = cells[0]
    c1, c2 = cells[1], cells[2]
    enemy = cells[3]
    own, partner = nearest_chair(spawn, c1, c2)
    angle = rng.choice(ANGLES)
    return spawn, own, partner, enemy, angle


def mark_lines(x, y, partner, lines):
    lines = set(lines)
    px, py = partner
    if x == px and y == py + 1:
        lines.add("y")
    if x == px and y == py - 1:
        lines.add("o")
    if x == px + 1 and y == py:
        lines.add("g")
    return frozenset(lines)


def step_pos(x, y, angle, diagonal=False):
    dx, dy = diagonal_delta(angle) if diagonal else delta(angle)
    nx, ny = x + dx, y + dy
    if nx < 0 or nx >= W or ny < 0 or ny >= H:
        return None
    return nx, ny


def move_enemy(ex, ey, px, py):
    prev = (ex, ey)
    best = prev
    best_dist = abs(ex - px) + abs(ey - py)
    for nx, ny in ((ex + 1, ey), (ex - 1, ey), (ex, ey + 1), (ex, ey - 1)):
        if nx < 0 or nx >= W or ny < 0 or ny >= H:
            continue
        d = abs(nx - px) + abs(ny - py)
        if d < best_dist or (d == best_dist and compare_cell((nx, ny), best) < 0):
            best_dist = d
            best = (nx, ny)
    ex, ey = best
    hp_loss = 1 if (ex, ey) == (px, py) and prev != (ex, ey) else 0
    return ex, ey, hp_loss


def apply(cmd, state, own, partner, diagonal_skill):
    px, py, angle, sitting, stood, lines, ex, ey, hp = state

    if cmd == "встать":
        if not sitting:
            return None
        n = step_pos(px, py, angle)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        return nx, ny, angle, False, True, lines, ex, ey, hp

    if cmd == "идти":
        if sitting:
            return None
        n = step_pos(px, py, angle)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        return nx, ny, angle, False, stood, lines, ex, ey, hp

    if cmd == "идти_диагональ":
        if sitting or not diagonal_skill:
            return None
        n = step_pos(px, py, angle, diagonal=True)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        return nx, ny, angle, False, stood, lines, ex, ey, hp

    if cmd == "повернуть_90":
        if sitting:
            return None
        return px, py, norm(angle + 90), False, stood, lines, ex, ey, hp

    if cmd == "повернуть_-90":
        if sitting:
            return None
        return px, py, norm(angle - 90), False, stood, lines, ex, ey, hp

    if cmd == "повернуть_180":
        if sitting:
            return None
        return px, py, norm(angle + 180), False, stood, lines, ex, ey, hp

    if cmd == "сесть":
        if sitting or (px, py) != own:
            return None
        return px, py, angle, True, stood, lines, ex, ey, hp

    return None


def after_enemy(state):
    px, py, angle, sitting, stood, lines, ex, ey, hp = state
    ex, ey, loss = move_enemy(ex, ey, px, py)
    hp -= loss
    return px, py, angle, sitting, stood, lines, ex, ey, hp


def goal(state, own):
    px, py, _, sitting, stood, lines, _, _, hp = state
    return (
        sitting
        and stood
        and (px, py) == own
        and lines >= frozenset({"y", "o", "g"})
        and hp > 0
    )


def commands_list(diagonal_skill):
    base = ["встать", "идти", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"]
    if diagonal_skill:
        return ["встать", "идти", "идти_диагональ", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"]
    return base


def bfs(seed: int, diagonal_skill=True, max_len=120):
    spawn, own, partner, enemy, angle = build_level3(seed)
    start = (spawn[0], spawn[1], angle, False, False, frozenset(), enemy[0], enemy[1], 3)
    cmds = commands_list(diagonal_skill)
    q = deque([(start, [])])
    seen = {start}

    while q:
        state, path = q.popleft()
        if goal(state, own):
            return path, spawn, own, partner, enemy, angle
        if len(path) >= max_len:
            continue
        for cmd in cmds:
            nxt = apply(cmd, state, own, partner, diagonal_skill)
            if nxt is None:
                continue
            nxt = after_enemy(nxt)
            if nxt[8] <= 0:
                continue
            if nxt in seen:
                continue
            seen.add(nxt)
            q.append((nxt, path + [cmd]))
    return None, spawn, own, partner, enemy, angle


if __name__ == "__main__":
    for seed in [42, 100, 1, 7, 123, 200]:
        path, spawn, own, partner, enemy, angle = bfs(seed, diagonal_skill=True, max_len=150)
        if path:
            print(f"seed={seed} FOUND {len(path)} cmds")
            print(f"  spawn={spawn} own={own} partner={partner} enemy={enemy} angle={angle}")
            if seed == 42:
                p = Path(__file__).resolve().parent.parent / "docs" / "demo-level3-try-seed42.json"
                p.write_text(
                    json.dumps({"seed": seed, "commands": path}, ensure_ascii=False, indent=2),
                    encoding="utf-8",
                )
                print(f"  saved {p}")
            break
    else:
        print("NOT FOUND for tested seeds")
