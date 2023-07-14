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
To generate plots, we require a **LaTeX distribution** with pgfplots.

We use [bind mounts](https://docs.docker.com/storage/bind-mounts/) to read and write
from the results and logs files.

### Windows

We've tested this artifact using Linux and MacOS, and our Makefile uses common
Unix commands such as `sh`, `date`, `mkdir`, `mv` and `rm` to manipulate files.
You may need to change these commands to the appropriate Windows equivalents
if running this artifact on Windows.

### Step 0. Set up

Be sure you have space free to build the docker image.
The image used for the benchmarks is around 1.35 GB, and may fail to
build if your machine is out of space.
Running `docker system prune` can help delete old containers and
images you no longer use.

Ensure that you have `make` installed and available from the command line.
Ensure your system has `pdflatex` installed, along with the following packages
(available in most standard LaTeX distributions like TeX Live and MikTeX):

- [`tikz`](https://www.ctan.org/pkg/pgf)
- [`etoolbox`](https://ctan.org/pkg/etoolbox)
- [`mathptmx`](https://ctan.org/pkg/mathptmx)
- [`siunitx`](https://ctan.org/pkg/siunitx)
- [`pgfplots`](https://ctan.org/pkg/pgfplots) (version >=1.17)
- [`pgfplotstable`](https://ctan.org/pkg/pgfplotstable)
- [`xspace`](https://ctan.org/pkg/xspace)
- [`standalone`](https://ctan.org/pkg/standalone)

### Step 1. Build the Docker image: `make image`

To start the evaluation, build the Docker image for Timepiece using `make image`.
This will build a Docker image that can run Timepiece.
You should see a series of messages to your screen as Docker describes
the stages of the build.

We include the `INTERNET2.angler.json` file that we generated in the repository
to save build time. This file is used to test Internet2 and is quite large (~1GB).
We provide the infrastructure to regenerate this file from the Internet2
network configurations (if so desired) in the `angler` directory.

### Step 2. Run the benchmarks: `make bench`

Now run `make bench` to run the benchmarks.
This will use the defaults defined in the Makefile for running the benchmarks,
which give each benchmark _only a minute to run_ before timing out (controlled by
the `TIMEOUT` variable).
As such, many of the larger benchmarks will time out sooner than our reported
results in the paper.
Nonetheless, this should test that everything can run as intended.

`make bench` will run each fattree policy benchmark modularly and monolithically,
from a fattree of k=4 pods to k=40 pods (controlled by the `MINSIZE` and `MAXSIZE` variables).
The policies run are controlled by the `POLICIES` variable.
Output is captured by the Python script [`run_all.py`](./run_all.py), which buffers
it to the screen: you may therefore have to wait some time before any output appears.
By default, each benchmark runs once (controlled by the `NTRIALS` variable):
you can automatically instruct `run_all.py` to take the minimum of multiple trials
(to reduce the impact of noise) by increasing `NTRIALS`.

The output of each benchmark should be a report of the time taken to verify each
benchmark. For the fattree benchmarks, we expect all benchmarks to either pass
all checks (i.e. no counterexamples are reported), or timeout.
Output should roughly resemble:

```
$ make bench
Running benchmark k=4 with options: reach
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
Running benchmark k=8 with options: reach
Trial 0 of 1 started ...
k=8
Inferred destination node: edge-79
Environment.ProcessorCount: ...
    All the modular checks passed!
...
```

After a benchmark finishes running, the log of all output is saved to the `logs/`
directory as a `$(POLICY).txt` file (`$(POLICY)-m.txt` for monolithic benchmarks).
The `--dat` option, supplied to `run_all.py`, instructs the script to generate a `.dat` file.
This file is used to generate the plot for the corresponding benchmark. These files are
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
these plots, along with the packages mentioned above.
We generate plots by using a template .tex file called [`plot.tex`](./plot.tex).

The generated plots have slightly different names from the plots in the paper,
as the policies are listed in shorthand rather than their full names.
The mapping between them is:

- `results/reach.pdf`: the SpReach benchmark
- `results/lengthWeak.pdf`: the SpLen benchmark
- `results/valley.pdf`: the SpVf benchmark
- `results/hijack.pdf`: the SpHijack benchmark
- `results/allReach.pdf`: the ApReach benchmark
- `results/allLengthWeak.pdf`: the ApLen benchmark
- `results/allValley.pdf`: the ApVf benchmark
- `results/allHijack.pdf`: the ApHijack benchmark

#### Step 4. Run the Internet2 benchmarks: `make logs/internet2.txt`

For the Internet2 benchmark, we run the docker container again, asking it to use
Timepiece.Angler to evaluate the Internet2 benchmark.
Timepiece.Angler works differently from Timepiece.Benchmarks: it reads in a given
`.angler.json` file and deserializes it to determine the network's behavior.
This process takes a few seconds, at the end of which standard output should report:

```
Internet2 benchmark identified...
Successfully deserialized INTERNET2.angler.json
```

This should then be followed by a series of counterexamples at particular
nodes.
_You may need to rerun or increase the timeout if the counterexamples are not reported_:
setting `TIMEOUT=300` (5 minutes) should be more than enough for the modular benchmark.
As Internet2 has over 200 external peers, each counterexample will run
for close to 300 lines of output, describing which check fails and what the state
of the counterexample is.
This behavior is expected, as Internet2's BlockToExternal property may not be enforced
for some of its connections (whether intentionally or not).

Example output may look something like:

```
Internet2 benchmark identified...
Successfully deserialized JSON file INTERNET2.angler.json
Environment.ProcessorCount: 4
    Counterexample for node 10.11.1.17:
Inductive check failed!
symbolic external-route-10.11.1.17 := RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/0, Weight=0, Lp=0, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})
symbolic external-route-108.59.25.20 := RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/0, Weight=0, Lp=0, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})
symbolic external-route-108.59.26.20 := RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/0, Weight=0, Lp=0, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})
symbolic external-route-109.105.98.9 := RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/0, Weight=0, Lp=0, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})
symbolic external-route-117.103.111.154 := RouteEnvironment(Result=RouteResult(Exit=False,Fallthrough=False,Returned=False,Value=False), LocalDefaultAction=False, Prefix=0.0.0.0/0, Weight=0, Lp=0, AsPathLength=0, Metric=0, OriginType=0, Tag=0, Communities={})
# and so on
...
```

We can also attempt to monolithically verify Internet2 by running `make logs/internet2-m.txt`.
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

As we claim in the paper, results should show timeouts occur quickly for
the monolithic benchmarks (around k=8 or k=12), with the exception of
the SpReach (reach) benchmark, which is sufficiently simple to run to completion.
Times should be larger for Ap benchmarks over Sp benchmarks.

We _cannot guarantee_ that the exact times reported in the paper will hold
in the artifact environment (e.g. the 99th percentile ApReach time for k=40
may be above 9 seconds). Nonetheless, the trends of our results should
be supported by the artifact times.

### Wide-area networks

To reproduce the evaluation of Internet2, run

```
make wan TIMEOUT=7200
```

This runs both Internet2 modularly and monolithically with a 2-hour timeout.
The details reported in the .txt logs should align with those given in text
on page 18 of the paper (under Wide-area networks).

## Other Evaluation

### Making INTERNET2.angler.json from scratch

You can use the Makefile's `make angler/INTERNET2.angler.json` command
to build the Internet2 benchmark from its configuration files.
Included in the `angler` repository is the `internet2.sh` shell script,
which builds a Docker image for running Angler on the Internet2 benchmark's
configuration files and constructing an .angler.json file from the Batfish output.
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

### Modifying benchmarks

You can explore different properties and benchmarks by modifying the C# files,
or changing the fattree policies used by make (using the POLICIES variable).
There are alternative policies ['l'](./Timepiece.Benchmarks/Sp.cs) (line 107)
and ['vl'](./Timepiece.Benchmarks/Vf.cs) (line 82)
that check slightly-different properties, as shown in the code.

It is also straightforward to add a new policy to test:

1. Define a [`Network.cs`](./Timepiece/Networks/Network.cs) instance
   ([`Sp.cs`](./Timepiece.Benchmarks/Sp.cs) and [`Vf.cs`](./Timepiece.Benchmarks/Vf.cs)
   illustrate this concept for specific subclasses of `Network`).
   You can make the network parametric in terms of the fattree size
   by following those examples.
2. Add a case for this network instance to [`Benchmark.cs`] (see `Run()` and
   `BenchmarkTypeExtensions.Parse()`).
3. Run `Timepiece.Benchmarks` with your new policy.

Modifications to the benchmarks' properties, annotations and other behavior
is also possible.
Properties and annotations are arbitrary C# functions which take [`Zen`](https://github.com/microsoft/Zen)
objects as arguments.
You may refer to the [Zen documentation](https://github.com/microsoft/Zen/wiki)
for more information on what operations Zen supports.
We defined temporal operators in our work as shorthands for common functions,
which return a `Func<Zen<T>, Zen<BigInteger>, Zen<bool>>` as output.

- `Zen<T>` is an input representing a route of type `T`, lifted to Zen
- `Zen<BigInteger>` represents the time input
- `Zen<bool>` is the output Zen-lifted boolean indicating if the function holds
  on the inputs.

The [`Timepiece.Lang`](./Timepiece/Lang.cs) file provides these operators
along with other useful static functions.
We use `Time` as a shorthand for `Zen<BigInteger>` in this file.
These include:

- `Globally`: the "script-G" operator from the paper.
- `Until`: the "script-U" operator from the paper.
- `Finally`: the "script-F" operator from the paper.
- `Intersect`: lifted set intersection (the "square-cap" operator from the paper).
- `Union`: lifted set union (the "square-cup" operator from the paper).

For instance, if one wanted to add a new `Complement`
operator to `Timepiece.Lang` to represent the lifted set complement
operator specified in the paper, one could write:

```c#
public static Func<Zen<T>, Zen<BigInteger>, Zen<bool>> Complement<T>(Func<Zen<T>, Zen<BigInteger>, Zen<bool>> f)
{
    return (r, t) => Zen.Not(f(r, t));
}
```

[`Timepiece.Networks.Network`](./Timepiece/Networks/Network.cs)
contains the generic definition of a network instance.
The `Network` constructor on line 110 is used in our benchmarks:
note that it takes two types of properties as input, both expressed
as `Func<Zen<T>, Zen<bool>>`: `stableProperties`, which represent `Finally` properties
that hold at a certain _convergence time_ (4 in the case of our fattree benchmarks);
and `safetyProperties`, which represent `Globally` properties that always holds.

Users may add _symbolic variables_ to their network instances
to represent nondeterministic behavior.
The [`Timepiece.SymbolicValue`](./Timepiece/SymbolicValue.cs) module
provides the API for defining these values.
[`Timepiece.Benchmarks.SymbolicDestination`](./Timepiece.Benchmarks/SymbolicDestination.cs)
provides a subclass to `SymbolicValue` for our `Ap` benchmarks.

## Troubleshooting

### Lingering `tptest` container

You may need to run `docker rm tptest` if you receive an error
that the benchmarking container cannot start,
as the `tptest` container is already in use.

### New logs not generated

If you run a benchmark again after a log file has previously been generated,
`make` may skip that benchmark.
You must delete or rename the old log files before running make again.
The `make clean` recipe will delete the logs and results files,
while the `make archive` recipe will move them to a new directory
specifying the current time.
