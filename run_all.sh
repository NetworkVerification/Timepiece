#!/usr/bin/env bash
# Run all benchmarks
# Usage: ./run_all.sh [lower size bound] [upper size bound] [options passed to DLL...]

# fail if not enough args
if [ $# -lt 3 ]; then
    echo "Not enough arguments given."
    echo "Usage: ./run_all.sh [lower size bound] [upper size bound] [options passed to DLL...]"
    exit 1
fi

DLL=Karesansui.Benchmarks/bin/Release/net6.0/Karesansui.Benchmarks.dll
SIZES=$(seq "$1" 4 "$2")
NTRIALS=3
TIMEOUT=4h

# fail if $DLL is missing
if [ ! -e $DLL ]; then
    echo "Could not find DLL $DLL, exiting..."
    exit 1
fi

dests=([4]="edge-19" [8]="edge-79" [12]="edge-179" [16]="edge-319" [20]="edge-499" [24]="edge-719" [28]="edge-979"
      [32]="edge-1279" [36]="edge-1619" [40]="edge-1999")
for s in $SIZES;
do
    echo "Running benchmark k=$s with options:" "${@:3}"
    for t in $(seq $NTRIALS);
    do
        echo "Trial $t of $NTRIALS"
        timeout $TIMEOUT dotnet "$DLL" -k "$s" -d "${dests[$s]}" "${@:3}"
        if [ $? -eq 124 ]; then
            echo "Timed out after $TIMEOUT"
        fi
    done
done
