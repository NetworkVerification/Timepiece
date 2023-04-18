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
    SerializationBinder = RouteEnvironmentAst.Binder()
    // throw an error when members are missing from the object instead of ignoring them
    // MissingMemberHandling = MissingMemberHandling.Error
  };
}

var rootCommand = new RootCommand("Timepiece benchmark runner");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically simulating Minesweeper");
var validateOption = new System.CommandLine.Option<bool>(
  new[] {"--validate", "-V"},
  "If given, validate the benchmark before running."
);
var fileArgument = new Argument<string>(
  "file",
  "The .angler.json file to use");
rootCommand.Add(fileArgument);
rootCommand.Add(monoOption);
rootCommand.Add(validateOption);
rootCommand.SetHandler(
  (file, mono, validate) =>
  {
    var json = new JsonTextReader(new StreamReader(file));
    RouteEnvironmentAst? ast;
    var isInternet2 = false;
    if (file.Contains("INTERNET2") || file.Contains("internet2") || file.Contains("BAGPIPE") ||
        file.Contains("bagpipe"))
    {
      Console.WriteLine("Internet2 benchmark identified...");
      ast = Serializer().Deserialize<Internet2>(json);
      isInternet2 = true;
    }
    else
    {
      ast = Serializer().Deserialize<RouteEnvironmentAst>(json);
    }

    Console.WriteLine($"Successfully deserialized JSON file {file}");
    Debug.WriteLine("Running in debug mode...");
    Debug.WriteLine("Warning: additional assertions in debug mode may substantially slow running time!");
    json.Close();
    if (ast != null)
    {
      if (validate) ast.Validate();
      if (mono)
        Profile.RunMonoWithStats(isInternet2
          ? ((Internet2) ast).ToNetwork(BlockToExternal.StrongInitialConstraints)
          : ast.ToNetwork());
      else
        Profile.RunAnnotatedWithStats(isInternet2
          ? ((Internet2) ast).ToNetwork(BlockToExternal.StrongInitialConstraints)
          : ast.ToNetwork());
    }
    else
    {
      Console.WriteLine("Failed to deserialize contents of {file} (received null).");
    }
  }, fileArgument, monoOption, validateOption);

await rootCommand.InvokeAsync(args);
