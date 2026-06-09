"""Verify demo commands against GameEngine logic (mirrors C# rules)."""
import json
from pathlib import Path

W, H = 14, 8
OWN = (2, 4)
PARTNER = (9, 4)
DELTA = {0: (1, 0), 90: (0, 1), 180: (-1, 0), 270: (0, -1)}


def norm(a):
    return a % 360


def run(commands):
    x, y, angle = OWN[0], OWN[1], 0
    sitting, stood = True, False
    lines = set()
    history = [(x, y)]

    def step():
        nonlocal x, y
        dx, dy = DELTA[norm(angle)]
        nx, ny = x + dx, y + dy
        if nx < 0 or nx >= W or ny < 0 or ny >= H:
            return False
        x, y = nx, ny
        history.append((x, y))
        px, py = PARTNER
        if x == px and y == py + 1:
            lines.add("y")
        if x == px and y == py - 1:
            lines.add("o")
        if x == px + 1 and y == py:
            lines.add("g")
        return True

    steps = 0
    for cmd in commands:
        if cmd == "встать":
            if not sitting:
                return False, history, lines
            sitting = False
            stood = True
            if not step():
                return False, history, lines
            steps += 1
        elif cmd == "идти":
            if sitting or not step():
                return False, history, lines
            steps += 1
        elif cmd == "повернуть_90":
            if sitting:
                return False, history, lines
            angle = norm(angle + 90)
            steps += 1
        elif cmd == "повернуть_-90":
            if sitting:
                return False, history, lines
            angle = norm(angle - 90)
            steps += 1
        elif cmd == "повернуть_180":
            if sitting:
                return False, history, lines
            angle = norm(angle + 180)
            steps += 1
        elif cmd == "сесть":
            if (x, y) != OWN:
                return False, history, lines
            sitting = True
            steps += 1
        else:
            return False, history, lines

    ok = stood and sitting and (x, y) == OWN and lines >= {"y", "o", "g"}
    return ok, history, lines


if __name__ == "__main__":
    path = Path(__file__).resolve().parent.parent / "docs" / "demo-level1-commands.json"
    commands = json.loads(path.read_text(encoding="utf-8"))
    ok, history, lines = run(commands)
    print("success:", ok)
    print("steps:", len(commands))
    print("lines:", lines)
    print("history length:", len(history))
