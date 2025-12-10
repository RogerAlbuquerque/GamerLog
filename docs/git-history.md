# About this repository's Git history

> **Synthetic history notice.** This repository is an educational Git lab for students. Its commit dates
> (2025-07-27 to 2025-12-31) were **assigned from a pre-generated schedule** while the project was being
> built; they do not record when the work was really done. Do not present this history as a real-world
> development timeline, and do not use it as evidence of working hours or effort.

## What was simulated

| Property | Value |
|---|---|
| Period | 2025-07-27 … 2025-12-31 (158 calendar days, UTC) |
| Commits | 305 |
| Inactive days | 15, never consecutive, never the first or last day |
| High-activity days | 20, each with exactly 7 commits, never consecutive |
| Other days | 123, each with 1–3 commits (a mix of 1, 2 and 3) |
| Timestamps | `AuthorDate == CommitterDate` for every commit, all in UTC |
| Messages | Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `build:`, `ci:`, `chore:`, …) |

The schedule (how many commits fall on which day, plus the random seed) was fixed before any code was
written. Commits were then made **in order, while the features were being implemented**: each commit contains
a coherent slice of the application, and the history was never reconstructed after the fact.

## How it works

Git reads the dates from two environment variables when a commit is created:

```bash
GIT_AUTHOR_DATE="2025-10-04T14:05:09+00:00" \
GIT_COMMITTER_DATE="2025-10-04T14:05:09+00:00" \
git commit -m "feat: add playthrough list"
```

Setting both to the same value is what makes author and committer timestamps identical. Within a day, the
times are increasing, so commits stay in chronological order.

## Checking it yourself

```bash
python3 scripts/validate_history.py . report.md
```

The script reads only `git log` and verifies: identical author/committer timestamps, UTC, date range,
15 / 20 / 123 day split, 1–3 commits on regular days, no consecutive inactive or 7-commit days, conventional
messages, no empty commits, chronological order and a clean working tree. A one-line check of the key property:

```bash
git log --format='%H %aI %cI' | awk '$2 != $3' | wc -l    # prints 0
```

## Known imperfections (kept honestly)

A few commits are corrections of earlier ones (for example a type error introduced by one commit and fixed
by the next). They are left in place: rewriting history to hide mistakes would defeat the purpose of a
teaching repository.
