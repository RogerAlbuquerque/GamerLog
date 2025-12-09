#!/usr/bin/env python3
"""Independent validator for the GameLog synthetic Git history (spec section 9.7).

Reads ONLY `git log --all` output. Usage: validate_history.py <repo> [report.md]
Exit code 0 = every check passed.
"""
import collections
import datetime as dt
import re
import subprocess
import sys

START, END = dt.date(2025, 7, 27), dt.date(2025, 12, 31)
CC = re.compile(r"^(feat|fix|refactor|test|docs|chore|perf|build|ci|style)(\([a-z0-9-]+\))?: \S.{5,}$")


def main():
    repo = sys.argv[1]
    report = sys.argv[2] if len(sys.argv) > 2 else None
    raw = subprocess.check_output(
        ["git", "-C", repo, "log", "--all", "--format=%H|%aI|%cI|%an|%ae|%cn|%ce|%s"], text=True).splitlines()
    commits = [l.split("|", 7) for l in raw]
    checks, ok = [], True

    def check(name, cond, detail=""):
        nonlocal ok
        ok &= bool(cond)
        checks.append((name, bool(cond), detail))

    total = len(commits)
    check("Repository has commits", total > 0, f"{total} commits")
    ident = sum(1 for c in commits if c[1] == c[2])
    check("100% of commits have identical Author and Committer timestamps (%aI == %cI)", ident == total, f"{ident}/{total}")
    ident_person = sum(1 for c in commits if (c[3], c[4]) == (c[5], c[6]))
    check("Author and committer identity identical", ident_person == total, f"{ident_person}/{total}")
    check("All timestamps use UTC (+00:00)", all(c[1].endswith("+00:00") for c in commits))
    dates = [dt.datetime.fromisoformat(c[1]) for c in commits]
    check("Every commit within 2025-07-27..2025-12-31", all(START <= d.date() <= END for d in dates),
          f"min={min(dates).isoformat()} max={max(dates).isoformat()}")
    per_day = collections.Counter(d.date() for d in dates)
    all_days = [START + dt.timedelta(days=i) for i in range((END - START).days + 1)]
    check("Period has exactly 158 calendar days", len(all_days) == 158)
    inactive = [d for d in all_days if per_day[d] == 0]
    high = [d for d in all_days if per_day[d] == 7]
    normal = [d for d in all_days if per_day[d] not in (0, 7)]
    check("Exactly 15 inactive days", len(inactive) == 15, str(len(inactive)))
    check("Exactly 20 days with exactly 7 commits", len(high) == 20, str(len(high)))
    check("Exactly 123 remaining days", len(normal) == 123, str(len(normal)))
    check("Remaining days have 1-3 commits", all(1 <= per_day[d] <= 3 for d in normal))
    check("Remaining days show varying quantities", len({per_day[d] for d in normal}) == 3)

    def no_adjacent(days):
        s = sorted(days)
        return all((b - a).days > 1 for a, b in zip(s, s[1:]))

    check("No inactive dates are consecutive", no_adjacent(inactive))
    check("Inactive days sit between active days (not first/last day)", START not in inactive and END not in inactive)
    check("No high-activity dates are consecutive", no_adjacent(high))
    check("Inactive and high-activity dates do not overlap", not set(inactive) & set(high))
    distinct = all(len({d for d in dates if d.date() == h}) == 7 for h in high)
    check("Each 7-commit day has 7 distinct timestamps", distinct)
    check("Every commit message is conventional and meaningful", all(CC.match(c[7]) for c in commits),
          f"{sum(1 for c in commits if not CC.match(c[7]))} violations")
    empty = subprocess.run(["git", "-C", repo, "rev-list", "--all", "--max-count=1"], capture_output=True, text=True)
    bad_empty = 0
    for c in commits:
        n = subprocess.check_output(["git", "-C", repo, "show", "--stat", "--format=", c[0]], text=True).strip()
        if not n:
            bad_empty += 1
    check("No empty commits", bad_empty == 0, f"{bad_empty} empty")
    chron = [dt.datetime.fromisoformat(c[1]) for c in reversed(commits)]
    check("History is chronological (parents never newer than children)", chron == sorted(chron))
    tree = subprocess.check_output(["git", "-C", repo, "status", "--porcelain"], text=True).strip()
    check("Working tree clean", not tree, tree[:80])

    lines = ["# GameLog Git history validation report", "",
             f"Commits: **{total}**  |  Range: {min(dates).isoformat()} -> {max(dates).isoformat()}", "",
             "| # | Check | Result | Detail |", "|---|-------|--------|--------|"]
    for i, (n, c, d) in enumerate(checks, 1):
        lines.append(f"| {i} | {n} | {'PASS' if c else 'FAIL'} | {d} |")
    lines += ["", "## Inactive days (15)", ", ".join(d.isoformat() for d in inactive), "",
              "## 7-commit days (20)", ", ".join(d.isoformat() for d in high), "",
              f"## Overall: {'ALL CHECKS PASSED' if ok else 'FAILURES PRESENT'}"]
    text = "\n".join(lines)
    print(text)
    if report:
        open(report, "w").write(text + "\n")
    sys.exit(0 if ok else 1)


main()
