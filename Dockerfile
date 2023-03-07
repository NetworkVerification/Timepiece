# syntax=docker/dockerfile:1
# stage to run Timepiece
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as run
# install python
RUN apt-get update -y && apt-get install python3.9 -y

# stage to build Timepiece.Benchmarks
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
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
