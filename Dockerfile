# syntax=docker/dockerfile:1
# stage to build Timepiece.Benchmarks
FROM mcr.microsoft.com/dotnet/sdk:latest as build
WORKDIR /timepiece
COPY . .
RUN dotnet restore "Timepiece.Benchmarks/Timepiece.Benchmarks.csproj"
RUN dotnet build Timepiece.Benchmarks -c Release -o /timepiece/build

# stage to publish
FROM build as publish
RUN dotnet publish Timepiece.Benchmarks -c Release -o /timepiece/publish

# stage to run Timepiece
FROM mcr.microsoft.com/dotnet/aspnet:latest as run
# install python
RUN apt-get update -y && apt-get install python3.9 -y
# set up an alias for python to python3.9
# RUN alias python=python3.9
WORKDIR /timepiece
COPY --from=publish /timepiece/publish publish
COPY run_all.py .
# COPY RunEverything.ps1 .
# Default command: run everything
# TODO: extract results!!
CMD python3.9 ./run_all.py -n 1 -t 7200 -k 4 40 -- r
