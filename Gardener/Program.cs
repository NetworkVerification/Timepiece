using Newtonsoft.Json;
using Gardener;
using Karesansui;

JsonSerializer Serializer()
{
  return new JsonSerializer
  {
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = PairRouteAst.Binder()
  };
}

foreach (var file in args)
{
  var json = new JsonTextReader(new StreamReader(file));
  var ast = Serializer().Deserialize<PairRouteAst>(json);
  Console.WriteLine($"Successfully deserialized JSON file {file}");
  json.Close();
  if (ast != null)
    Profile.RunCmp(ast.ToNetwork());
  else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
}
