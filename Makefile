BENCHMARKS=Timepiece.Benchmarks

all: bench

bench:
		dotnet publish $(BENCHMARKS) -c Release
