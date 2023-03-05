# syntax=docker/dockerfile:1
# stage to run Timepiece
FROM mcr.microsoft.com/dotnet/aspnet:latest as run
# install python
RUN apt-get update -y && apt-get install python3.9 -y

# stage to build Timepiece.Benchmarks
FROM mcr.microsoft.com/dotnet/sdk:latest as build
WORKDIR /timepiece
COPY Timepiece.Benchmarks/Timepiece.Benchmarks.csproj Timepiece.Benchmarks/
RUN dotnet restore "Timepiece.Benchmarks/Timepiece.Benchmarks.csproj"

COPY . .
RUN dotnet build Timepiece.Benchmarks -c Release -o /timepiece/build

# stage to publish
FROM build as publish
RUN dotnet publish Timepiece.Benchmarks -c Release -o /timepiece/publish

FROM run
# turn off net diagnostics
ENV DOTNET_EnableDiagnostics=0
WORKDIR /timepiece
COPY --from=publish /timepiece/publish publish
COPY run_all.py .
# Default command: run everything
# CMD python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- r; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- lw; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- v; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- h; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- ar; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- alw; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- av; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- ah; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- r -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- lw -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- v -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- h -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- ar -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- alw -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- av -m; \
#     python3.9 ./run_all.py -d /timepiece/publish -n 1 -t 30 -k 4 40 -- ah -m; \
#     echo 'done'
