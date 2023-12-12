#!/usr/bin/env python3
# Print a .dat to stdout using the given benchmark log file.
# Usage: make_dat.py [log file] [mono|modular]

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
    """
    Convert the given text `s` to dictionary rows.
    Each row is one match of the `MOD_PAT` or `MONO_PAT` in `s`.
    """
    return [
        dict(zip(match[1].split("\t"), map(float, match[2].split("\t"))))
        for match in (MONO_PAT if is_mono else MOD_PAT).finditer(s)
    ]


if len(sys.argv) < 2:
    raise Exception("Usage: make_dat.py [log file] [mono|modular]")

log_file = pathlib.PurePath(sys.argv[1])
if sys.argv[2] == "mono":
    is_mono = True
elif sys.argv[2] == "modular":
    is_mono = False
else:
    raise Exception("Must specify modular or mono!")

with open(log_file, "r") as log:
    rows = output_to_rows(log.read(), is_mono)
if is_mono:
    headers = {"n": int, "total": float}
else:
    headers = {
            "n": int,
            "max": float,
            "min": float,
            "avg": float,
            "med": float,
            "99p": float,
            "total": float,
            "wall": float,
    }
min_rows = []
for _, g in itertools.groupby(rows, key=lambda r: r["n"]):
    groups = list(g)
    # take the row with the lowest total time
    min_row = min(groups, key=lambda row: row["total"])
    casted = {h: cast(min_row[h]) for h,cast in headers.items()}
    min_rows.append(casted)
writer = csv.DictWriter(sys.stdout, fieldnames=headers.keys(), delimiter="\t")
writer.writeheader()
min_rows.sort(key=lambda r: r["n"])
writer.writerows(min_rows)
