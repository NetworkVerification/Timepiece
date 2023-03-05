#!/usr/bin/env bash
# Generate PDFs of the plots for each desired benchmark in the logs.
TIMEOUT=$1
mkdir -p results
MODHEAD='n	max	min	avg	med	99p	total	wall'
MONOHEAD='n	total'

for benchmark in "${@:1}"; do
    modlog="${benchmark}.txt"
    moddat="${benchmark}.dat"
    monolog="${benchmark}-m.txt"
    monodat="${benchmark}-m.dat"
    if [ -e "logs/${modlog}" ] && [ -e "logs/${monolog}" ]; then
        # echo the header before the grep to ensure that the created file contains the header;
        # if none of the logged benchmarks complete before the timeout,
        # the result of grep on the log file will be empty
        (echo "$MODHEAD" && \
            grep -E -A1 --no-group-separator -e "$MODHEAD" "logs/${modlog}") | sort -n -u > "results/${moddat}"
        (echo "$MONOHEAD" && \
            grep -E -A1 --no-group-separator -e "$MONOHEAD" "logs/${monolog}") | sort -n -u > "results/${monodat}"
    else
        echo "Unable to find relevant log files for ${benchmark} benchmark."
        continue
    fi
    # generate the plot
    if [ -e "results/${moddat}" ] && [ -e "results/${monodat}" ]; then
        # generate the PDF using the configured choice of \timeout, \benchmono and \benchmod
        pdflatex -halt-on-error -output-directory results \
            "\newcommand\timeout{${TIMEOUT}}\newcommand\benchmono{${monodat}}\newcommand\benchmod{${moddat}}\input{plot.tex}"
        # rename created plot file
        if [ -e results/plot.pdf ]; then
            mv results/plot.pdf "results/${benchmark}plot.pdf"
        fi
    else
        echo "Unable to find relevant data files for ${benchmark} benchmark."
    fi
done
