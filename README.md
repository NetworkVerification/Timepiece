# Timepiece

Timepiece is a network verification tool for verifying properties of distributed network control planes.
It does so efficiently and scalably by dividing the verification task into independent SMT queries on each node.

## Build

To build Timepiece, you will need [`dotnet`](https://dotnet.microsoft.com/en-us/download).

```shell
dotnet build Timepiece.Angler
```

## Run

Timepiece can take network configurations from Batfish that have been processed by [Angler](https://github.com/NetworkVerification/angler).

```sh
dotnet run --project Timepiece.Angler -- FILE.json [query]
```

The list of predefined queries can be found by passing the `list-queries` subcommand.

```shell
dotnet run --project Timepiece.Angler -- list-queries
```
