# syntax=docker/dockerfile:1
# stage to build Timepiece.Benchmarks
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
RUN dotnet restore
RUN dotnet publish Timepiece.Benchmarks -c Release -o /timepiece

# stage to run Timepiece
FROM phusion/baseimage:latest as runtime
# Run
COPY . /timepiece
RUN make /timepiece

# Run everything
CMD bash /timepiece/RunEverything.ps1
