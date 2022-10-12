#!/usr/bin/env pwsh
# Run every benchmark in sequence.
# Note: as long as this file remains just a sequence of python calls,
# it's actually bash-compatible too!

# single destination versions
python3 ./run_all.py 4 60 r
python3 ./run_all.py 4 60 l
python3 ./run_all.py 4 60 v
python3 ./run_all.py 4 60 h
python3 ./run_all.py 4 60 r -m
python3 ./run_all.py 4 60 l -m
python3 ./run_all.py 4 60 v -m
python3 ./run_all.py 4 60 h -m
# all pairs versions
python3 ./run_all.py 4 60 ar
python3 ./run_all.py 4 60 al
python3 ./run_all.py 4 60 av
python3 ./run_all.py 4 60 ah
python3 ./run_all.py 4 60 ar -m
python3 ./run_all.py 4 60 al -m
python3 ./run_all.py 4 60 av -m
python3 ./run_all.py 4 60 ah -m
