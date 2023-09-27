using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json;
using Timepiece;
using Timepiece.Angler;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

JsonSerializer Serializer()
{
  return new JsonSerializer
  {
    // use $type for type names, and the given binder
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = AnglerNetwork.Binder()
    // throw an error when members are missing from the object instead of ignoring them
    // MissingMemberHandling = MissingMemberHandling.Error
  };
}

var rootCommand = new RootCommand("Timepiece benchmark runner");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically simulating Minesweeper");
var fileArgument = new Argument<string>(
  "file",
  "The .angler.json file to use");
rootCommand.Add(fileArgument);
rootCommand.Add(monoOption);
rootCommand.SetHandler(
  (file, mono) =>
  {
    var json = new JsonTextReader(new StreamReader(file));
    var isInternet2 = false;
    if (file.Contains("INTERNET2") || file.Contains("internet2") || file.Contains("BAGPIPE") ||
        file.Contains("bagpipe"))
    {
      Console.WriteLine("Internet2 benchmark identified...");
      isInternet2 = true;
    }

    var ast = Serializer().Deserialize<AnglerNetwork>(json);

    Console.WriteLine($"Successfully deserialized JSON file {file}");
    Debug.WriteLine("Running in debug mode...");
    Debug.WriteLine("Warning: additional assertions in debug mode may substantially slow running time!");
    json.Close();
    if (ast != null)
    {
      var (topology, transfer) = ast.TopologyAndTransfer();
      Console.WriteLine(topology);
      var query = isInternet2
        ? BlockToExternal.StrongInitialConstraints(topology, ast.Externals.Select(i => $"{i.ip}"))
        : throw new NotImplementedException("Non-Internet2 networks not supported");
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
  }, fileArgument, monoOption);

await rootCommand.InvokeAsync(args);
