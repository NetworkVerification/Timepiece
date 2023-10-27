# paper-results

This directory presents the results used in the PLDI 2023 paper,
"Modular Control Plane Verification via Temporal Invariants" by
Timothy Alberdingk Thijm, Ryan Beckett, Aarti Gupta and David Walker.

The result files are organized as follows:

* `.txt` files: logs of output from Timepiece as it was run; the logs
  specify the current running benchmark and report statistics on its runtime.
  File name prefixes are concatenations of the benchmark run, e.g. alwm is the "alw"
  (all prefix, weak length property) benchmark run monolithically (the "m").
* `.dat` files: tabulated data from the text output. The table columns are as follows
  (all times are in milliseconds):
    * `k`: the number of pods in the fat-tree
    * `tk`: the total wall-clock time taken by Timepiece (running in parallel)
    * `med`: the time taken by the median modular node check
    * `99`: the time taken by the 99th percentile modular node check
    * `ms`: the time taken by a monolithic Minesweeper-style single-threaded verification

The number of threads used is reported by the `Environment.ProcessorCount: ...` line.
We used timeouts on the benchmarks, which are also specified in the text files:
if a benchmark timed out, the timeout time was written into the data file.
