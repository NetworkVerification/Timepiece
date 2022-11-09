using System.Diagnostics;
using Newtonsoft.Json;
using Timepiece;
using Timepiece.Angler;

ZenLib.ZenSettings.UseLargeStack = true;
ZenLib.ZenSettings.LargeStackSize = 30_000_000;

JsonSerializer Serializer()
{
  return new JsonSerializer
  {
    // use $type for type names, and the given binder
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = RouteEnvironmentAst.Binder(),
    // throw an error when members are missing from the object instead of ignoring them
    // MissingMemberHandling = MissingMemberHandling.Error
  };
}

foreach (var file in args)
{
  var json = new JsonTextReader(new StreamReader(file));
  RouteEnvironmentAst? ast;
  var isInternet2 = false;
  if (file.Contains("INTERNET2") || file.Contains("internet2") || file.Contains("BAGPIPE") || file.Contains("bagpipe"))
  {
    Console.WriteLine("Internet2 benchmark identified...");
    ast = Serializer().Deserialize<Internet2>(json);
    isInternet2 = true;
  }
  else
    ast = Serializer().Deserialize<RouteEnvironmentAst>(json);

  Console.WriteLine($"Successfully deserialized JSON file {file}");
  Debug.WriteLine("Running in debug mode...");
  Debug.WriteLine("Warning: additional assertions in debug mode may substantially slow running time!");
  json.Close();
  if (ast != null)
  {
    // ast.Validate();
    Profile.RunCmpPerNode(isInternet2
      ? ((Internet2) ast).ToNetwork(BlockToExternal.StrongInitialConstraints)
      : ast.ToNetwork());
    // Profile.RunAnnotatedWithStats(ast.ToNetwork());
  }
  else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
}
