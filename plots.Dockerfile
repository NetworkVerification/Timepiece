# syntax=docker/dockerfile:1
FROM leplusorg/latex:latest
WORKDIR /timepiece
COPY make_plots.sh plot.tex ./
COPY logs ./logs
