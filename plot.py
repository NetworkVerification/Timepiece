#!/usr/bin/env python3
# Using `matplotlib`, plot the results of the given benchmark .dat file.

import matplotlib.pyplot as plt
import pandas as pd
import pathlib
import sys
from cycler import cycler


def read_dat(path: str) -> pd.DataFrame:
    """
    Read in a table formatted like a .dat file: whitespace is used to separate columns.
    """
    dat_file = pathlib.PurePath(path)
    with open(dat_file, "r") as dat:
        return pd.read_table(dat, delim_whitespace=True)


def plot_modular_vs_mono(
    modular_table: pd.DataFrame, mono_table: pd.DataFrame, timeout=None
):
    """
    Plot verification time with respect to the number of nodes for modular verification
    vs. monolithic verification.
    If a `timeout` is given, add as an additional horizontal line.
    """
    cute_cycler = cycler(
        color=[
            "darkseagreen",
            "coral",
            "goldenrod",
            "cornflowerblue",
        ]
    ) + cycler(
        marker=["p", "^", "s", "D"]  # pentagon, triangle, square, diamond
    )
    fig, ax = plt.subplots()
    ax.set_prop_cycle(cute_cycler)
    ax.set_xlabel("Number of nodes")
    ax.set_yscale("log")
    ax.set_ylabel("Verification time (seconds)")
    if timeout is not None:
        to_line = ax.axhline(y=timeout, linestyle="--", color="#000000")
        # annotate that this is the timeout line
        ax.annotate(
            "SMT timeout",
            (0.2, 1),
            xycoords=to_line,
            ha="center",
            va="top",
            xytext=(0, -5),
            textcoords="offset points",
        )
    ax.plot("n", "wall", label="Timepiece (wall)", data=modular_table)
    ax.plot("n", "99p", label="Timepiece (99th percentile)", data=modular_table)
    ax.plot("n", "med", label="Timepiece (median)", data=modular_table)
    ax.plot("n", "total", label="Monolithic", data=mono_table)
    ax.legend()
    ax.grid(True, which="major")
    return fig


if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise Exception(
            "Usage: plot.py [modular dat file] [mono dat file] [output file (default: plot.pdf)] [timeout (in seconds)?]"
        )
    modular_table = read_dat(sys.argv[1])
    mono_table = read_dat(sys.argv[2])
    plotfile = sys.argv[3] if len(sys.argv) > 3 else "plot.pdf"
    timeout = float(sys.argv[4]) if len(sys.argv) > 4 else None
    fig = plot_modular_vs_mono(modular_table, mono_table, timeout)
    # plt.show()
    fig.savefig(plotfile)
