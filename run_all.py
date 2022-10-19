#!/usr/bin/env python3
# Run all the benchmarks
# Usage: run_all.py [lower size bound] [upper size bound] [options passed to DLL...]

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
# OUTPUT_FILE = None

DLL = "Timepiece.Benchmarks/bin/Release/net6.0/Timepiece.Benchmarks.dll"
NTRIALS = 3
# 14400 seconds == 4 hours
TIMEOUT = 14400


class Response(Enum):
    SUCCESS = 0
    USER_INTERRUPT = 1
    TIMEOUT = 2


def run_dotnet(size, options, output_file) -> Response:
    """
    Run dotnet for the given benchmark size with the given options.
    size is an int
    options is a list of str
    output_file is None or a file name
    Return the return code of running the process.
    """

    def tee_output(output, output_file):
        """Print output and write to file if given."""
        print(output.decode("utf-8"))
        if output_file is not None:
            # 'ab': append bytes to the end of the file
            with open(output_file, "ab") as f:
                f.write(output)

    args = ["dotnet", DLL, "-k", str(size)] + options
    # run the process, redirecting stderr to stdout,
    # timing out after TIMEOUT,
    # and raising an exception if the return code is non-zero
    proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    try:
        output, _ = proc.communicate(timeout=TIMEOUT)
        tee_output(output, output_file)
        return Response.SUCCESS
    except KeyboardInterrupt:
        print("Killing process...")
        proc.kill()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        return Response.USER_INTERRUPT
    except subprocess.TimeoutExpired:
        print("Timed out after {time} seconds".format(time=TIMEOUT))
        proc.kill()
        output, _ = proc.communicate()
        tee_output(output, output_file)
        return Response.TIMEOUT


def run_all(sizes, trials, options, output_file, short_circuit=True):
    """
    Run the given benchmark for the sequence of sizes and trials.
    Pass the given options into dotnet and optionally save the results to
    the given output file.
    """
    for size in sizes:
        print(
            "Running benchmark k={size} with options: {options}".format(
                size=size, options=" ".join(options)
            )
        )
        for trial in range(trials):
            date = datetime.datetime.now(datetime.timezone.utc)
            print(
                "Trial {t} of {total} started {date}".format(
                    t=trial, total=trials, date=date
                )
            )
            # run the benchmark
            returncode = run_dotnet(size, options, output_file)
            # if the benchmark timed out or was interrupted and short_circuit is set,
            # end immediately
            if returncode != Response.SUCCESS and short_circuit:
                return


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Not enough arguments given.")
        print(
            "Usage: run_all.py [lower size bound] [upper size bound] [options passed to DLL...]"
        )
        sys.exit(1)

    if not os.path.exists(DLL):
        print("Could not find DLL {}, exiting...".format(DLL))
        sys.exit(1)

    SIZES = range(int(sys.argv[1]), int(sys.argv[2]) + 1, 4)
    OPTIONS = sys.argv[3:]
    run_all(SIZES, NTRIALS, sys.argv[3:], OUTPUT_FILE)
