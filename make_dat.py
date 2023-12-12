#!/usr/bin/env python3
# Print a .dat to stdout using the given benchmark log file.
# Usage: make_dat.py [log file]

import csv
import itertools
import pathlib
import re
import sys

# regex patterns for identifying table rows for modular and monolithic benchmarks
MOD_PAT = re.compile(
    r"^(n\tmax\tmin\tavg\tmed\t99p\ttotal\twall)\n((?:[\d\.]+\s)*)", re.M
)
MONO_PAT = re.compile(r"^(n\ttotal)\n((?:[\d\.]+\s)*)", re.M)


def output_to_rows(s: str, is_mono: bool = False) -> list[dict[str, float]]:
    """Convert the given text to dictionary rows."""
    return [
        dict(zip(match[1].split("\t"), map(float, match[2].split("\t"))))
        for match in (MONO_PAT if is_mono else MOD_PAT).finditer(s)
    ]


log_file = pathlib.PurePath(sys.argv[1])
is_mono = "-m" in log_file.stem
with open(log_file, "r") as log:
    rows = output_to_rows(log.read(), is_mono)
if is_mono:
    headers = ["n", "total"]
else:
    headers = [
        "n",
        "max",
        "min",
        "avg",
        "med",
        "99p",
        "total",
        "wall",
    ]
min_rows = []
for _, g in itertools.groupby(rows, key=lambda r: r["n"]):
    groups = list(g)
    min_rows.append({h: min(r[h] for r in groups) for h in headers})
writer = csv.DictWriter(sys.stdout, fieldnames=headers, delimiter="\t")
writer.writeheader()
writer.writerows(min_rows)
