#!/usr/bin/env bash
# Collect the times taken by the checks from a .txt log file.
# Output a string suitable for use as the contents of a .dat file.
# The first input is the file name.
# The second input is a flag to indicate whether the file should be read as monolithic
# or modular. For monolithic logs, the flag "m" must be specified (note, there is no preceding '-').
#
# Usage: ./table.sh [input] m --> produce monolithic table
#        ./table.sh [input] --> produce modular table
INPUT=$1


case $2 in
    "m")
        runtimes=$(rg '^Monolithic verification took (\d+)ms' -Nor '$1' "$INPUT")
        # convert the destination node back to the number of nodes
        nodes=$(rg 'Inferred destination node: [a-z]+-\d+\n    ' -NUo "$INPUT" |\
            rg '(\d+)' -No |\
            xargs -I{} printf "{}+1\n" | bc )
        # this is the only stat so the times are just one column
        times=$(echo "$runtimes" | paste -)
        printf "n\tmono\n"
        ;;
     *)
        # collect all the ms times
        runtimes=$(rg '^((?:Modular)|(?:Maximum)|(?:Minimum)|(?:Average)|(?:99th)|(?:Median)) .*?(\d+.?\d*)ms' \
            -Nor '$2' "$INPUT")
        # convert the destination node back to the number of nodes
        nodes=$(rg 'Inferred destination node: ([a-z]+-(\d+))\nEnvironment' -NUor '$2' "$INPUT" | xargs -I{} printf "{}+1\n" | bc )
        # there are 6 modular output stats we want to collect,
        # so we want to paste the output of runtimes to concat every 6
        times=$(echo "$runtimes" | paste - - - - - -)
        printf "n\tmod\tmax\tmin\tavg\tmed\t99\n"
       ;;
esac

paste <(printf %s "$nodes") <(printf %s "$times")
