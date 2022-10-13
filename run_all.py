#!/usr/bin/env python3
# Run all the benchmarks
# Usage: run_all.py [lower size bound] [upper size bound] [options passed to DLL...]

import datetime
import subprocess
import sys
import os.path

# name the output file after the current time
OUTPUT_FILE = "{}.txt".format(datetime.datetime.now(datetime.timezone.utc).isoformat())
# OUTPUT_FILE = None

DLL = "Timepiece.Benchmarks/bin/Release/net6.0/Timepiece.Benchmarks.dll"
NTRIALS = 3
# 14400 seconds == 4 hours
TIMEOUT = 14400


def run_dotnet(size, options, output_file):
    """
    Run dotnet for the given benchmark size with the given options.
    size is an int
    options is a list of str
    output_file is None or a file name
    """
    args = ["dotnet", DLL, "-k", str(size)] + options
    # run the process, redirecting stderr to stdout,
    # timing out after TIMEOUT,
    # and raising an exception if the return code is non-zero
    proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    try:
        output, _ = proc.communicate(timeout=TIMEOUT)
        print(output.decode("utf-8"))
        if output_file is not None:
            # 'ab': append bytes to the end of the file
            with open(output_file, "ab") as f:
                f.write(output)
    except KeyboardInterrupt:
        print("Killing process...")
        proc.kill()
        output, _ = proc.communicate()
        print(output.decode("utf-8"))
    except subprocess.TimeoutExpired:
        print("Timed out after {time} seconds".format(time=TIMEOUT))
        proc.kill()
        output, _ = proc.communicate()
        print(output.decode("utf-8"))


def run_all(sizes, trials, options, output_file):
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
            run_dotnet(size, options, output_file)


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
