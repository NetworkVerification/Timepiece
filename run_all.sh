#!/usr/bin/bash
# Run all benchmarks

DLL=Karesansui.Benchmarks/bin/Release/net6.0/Karesansui.Benchmarks.dll
SIZES=$(seq 4 4 40)
NTRIALS=5
TIMEOUT=4h

# fail if $DLL is missing
if [ ! -e $DLL ]; then
    echo "Could not find DLL $DLL, exiting..."
    exit 1
fi

declare -A dests
dests=([4]="edge-19" [8]="edge-79" [12]="edge-179" [16]="edge-319" [20]="edge-499" [24]="edge-719" [28]="edge-979"
      [32]="edge-1279" [36]="edge-1619" [40]="edge-1999")
for s in $SIZES;
do
    echo "Running benchmark k=$s with options:" "$@"
    for t in $(seq $NTRIALS);
    do
        echo "Trial $t of $NTRIALS"
        timeout $TIMEOUT dotnet "$DLL" -k "$s" -d "${dests[$s]}" "$@"
        if [ $? -eq 124 ]; then
            echo "Timed out after $TIMEOUT"
        fi
    done
done
