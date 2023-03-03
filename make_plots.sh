#!/usr/bin/env sh
# Copy all logs out of the given Docker container, and then
# generate PDFs of the plots for each desired benchmark in the logs.
CONTAINER_NAME=$1
BENCHMARK=$2

mkdir -p results

docker cp "$CONTAINER_NAME":/timepiece/logs .
for f in logs/*; do
    # check that the log file is for the appropriate benchmark
    # this command extracts the options after the colon in the opening line
    # "Running ... with options: blah" --> blah
    bench=$(head -n1 "$f" | cut -d: -f2 | xargs | cut -d' ' -f1)
    if [ "$bench" != "$BENCHMARK" ]; then
        continue
    fi
    # FIXME: what to do if multiple files in logs refer to these??
    # get file benchmark type
    if grep -q "Modular" "$f"; then
        # modular benchmark
        grep -E -A1 --no-group-separator -e 'n	max' "$f" | sort -n -u > "results/$bench-mod.dat"
    else
        # monolithic benchmark
        grep -E -A1 --no-group-separator -e 'n	total' "$f" | sort -n -u > "results/$bench-mono.dat"
    fi
done
# generate the plot
# TODO: fix plot.tex somehow to use the correct file
pdflatex -halt-on-error -output-directory results plot.tex
# rename created plot file
if [ -e results/plot.pdf ]; then
    mv results/plot.pdf "results/$bench-plot.pdf"
fi
