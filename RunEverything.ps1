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
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- rs -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ls
python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- vs -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- hs -I
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- rs -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ls -m
python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- vs -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- hs -m
# all pairs versions
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ars
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- als
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- avs
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ahs
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ars -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- als -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- avs -m
# python3.9 ./run_all.py -d Timepiece.Benchmarks/bin/Release/net7.0/ -n 1 -t 7200 -k 4 40 -- ahs -m
