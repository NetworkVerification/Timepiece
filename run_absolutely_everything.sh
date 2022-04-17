#!/usr/bin/env bash
# Runs run_all.sh with various benchmarks.
# Really runs absolutely everything.

function run() {
    # run run_all with the given arguments and save to an output file
    fname="$(date +%F-%H-%M-%S)-${*// /_}.txt"
    bash ./run_all.sh "$@" | tee "$fname"
}

function ctrl-c() {
    echo "Killing dotnet..."
    pkill dotnet
}

trap ctrl-c INT

run 4 60 r
run 4 60 l
run 4 60 v
run 4 60 h
run 4 60 r -m
run 4 60 l -m
run 4 60 v -m
run 4 60 h -m
