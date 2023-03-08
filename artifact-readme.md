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

```
> make bench
```

After a benchmark finishes running, the log of all output is saved to the `logs/`
directory as a `$(POLICY).txt` file (`$(POLICY)-m.txt` for monolithic benchmarks).
When the `--dat` option is supplied to `run_all.py`, it also generates a `.dat` file
which is used to generate the plot for the corresponding benchmark. These files are
saved to the `results/` directory as `$(POLICY).dat` or `$(POLICY)-m.dat`.
Each line of the `.dat` file is the

#### Step 3. Create the plots: `make plots`

Finally, run `make plots` to generate all the LaTeX plots corresponding to the benchmarks.
These plots should resemble those in the paper, although they will necessarily
have fewer data points as our timeout is set to be significantly lower.

## Step-by-Step Guide

## Other Evaluation

You can use the Makefile's `make angler` command to clone the `angler` repository
and use it directly to build the Internet2 benchmark from its configuration files.
Included in the repository is the `internet2.sh` shell script, which build a Docker
image for running Angler on the Internet2 benchmark's configuration files
and constructing an .angler.json file from the Batfish output.
Note that the `internet2.sh` script also requires **Docker Compose** to be installed.

Building the Docker image can take some time, as the
`igraph` and `pybatfish` Python dependencies may take several minutes to build.
