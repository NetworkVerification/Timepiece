# Timepiece

This repository contains the relevant code for evaluating the Timepiece system,
as described in the PLDI2023 submission #11, "Modular Control Plane Verification via Temporal Invariants".

## Getting Started

All the relevant code for testing Timepiece's features as described in our paper can be run using the
Makefile, which builds a Docker container to run Timepiece's benchmarks.
We also include the code of Angler, an additional tool that we used to extract the Internet2
network configurations' details from Batfish and output a JSON file.
Our Docker image can also be used to run Timepiece using this JSON input.

The minimal required dependencies are [Docker](https://www.docker.com/)
and [Make](https://www.gnu.org/software/make/).
We use a couple of other utilities that should be present on most Unix machines to extract and
evaluate our benchmarks: these include a **LaTeX distribution** for generating plots,
along with the shell utilities `test`, `timeout` and `tee` for running our Internet2 benchmark.

### Step 1. Build the Docker image

To start the evaluation, build the Docker image for Timepiece using `make image`.
This will build a Docker image that can run Timepiece.
You should see a series of messages to your screen as Docker describes the stages of the build.

To simplify the process of running the Angler Internet2 benchmark, the build requires the
`INTERNET2.angler.json` file to have been constructed.

### Step 2.
