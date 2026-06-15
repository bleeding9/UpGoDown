"""Generate demo JSON bodies for levels 1-5 (matches C# Random + GameEngine)."""
import json
from collections import deque
from pathlib import Path

DOCS = Path(__file__).resolve().parent.parent / "docs"
DELTA = {0: (1, 0), 90: (0, 1), 180: (-1, 0), 270: (0, -1)}
ANGLES = [0, 90, 180, 270]
DEMO_SEED = 42


class DotNetRandom:
    MBIG = 2147483647
    MSEED = 161803398

    def __init__(self, seed: int):
        self.seed_array = [0] * 56
        self.inext = 0
        self.inextp = 21
        subtraction = self.MSEED - abs(seed)
        self.seed_array[55] = subtraction
        mj = 1 if subtraction >= 0 else -1
        for i in range(1, 55):
            ii = (21 * i) % 55
            self.seed_array[ii] = mj
            mj = subtraction - mj
            if mj < 0:
                mj += self.MBIG
            subtraction = self.seed_array[ii]
        for _ in range(1, 5):
            for i in range(1, 56):
                self.seed_array[i] -= self.seed_array[1 + (i + 30) % 55]
                if self.seed_array[i] < 0:
                    self.seed_array[i] += self.MBIG

    def _sample(self) -> float:
        loc_inext = self.inext + 1
        loc_inextp = self.inextp + 1
        if loc_inext >= 56:
            loc_inext = 1
        if loc_inextp >= 56:
            loc_inextp = 1
        ret_val = self.seed_array[loc_inext] - self.seed_array[loc_inextp]
        if ret_val == self.MBIG:
            ret_val -= 1
        if ret_val < 0:
            ret_val += self.MBIG
        self.seed_array[loc_inext] = ret_val
        self.inext = loc_inext
        self.inextp = loc_inextp
        return ret_val * (1.0 / self.MBIG)

    def next(self, max_value: int) -> int:
        return int(self._sample() * max_value)

    def next_range(self, min_val: int, max_val: int) -> int:
        return min_val + self.next(max_val - min_val)


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


def shuffle(cells, rng):
    for i in range(len(cells) - 1, 0, -1):
        j = rng.next(i + 1)
        cells[i], cells[j] = cells[j], cells[i]


def all_cells(w, h):
    return [(x, y) for x in range(w) for y in range(h)]


def random_chairs(w, h, rng):
    cells = all_cells(w, h)
    shuffle(cells, rng)
    return cells[0], cells[1]


def resolve_grid(rng):
    return rng.next_range(12, 17), rng.next_range(6, 11)


def angle_stand_from_chair(w, h, ax, ay, px, py):
    for ang in ANGLES:
        dx, dy = delta(ang)
        tx, ty = ax + dx, ay + dy
        if 0 <= tx < w and 0 <= ty < h and (tx, ty) != (ax, ay) and (tx, ty) != (px, py):
            return ang
    return 0


def nearest_chair(pos, a, b):
    def dist(c):
        return abs(c[0] - pos[0]) + abs(c[1] - pos[1])

    da, db = dist(a), dist(b)
    if da < db:
        return a, b
    if db < da:
        return b, a
    return (a, b) if compare_cell(a, b) <= 0 else (b, a)


def random_free_cell(w, h, occupied, rng):
    free = [(x, y) for x in range(w) for y in range(h) if (x, y) not in occupied]
    return free[rng.next(len(free))]


def build_level2(seed):
    rng = DotNetRandom(seed)
    w, h = 14, 8
    own, partner = random_chairs(w, h, rng)
    angle = angle_stand_from_chair(w, h, own[0], own[1], partner[0], partner[1])
    return w, h, own, partner, own, angle, True


def build_level3(seed):
    rng = DotNetRandom(seed)
    w, h = resolve_grid(rng)
    own, partner = random_chairs(w, h, rng)
    angle = angle_stand_from_chair(w, h, own[0], own[1], partner[0], partner[1])
    return w, h, own, partner, own, angle, True


def build_level4(seed):
    rng = DotNetRandom(seed)
    w, h = resolve_grid(rng)
    own, partner = random_chairs(w, h, rng)
    occupied = {own, partner}
    spawn = random_free_cell(w, h, occupied, rng)
    angle = ANGLES[rng.next(len(ANGLES))]
    return w, h, own, partner, spawn, angle, False


def build_level5(seed):
    rng = DotNetRandom(seed)
    w, h = resolve_grid(rng)
    cells = all_cells(w, h)
    shuffle(cells, rng)
    spawn = cells[0]
    c1, c2 = cells[1], cells[2]
    enemy = cells[3]
    own, partner = nearest_chair(spawn, c1, c2)
    angle = ANGLES[rng.next(len(ANGLES))]
    return w, h, own, partner, spawn, angle, enemy


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


def step_pos(x, y, angle, w, h, diagonal=False):
    dx, dy = diagonal_delta(angle) if diagonal else delta(angle)
    nx, ny = x + dx, y + dy
    if nx < 0 or nx >= w or ny < 0 or ny >= h:
        return None
    return nx, ny


def move_enemy(ex, ey, px, py, w, h):
    prev = (ex, ey)
    best = prev
    best_dist = abs(ex - px) + abs(ey - py)
    for nx, ny in ((ex + 1, ey), (ex - 1, ey), (ex, ey + 1), (ex, ey - 1)):
        if nx < 0 or nx >= w or ny < 0 or ny >= h:
            continue
        d = abs(nx - px) + abs(ny - py)
        if d < best_dist or (d == best_dist and compare_cell((nx, ny), best) < 0):
            best_dist = d
            best = (nx, ny)
    ex, ey = best
    loss = 1 if (ex, ey) == (px, py) and prev != (ex, ey) else 0
    return ex, ey, loss


def cmds(diagonal):
    if diagonal:
        return ["встать", "идти", "идти_диагональ", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"]
    return ["встать", "идти", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"]


def apply(cmd, state, own, partner, w, h, diagonal_skill, with_enemy):
    px, py, angle, sitting, stood, lines = state[:6]
    ex, ey, hp = state[6], state[7], state[8]

    if cmd == "встать":
        if not sitting:
            return None
        n = step_pos(px, py, angle, w, h)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        state = (nx, ny, angle, False, True, lines, ex, ey, hp)
    elif cmd == "идти":
        if sitting:
            return None
        n = step_pos(px, py, angle, w, h)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        state = (nx, ny, angle, False, stood, lines, ex, ey, hp)
    elif cmd == "идти_диагональ":
        if sitting or not diagonal_skill:
            return None
        n = step_pos(px, py, angle, w, h, diagonal=True)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, partner, lines)
        state = (nx, ny, angle, False, stood, lines, ex, ey, hp)
    elif cmd == "повернуть_90":
        if sitting:
            return None
        state = (px, py, norm(angle + 90), False, stood, lines, ex, ey, hp)
    elif cmd == "повернуть_-90":
        if sitting:
            return None
        state = (px, py, norm(angle - 90), False, stood, lines, ex, ey, hp)
    elif cmd == "повернуть_180":
        if sitting:
            return None
        state = (px, py, norm(angle + 180), False, stood, lines, ex, ey, hp)
    elif cmd == "сесть":
        if sitting or (px, py) != own:
            return None
        state = (px, py, angle, True, stood, lines, ex, ey, hp)
    else:
        return None

    if with_enemy:
        px, py, angle, sitting, stood, lines, ex, ey, hp = state
        ex, ey, loss = move_enemy(ex, ey, px, py, w, h)
        hp -= loss
        if hp <= 0:
            return None
        state = (px, py, angle, sitting, stood, lines, ex, ey, hp)
    return state


def goal(state, own):
    px, py, _, sitting, stood, lines, _, _, hp = state
    return sitting and stood and (px, py) == own and lines >= frozenset({"y", "o", "g"}) and hp > 0


def bfs(w, h, own, partner, spawn, angle, sitting, diagonal, with_enemy, enemy=None, max_len=220):
    if with_enemy:
        start = (spawn[0], spawn[1], angle, sitting, False, frozenset(), enemy[0], enemy[1], 3)
    else:
        start = (spawn[0], spawn[1], angle, sitting, False, frozenset(), 0, 0, 3)

    q = deque([(start, [])])
    seen = {start}
    for cmd in cmds(diagonal):
        pass

    while q:
        state, path = q.popleft()
        if goal(state, own):
            return path
        if len(path) >= max_len:
            continue
        for cmd in cmds(diagonal):
            nxt = apply(cmd, state, own, partner, w, h, diagonal, with_enemy)
            if nxt is None or nxt in seen:
                continue
            seen.add(nxt)
            q.append((nxt, path + [cmd]))
    return None


def try_seed(seed):
    results = {}
    w, h, own, partner, spawn, angle, sitting = build_level2(seed)
    path = bfs(w, h, own, partner, spawn, angle, sitting, False, False)
    if not path:
        return None
    results[2] = {"seed": seed, "commands": path}

    w, h, own, partner, spawn, angle, sitting = build_level3(seed)
    path = bfs(w, h, own, partner, spawn, angle, sitting, False, False)
    if not path:
        return None
    results[3] = {"seed": seed, "commands": path}

    w, h, own, partner, spawn, angle, sitting = build_level4(seed)
    path = bfs(w, h, own, partner, spawn, angle, sitting, True, False, max_len=200)
    if not path:
        return None
    results[4] = {"seed": seed, "commands": path}

    w, h, own, partner, spawn, angle, enemy = build_level5(seed)
    path = bfs(w, h, own, partner, spawn, angle, False, True, True, enemy, max_len=260)
    if not path:
        return None
    results[5] = {"seed": seed, "commands": path}
    return results


def main():
    level1 = json.loads((DOCS / "demo-level1-try.json").read_text(encoding="utf-8"))
    (DOCS / "demo-level1-try.json").write_text(
        json.dumps(level1, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    chosen = None
    for seed in [42, 7, 1, 123, 200, 17, 99, 314]:
        print(f"trying seed {seed}...")
        res = try_seed(seed)
        if res:
            chosen = seed, res
            print(f"  OK all levels 2-5 with seed {seed}")
            break

    if not chosen:
        raise SystemExit("No seed found for levels 2-5")

    seed, results = chosen
    for level, body in results.items():
        out = DOCS / f"demo-level{level}-try-seed{seed}.json"
        out.write_text(json.dumps(body, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"saved {out.name} ({len(body['commands'])} commands)")

    readme = DOCS / "DEMO-BODIES.md"
    readme.write_text(
        f"""# Demo JSON для Swagger / Postman

Проходите **по порядку**: 1 → 2 → 3 → 4 → 5.

| Уровень | Файл | Примечание |
|---------|------|------------|
| 1 | `demo-level1-try.json` | фиксированная карта |
| 2 | `demo-level{2}-try-seed{seed}.json` | seed {seed} |
| 3 | `demo-level{3}-try-seed{seed}.json` | после ур.3 — скилл диагонали |
| 4 | `demo-level{4}-try-seed{seed}.json` | можно `идти_диагональ` |
| 5 | `demo-level{5}-try-seed{seed}.json` | враг, 3 HP |

Login: `player` / `123456`
""",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
