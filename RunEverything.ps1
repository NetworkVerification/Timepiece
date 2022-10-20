#!/usr/bin/env pwsh
# Run every benchmark in sequence.
# Note: as long as this file remains just a sequence of python calls,
# it's actually bash-compatible too!
# -n = num trials
# -t = timeout in seconds
# -k = lower and upper size bound (inclusize range, 2 ints)
# -- = everything after this is an option for timepiece
#      "--" is required to ensure that the "-m" isn't parsed by python

# single destination versions
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- r
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- lw
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- v
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- h
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- r -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- lw -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- v -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- h -m
# all pairs versions
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- ar
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- alw
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- av
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- ah
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- ar -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- alw -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- av -m
python3 ./run_all.py -n 1 -t 3600 -k 4 40 -- ah -m
