#!/usr/bin/env python3
# Run all the benchmarks
# Run with --help for usage.

import argparse
import datetime
from enum import Enum
import subprocess
import sys
import os.path

# name the output file after the current time
# as Windows filenames cannot contain ':' characters, we deviate slightly from the ISO representation
# to YYYY-MM-DD{T}HHMMSS, where {T} is the literal 'T' character
OUTPUT_FILE = "{:%Y-%m-%dT%H%M%S}.txt".format(
    datetime.datetime.now(datetime.timezone.utc)
)

DLL = "Timepiece.Benchmarks/bin/Release/net7.0/publish/Timepiece.Benchmarks.dll"


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


def run_dotnet(size, options, timeout, output_file) -> Response:
    """
    Run dotnet for the given benchmark size with the given options.
    size is an int
    options is a list of str
    output_file is None or a file name
    Return the return code of running the process.
    """
    args = ["dotnet", DLL, "-k", str(size)] + options
    # run the process, redirecting stderr to stdout,
    # timing out after TIMEOUT,
    # and raising an exception if the return code is non-zero
    proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    try:
        output, _ = proc.communicate(timeout=timeout)
        tee_output(output, output_file)
        return Response.SUCCESS
    except KeyboardInterrupt:
        kill_output = "Killing process..."
        tee_output(kill_output, output_file)
        proc.kill()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        return Response.USER_INTERRUPT
    except subprocess.TimeoutExpired:
        timeout_output = "Timed out after {time} seconds".format(time=timeout)
        tee_output(timeout_output, output_file)
        proc.kill()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        return Response.TIMEOUT


def run_all(sizes, trials, timeout, options, output_file, short_circuit=True):
    """
    Run the given benchmark for the sequence of sizes and trials.
    Pass the given options into dotnet and optionally save the results to
    the given output file.
    """
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
            returncode = run_dotnet(size, options, timeout, output_file)
            # if the benchmark timed out or was interrupted and short_circuit is set,
            # end immediately
            if returncode != Response.SUCCESS and short_circuit:
                return


def parser():
    parser = argparse.ArgumentParser(description="Run Timepiece benchmarks")
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
    parser.add_argument(
        "--size",
        "-k",
        nargs=2,
        type=int,
        help="Lower and upper bound on size of benchmark",
    )
    parser.add_argument(
        "--no-short-circuit",
        "-X",
        action="store_false",
        help="Run the remaining trials even if a previous trial times out or is interrupted by the user",
    )
    parser.add_argument("options", nargs="+", help="Options passed to DLL")
    return parser.parse_args()


if __name__ == "__main__":
    if not os.path.exists(DLL):
        print("Could not find DLL {}, exiting...".format(DLL))
        sys.exit(1)

    args = parser()
    SIZES = range(args.size[0], args.size[1] + 1, 4)
    run_all(
        SIZES,
        args.trials,
        args.timeout,
        args.options,
        OUTPUT_FILE,
        short_circuit=args.no_short_circuit,
    )
