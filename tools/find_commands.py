"""BFS: find winning command sequence for level 1 (same rules as GameEngine)."""
from collections import deque

W, H = 14, 8
OWN = (2, 4)
PARTNER = (9, 4)

DELTA = {0: (1, 0), 90: (0, 1), 180: (-1, 0), 270: (0, -1)}


def norm(a):
    return a % 360


def step_forward(x, y, angle):
    dx, dy = DELTA[norm(angle)]
    nx, ny = x + dx, y + dy
    if nx < 0 or nx >= W or ny < 0 or ny >= H:
        return None
    return nx, ny


def mark_lines(x, y, lines):
    px, py = PARTNER
    if x == px and y == py + 1:
        lines.add("y")
    if x == px and y == py - 1:
        lines.add("o")
    if x == px + 1 and y == py:
        lines.add("g")
    return lines


def apply(cmd, state):
    x, y, angle, sitting, stood, lines = state
    lines = set(lines)

    if cmd == "встать":
        if not sitting:
            return None
        n = step_forward(x, y, angle)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, lines)
        return nx, ny, angle, False, True, frozenset(lines)

    if cmd == "идти":
        if sitting:
            return None
        n = step_forward(x, y, angle)
        if n is None:
            return None
        nx, ny = n
        lines = mark_lines(nx, ny, lines)
        return nx, ny, angle, False, stood, frozenset(lines)

    if cmd == "повернуть_90":
        if sitting:
            return None
        return x, y, norm(angle + 90), False, stood, frozenset(lines)

    if cmd == "повернуть_-90":
        if sitting:
            return None
        return x, y, norm(angle - 90), False, stood, frozenset(lines)

    if cmd == "повернуть_180":
        if sitting:
            return None
        return x, y, norm(angle + 180), False, stood, frozenset(lines)

    if cmd == "сесть":
        if sitting or (x, y) != OWN:
            return None
        return x, y, angle, True, stood, frozenset(lines)

    return None


def goal(state):
    x, y, _, sitting, stood, lines = state
    return sitting and stood and lines >= {"y", "o", "g"} and (x, y) == OWN


def bfs():
    start = (OWN[0], OWN[1], 0, True, False, frozenset())
    cmds_list = ["встать", "идти", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"]
    q = deque([(start, [])])
    seen = {start}

    while q:
        state, path = q.popleft()
        if goal(state):
            return path
        if len(path) > 100:
            continue
        for cmd in cmds_list:
            nxt = apply(cmd, state)
            if nxt is None or nxt in seen:
                continue
            seen.add(nxt)
            q.append((nxt, path + [cmd]))
    return None


if __name__ == "__main__":
    result = bfs()
    if result:
        print(len(result), "commands")
        for c in result:
            print(c)
    else:
        print("NOT FOUND")
