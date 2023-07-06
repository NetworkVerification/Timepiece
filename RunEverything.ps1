#!/usr/bin/env pwsh
# Run every benchmark in sequence.
# Note: as long as this file remains just a sequence of python3.9 calls,
# it's actually bash-compatible too!
# -n = num trials
# -t = timeout in seconds
# -k = lower and upper size bound (inclusize range, 2 ints)
# -- = everything after this is an option for timepiece
#      "--" is required to ensure that the "-m" isn't parsed by python3.9

# single destination versions
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- r -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- lw
python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- v -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- h -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- r -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- lw -m
python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- v -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- h -m
# all pairs versions
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ar
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- alw
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- av
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ah
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ar -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- alw -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- av -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ah -m
