# Timepiece

Timepiece is a network verification tool for verifying properties of distributed network control planes.
It does so efficiently and scalably by dividing the verification task into independent SMT queries on each node.

## Build

To build Timepiece, you will need [`dotnet`](https://dotnet.microsoft.com/en-us/download).

```shell
dotnet build Timepiece.Angler
```

## Run

Timepiece can take network configurations from Batfish that have been processed by [Angler](https://github.com/NetworkVerification/angler).

```sh
dotnet run --project Timepiece.Angler -- run FILE.json [query]
```

The list of predefined queries can be found by passing the `list-queries` subcommand.

```shell
dotnet run --project Timepiece.Angler -- list-queries
```

### Benchmarking

Each time Timepiece runs, it measures the time taken by the SMT solver to perform each node's checks. 
Timepiece sorts the resulting times,
and outputs a [statistical summary](https://github.com/NetworkVerification/Timepiece/tree/main/Timepiece/Statistics.cs).
This summary includes:
  * How long the solver took to verify all nodes, per ["wall-clock time"](https://en.wikipedia.org/wiki/Elapsed_real_time).
  Reported as "Modular verification".
  * The fastest node to verify, and how long the solver took to perform its checks. Reported as the "minimum time".
  * The slowest node to verify, and how long the solver took to perform its checks. Reported as the "maximum time".
  * The median node to verify, and how long the solver took to perform its checks. Reported as the "median time".
  * The average time taken by the solver to perform its checks. Reported as the "average time".
  * The 99th percentile node to verify, and how long the solver took to perform its checks. 
  Reported as the "99th percentile time".
  * How long the solver took in total to verify _all_ nodes' checks. Reported as the "total time".

When run monolithically, Timepiece simply reports the total time taken by monolithic verification.
The statistics reported can be changed by editing the code in 
[`Profile.cs`](https://github.com/NetworkVerification/Timepiece/tree/main/Timepiece/Profile.cs).

The summary is also duplicated in table format, using short-hands for the information above.
The table lists the following data:
  * `n`, the number of nodes in the topology;
  * `max`, the maximum time;
  * `min`, the minimum time;
  * `avg`, the average time;
  * `med`, the median time;
  * `99p`, the 99th-percentile time;
  * `total`, the total time; and
  * `wall`, the wall-clock time.
For monolithic benchmarks, only `n` and `total` are listed.

Timepiece will try and catch a user interrupt signal (CTRL-C) and report partial results,
but depending on when the interrupt is sent, if verification has not yet begun, 
results will _not_ be reported.

## Generating Plots

Included with Timepiece are two Python scripts to help users generate plots of the data output by the tool.
These scripts allow users to compare verification times between Timepiece's modular verifier and its implemented
monolithic verifier.

### Generating a Table of Results

The benchmarked output can be converted into a .dat table file via the 
[`make_dat.py`](https://github.com/NetworkVerification/Timepiece/tree/main/make_dat.py)
Python script.
Each row of the table is the table output extracted from a given benchmark.
To reduce the influence of noise on the results,
given multiple table rows for the same choice of `n`, indicating multiple trials for the same network,
the script chooses the row with the _smallest_ total time `total`.
`make_dat.py` must be run separately to create a table of modular results and a table of monolithic results.

_NB:_ To construct a table containing all results from a series of benchmarks, one may use `cat` on Unix, _e.g._,
``` shell
# ...run Timepiece and save output as FatReachable.*.modular.out files...
python make_dat.py <(cat FatReachable.*.modular.out) modular > FatReachable.modular.dat
```

### Generating a Plot

Once a matching modular and monolithic .dat file are created using `make_dat.py`, 
they can then be supplied to the
[`plot.py`](https://github.com/NetworkVerification/Timepiece/tree/main/plot.py)
Python script, which will produce a PDF file (by default called `plot.pdf`) plotting the modular results against
the monolithic ones, with the number of nodes on the x-axis and the verification time in seconds
on the y-axis.

``` shell
# usage: plot.py [modular dat file] [monolithic dat file] [output file (default: plot.pdf)] [timeout in seconds (optional)]
python plot.py FatReachable.modular.dat FatReachable.mono.dat FatReachable.pdf 14400000 
```

#### Plotting timeouts

`plot.py` also accepts an optional timeout argument: if supplied, a dashed black line will
be added to the plot, showing when a timeout was sent to interrupt verification.
Since we aren't guaranteed to report the time results for verification when a timeout occurs,
benchmarks that timeout may not be shown on the plot.
You can add data points for timed-out benchmarks by hand by writing in additional rows
in the .dat file, _e.g.,_
``` csv
n   total
20  1000
# added by hand, assuming timeout is 7200 seconds (2 hours)
80  7200
180 7200
# ...etc...
```
