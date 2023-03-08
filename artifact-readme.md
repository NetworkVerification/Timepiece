# Timepiece

This repository contains the relevant code for evaluating the Timepiece system,
as described in the PLDI2023 submission #11, "Modular Control Plane
Verification via Temporal Invariants".

## Getting Started

All the relevant code for testing Timepiece's features as described in our
paper can be run using the Makefile, which builds a Docker container to run
Timepiece's benchmarks.
We also include the code of Angler, an additional tool that we used to extract
the Internet2 network configurations' details from Batfish and output a JSON file.
Our Docker image can also be used to run Timepiece using this JSON input.

The minimal required dependencies are [Docker](https://www.docker.com/)
and [Make](https://www.gnu.org/software/make/).
We use a couple of shell utilities that should be present on most Unix machines
to extract and evaluate our benchmarks, namely `test`, `timeout` and `tee`.
To generate plots, we require a **LaTeX distribution** with pgfplots.

### Step 1. Build the Docker image: `make image`

To start the evaluation, build the Docker image for Timepiece using `make image`.
This will build a Docker image that can run Timepiece.
You should see a series of messages to your screen as Docker describes
the stages of the build.

We include the `INTERNET2.angler.json` file that we generated in the repository
to save build time. This file is used to test Internet2.
We also provide the infrastructure to generate this file from the Internet2
network configurations if so desired.

### Step 2. Run the benchmarks: `make bench`

Now run `make bench` to run the benchmarks.
This will use the defaults defined in the Makefile for running the benchmarks,
which give each benchmark only a minute to run before timing out (controlled by
the TIMEOUT variable).
As such, many of the larger benchmarks will time out sooner than our reported
results in the paper.
Nonetheless, this should test that everything is working as intended.
`make bench` will run each fattree policy benchmark modularly and monolithically,
from a fattree of k=4 pods to k=40 pods (controlled by the MINSIZE and MAXSIZE variables).
The policies run are controlled by the POLICIES variable.
Output is captured by the Python script [`run_all.py`](./run_all.py), which buffers
it to the screen: you may therefore have to wait some time before any output appears.
By default, each benchmark runs once (controlled by the NTRIALS variable)

The output of each benchmark should be a report of the time taken to verify each
benchmark. For the fattree benchmarks, we expect all benchmarks to either pass
all checks (i.e. no counterexamples are reported), or timeout.
Output should roughly resemble:

```
$ make bench
Running benchmark k=4 with options: r
Trial 0 of 1 started ...
k=4
Inferred destination node: edge-19
Environment.ProcessorCount: ...
    All the modular checks passed!
Modular verification took ...
Check statistics:
Maximum check time: node ... in ...
Minimum check time: node ... in ...
Average check time: ...
Median check time: node ... in ...
99th percentile check time: node ... in ...
Total check time: ...
Running benchmark k=8 with options: r
Trial 0 of 1 started ...
k=8
Inferred destination node: edge-79
Environment.ProcessorCount: ...
    All the modular checks passed!
...
```

After a benchmark finishes running, the log of all output is saved to the `logs/`
directory as a `$(POLICY).txt` file (`$(POLICY)-m.txt` for monolithic benchmarks).
When the `--dat` option is supplied to `run_all.py`, it also generates a `.dat` file
which is used to generate the plot for the corresponding benchmark. These files are
saved to the `results/` directory as `$(POLICY).dat` or `$(POLICY)-m.dat`.
Each line of the `.dat` file is the average across trials of each statistic.
For modular benchmarks, these include:

- the number of nodes (n),
- the maximum time of a single node (max),
- minimum time of a single node (min),
- average time of a single node (avg),
- median time of a single node (med),
- 99th percentile time of a single node (99p),
- total time of all nodes' checks (total)
- wall-clock time of all nodes' checks (wall)

For example:

```
n       max     min     avg     med     99p     total   wall
293     2965    16      224.11945392491467      158     1876    65667   25350
```

As the benchmarks are parallelized, the wall-clock time illustrates the actual time
taken by the benchmarks once each check is distributed across your machine's cores.
Timepiece will attempt to use as many cores as are available to it: this statistic
is reported by the line starting with `Environment.ProcessorCount: ...`
(note that this number may be half as many as are available, for reasons unclear
to us).
We conducted our experiments on machines with 96 cores: we do not expect reviewers
to have access to such machines, so the wall clock times may be higher during
evaluation; nonetheless, the overall trends should be similar as the size of the
benchmark increases.

For monolithic benchmarks, statistics are simply the number of nodes and the total
time of all checks. For example:

```
n   total
2000  7200000
```

#### Step 3. Create the plots: `make plots`

Run `make plots` to generate all the LaTeX plots corresponding to the benchmarks.
These plots should resemble those in Figure 6 in the paper, although they will
necessarily have fewer data points as our default timeout is set to only 60 seconds.
To better indicate timeouts in our paper, we manually added data points of 7,200,000ms
in the .dat file to emphasize when timeouts occurred.
This is not done automatically by our logging tool `run_all.py`: if you wish to
include these data points, you must add rows to the appropriate `.dat` files, e.g.:

```
n   total
20  4936
# the following rows are manually added
80  7200000
180 7200000
320 7200000
500 7200000
720 7200000
980 7200000
1280    7200000
1620    7200000
2000    7200000
```

As mentioned above, you will need LaTeX and the `pdflatex` binary to be able to generate
these plots.
We generate plots by using a template .tex file called [`plot.tex`](./plot.tex).
This file uses the following packages:

- `tikz`
- `etoolbox`
- `mathptmx`
- `siunitx`
- `pgfplots` (version >=1.17)
- `pgfplotstable`
- `xspace`

#### Step 4. Run the Internet2 benchmark: `make internet2`

For the Internet2 benchmark, we run the docker container again, asking it to use
Timepiece.Angler to evaluate the Internet2 benchmark.
Timepiece.Angler works differently from Timepiece.Benchmarks: it reads in a given
.angler.json file and deserializes it to determine the network's behavior.
This process takes a few seconds, at the end of which standard output should report:

```
Internet2 benchmark identified...
Successfully deserialized INTERNET2.angler.json
```

This should then be followed by a series of counterexamples at particular
nodes (you may need to rerun or increase the timeout if the counterexamples are not
reported). As Internet2 has over 200 external peers, each counterexample will run
for close to 300 lines of output, describing which check fails and what the state
of the counterexample is.
This behavior is expected, as Internet2's BlockToExternal property may not be enforced
for some of its connections (whether intentionally or not).

We can also attempt to monolithically verify Internet2 by running `make internet2-m`.
In our experiments, we did not see monolithic verification complete after over
two hours.
As we do not use `run_all.py` to execute Timepiece.Angler, the benchmark must
be manually terminated by sending a SIGTERM signal.
We do so using `timeout` inside the container.
Because of this, the benchmark will simply exit once the timeout is reached.

## Step-by-Step Guide

### Fattrees

To reproduce the evaluation for each fattree policy, simply run

```
make TIMEOUT=7200
```

This runs each policy with a 2-hour timeout and generates its plot.
You may compare the .txt log and .dat result files with those included
in the [`paper-results`](./paper-results/) directory: note that the
paper-results dat files omit some statistics (max, min, avg)
and label others differently (tk=modular wall, ms=monolithic total).
The plots should roughly correspond to those in Figure 6 (page 15) of
the paper.
(Note that Figure 1 on page 2 is a simplified depiction of Figure 6(d),
without median or 99th percentile times reported.)

As highlighted in the paper, results should show timeouts occur quickly for
the monolithic benchmarks (around k=8 or k=12), with the exception of
the SpReach (r) benchmark, which is sufficiently simple to run to completion.

### Wide-area networks

To reproduce the evaluation of Internet2, run

```
make wan TIMEOUT=7200
```

This runs both Internet2 modularly and monolithically with a 2-hour timeout.
The details reported in the .txt logs should align with those given in text
on page 18 of the paper (under Wide-area networks).

## Other Evaluation

You can use the Makefile's `make angler` command to clone the `angler` repository
and use it directly to build the Internet2 benchmark from its configuration files.
Included in the repository is the `internet2.sh` shell script, which build a Docker
image for running Angler on the Internet2 benchmark's configuration files
and constructing an .angler.json file from the Batfish output.
Note that the `internet2.sh` script also requires that
[**Docker Compose**](https://docs.docker.com/compose/install/) is installed.

Building the Docker image can take some time, as the
`igraph` and `pybatfish` Python dependencies may take several minutes to build.
Once it is created, the `internet.sh` script will run batfish
together with angler to generate the `INTERNET2.angler.json` file from the
configurations.
The configuration files we used can be found in `angler/examples/INTERNET2`.
The `INTERNET2.json` file is the intermediate output from querying batfish,
while the `INTERNET2.angler.json` file is the one we generate ourselves.
Note that `INTERNET2.angler.json` may be close to 1GB in size.
