#!/usr/bin/env bash
# Generate PDFs of the plots for each desired benchmark in the logs.
TIMEOUT=$1
mkdir -p results

for benchmark in "${@:2}"; do
    modlog="${benchmark}.txt"
    moddat="${benchmark}.dat"
    monolog="${benchmark}-m.txt"
    monodat="${benchmark}-m.dat"
    if [ -e "logs/${modlog}" ] && [ -e "logs/${monolog}" ]; then
        python3 make_dat.py "logs/${modlog}" > "results/${moddat}"
        python3 make_dat.py "logs/${monolog}" > "results/${monodat}"
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
