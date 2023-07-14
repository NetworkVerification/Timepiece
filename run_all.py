#!/usr/bin/env python3
# Run all the benchmarks
# Run with --help for usage.

import argparse
import csv
import datetime
from enum import Enum
import itertools
import pathlib
import re
import subprocess
import sys


def table_pattern_to_rows(s: str, pat: re.Pattern[str]) -> list[dict[str, float]]:
    """
    Convert the given text representing a one-row table
    to dictionary rows according to the specified pattern.
    The pattern should have two match groups: one for the table header,
    and one for the table data.
    """
    return [
        dict(zip(match[1].split("\t"), map(float, match[2].split("\t"))))
        for match in pat.finditer(s)
    ]


class Response(Enum):
    SUCCESS = 0
    USER_INTERRUPT = 1
    TIMEOUT = 2


def tee_output(output, output_file):
    """Print output and write to file if given."""
    if isinstance(output, bytes):
        print(output.decode("utf-8"))
    else:
        print(output)
    if output_file is not None:
        # 'ab': append bytes to the end of the file
        mode = "ab" if isinstance(output, bytes) else "a"
        with open(output_file, mode) as f:
            if isinstance(output, bytes):
                output += b"\n"
            else:
                output += "\n"
            f.write(output)


def run_dotnet(dll_file, options, timeout, output_file) -> tuple[Response, list[dict]]:
    """
    Run dotnet for the given dll file with the given options.
    options is a list of str
    output_file is None or a file name
    Return the return code of running the process and any collected table rows.
    """
    args = ["dotnet", dll_file] + options
    # run the process, redirecting stderr to stdout, timing out after TIMEOUT,
    # and raising an exception if the return code is non-zero
    proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    # regex patterns for identifying table rows for modular and monolithic benchmarks
    if "-m" in options:  # monolithic pattern
        output_pat = re.compile(r"^(n\ttotal)\n((?:[\d\.]+\s)*)", re.M)
    else:  # modular pattern
        output_pat = re.compile(
            r"^(n\tmax\tmin\tavg\tmed\t99p\ttotal\twall)\n((?:[\d\.]+\s)*)", re.M
        )
    try:
        output, _ = proc.communicate(timeout=timeout)
        tee_output(output, output_file)
        rows = table_pattern_to_rows(output.decode("utf-8"), output_pat)
        return (Response.SUCCESS, rows)
    except KeyboardInterrupt:
        kill_output = "Killing process..."
        tee_output(kill_output, output_file)
        proc.terminate()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        rows = table_pattern_to_rows(output.decode("utf-8"), output_pat)
        return (Response.USER_INTERRUPT, rows)
    except subprocess.TimeoutExpired:
        timeout_output = "Timed out after {time} seconds".format(time=timeout)
        tee_output(timeout_output, output_file)
        proc.kill()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        rows = table_pattern_to_rows(output.decode("utf-8"), output_pat)
        return (Response.TIMEOUT, rows)


def run_all(
    dll_file,
    sizes,
    trials,
    timeout,
    options,
    output_file,
    short_circuit=True,
) -> list[dict]:
    """
    Run the given benchmark for the sequence of sizes and trials.
    Pass the given options into dotnet and optionally save the results to
    the given output file.
    """
    rows = []
    for size in sizes:
        bench_output = "Running benchmark k={size} with options: {options}".format(
            size=size, options=" ".join(options)
        )
        tee_output(bench_output, output_file)
        for trial in range(trials):
            date = datetime.datetime.now(datetime.timezone.utc)
            trial_output = "Trial {t} of {total} started {date}".format(
                t=trial, total=trials, date=date
            )
            tee_output(trial_output, output_file)

            # run the benchmark
            # add [-k size] to the options to set the size
            returncode, bench_rows = run_dotnet(
                dll_file, ["-k", str(size)] + options, timeout, output_file
            )
            rows.extend(bench_rows)
            # if the benchmark timed out or was interrupted and short_circuit is set,
            # end immediately
            if returncode != Response.SUCCESS and short_circuit:
                return rows
    return rows


def run_angler(
    dll_file, angler_files, trials, timeout, output_file, short_circuit=True
):
    """Run the given angler dll for the given files for the specified number of trials."""
    rows = []
    for trial in range(trials):
        date = datetime.datetime.now(datetime.timezone.utc)
        trial_output = "Trial {t} of {total} started {date}".format(
            t=trial, total=trials, date=date
        )
        tee_output(trial_output, output_file)

        # run the benchmark
        returncode, bench_rows = run_dotnet(
            dll_file, angler_files, timeout, output_file
        )
        rows.extend(bench_rows)
        # if the benchmark timed out or was interrupted and short_circuit is set,
        # end immediately
        if returncode != Response.SUCCESS and short_circuit:
            return rows
    return rows


def parser():
    parser = argparse.ArgumentParser(description="Run Timepiece benchmarks")
    parser.add_argument(
        "--dll-path",
        "-d",
        type=pathlib.Path,
        default=pathlib.Path.cwd(),
        help=f"Path to the directory containing the DLL file",
    )
    parser.add_argument(
        "--trials",
        "-n",
        type=int,
        default=1,
        help="Number of trials to run (default: %(default))",
    )
    parser.add_argument(
        "--timeout",
        "-t",
        type=int,
        default=3600,
        help="Number of seconds to wait before timing out benchmark (default: %(default)s)",
    )
    benchmark_arg = parser.add_mutually_exclusive_group(required=True)
    benchmark_arg.add_argument(
        "--size",
        "-k",
        nargs=2,
        type=int,
        help="Lower and upper bound on size of benchmark",
    )
    benchmark_arg.add_argument(
        "--angler",
        "-a",
        action="store_true",
        help="Interpret inputs as angler files rather than benchmarks",
    )
    parser.add_argument(
        "--no-short-circuit",
        "-X",
        action="store_false",
        help="Run the remaining trials even if a previous trial times out or is interrupted by the user",
    )
    parser.add_argument(
        "--no-log",
        "-L",
        action="store_false",
        help="Do not log the result of running the benchmarks to a file",
    )
    parser.add_argument(
        "--dat",
        "-D",
        action="store_true",
        help="Output a .dat file summarizing the benchmark results in a table",
    )
    parser.add_argument("options", nargs="+", help="Options passed to DLL")
    return parser.parse_args()


if __name__ == "__main__":
    # create the log directory if necessary
    log_dir = pathlib.Path("logs")
    if not log_dir.exists():
        log_dir.mkdir()
    # name the output file after the current time
    # as Windows filenames cannot contain ':' characters, we deviate slightly from the ISO representation
    # to YYYY-MM-DD{T}HHMMSS, where {T} is the literal 'T' character
    # output_file = log_dir.joinpath(
    #     "{:%Y-%m-%dT%H%M%S}.txt".format(datetime.datetime.now(datetime.timezone.utc))
    # )

    # parse arguments and begin
    args = parser()

    # name the output file after the runner arguments
    if args.angler:
        # name it after the first angler file passed in
        output_file = log_dir.joinpath(
            pathlib.PurePath(args.options[0]).with_suffix(".txt").name
        )
    else:
        output_file = log_dir.joinpath("{}.txt".format("".join(args.options)))
    if output_file.exists():
        # move the old output file
        # add the {current time} in front of the original stem
        output_file.rename(
            output_file.with_stem(
                "{:%Y-%m-%dT%H%M%S}.{}".format(
                    datetime.datetime.now(datetime.timezone.utc), output_file.stem
                )
            )
        )
        # create a new file
        output_file.touch()

    # run the appropriate DLL
    if args.angler:
        dll = "Timepiece.Angler.dll"
    else:
        dll = "Timepiece.Benchmarks.dll"
    dll_file = args.dll_path.joinpath(dll)
    if not dll_file.exists():
        print("Could not find DLL {}, exiting...".format(dll))
        sys.exit(1)
    if args.angler:
        rows = run_angler(
            dll_file,
            args.options,
            args.trials,
            args.timeout,
            output_file if args.no_log else None,
            short_circuit=args.no_short_circuit,
        )
    else:
        sizes = range(args.size[0], args.size[1] + 1, 4)
        rows = run_all(
            dll_file,
            sizes,
            args.trials,
            args.timeout,
            args.options,
            output_file if args.no_log else None,
            short_circuit=args.no_short_circuit,
        )
    if args.dat:
        is_mono = "-m" in args.options
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
        # we use multiple trials to avoid noise in the results, hence we want to take the minimum
        min_rows = []
        for _, g in itertools.groupby(rows, key=lambda r: r["n"]):
            groups = list(g)
            min_rows.append({h: min(r[h] for r in groups) for h in headers})
        # create a .dat file in the results directory adjacent to logs
        results_path = pathlib.Path("results")
        if not results_path.exists():
            results_path.mkdir()
        dat_file = results_path.joinpath(output_file.stem + ".dat")
        with open(dat_file, "w") as dat:
            writer = csv.DictWriter(dat, fieldnames=headers, delimiter="\t")
            writer.writeheader()
            writer.writerows(min_rows)
