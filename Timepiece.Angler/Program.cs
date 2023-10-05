using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json;
using Timepiece;
using Timepiece.Angler;
using Timepiece.Angler.Ast;
using Timepiece.Angler.Queries;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

JsonSerializer serializer = new()
{
  // use $type for type names, and the given binder
  TypeNameHandling = TypeNameHandling.All,
  SerializationBinder = new AstSerializationBinder(),
  // throw an error when members are missing from the object instead of ignoring them
  // MissingMemberHandling = MissingMemberHandling.Error
};

var rootCommand = new RootCommand("Timepiece benchmark runner");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically (simulating Minesweeper)");
var fileArgument = new Argument<string>(
  "file",
  "The .angler.json file to use");
var queryArgument =
  new Argument<NetworkQueryType>("query",
    description: "The type of query to check",
    parse: result => NetworkQueryTypeExtensions.Parse(result.Tokens.Single().Value));
rootCommand.Add(fileArgument);
rootCommand.Add(queryArgument);
rootCommand.Add(monoOption);
rootCommand.SetHandler(
  (file, queryType, mono) =>
  {
    var json = new JsonTextReader(new StreamReader(file));

    var ast = serializer.Deserialize<AnglerNetwork>(json);

    Console.WriteLine($"Successfully deserialized JSON file {file}");
    Debug.WriteLine("Running in debug mode...");
    Debug.WriteLine("Warning: additional assertions in debug mode may substantially slow running time!");
    json.Close();
    if (ast != null)
    {
      var (topology, transfer) = ast.TopologyAndTransfer();
      var externalNodes = ast.Externals.Select(i => i.Name);
      // TODO: add support for other queries
      var query = queryType switch
      {
        NetworkQueryType.Internet2BlockToExternal => Internet2.BlockToExternal(topology, externalNodes),
        NetworkQueryType.Internet2NoMartians => Internet2.NoMartians(topology, externalNodes),
        // NetworkQueryType.FatReachable => Timepiece.Angler.Queries.FatTree.Reachable(topology, destination)
        _ => throw new ArgumentOutOfRangeException(nameof(queryType), queryType, "Query type not supported!")
      };
      var net = query.ToNetwork(topology, transfer, RouteEnvironmentExtensions.MinOptional);
      if (mono)
        Profile.RunMonoWithStats(net);
      else
        Profile.RunAnnotatedWithStats(net);
    }
    else
    {
      Console.WriteLine("Failed to deserialize contents of {file} (received null).");
    }
  }, fileArgument, queryArgument, monoOption);

await rootCommand.InvokeAsync(args);
