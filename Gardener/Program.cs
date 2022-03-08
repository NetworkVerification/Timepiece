// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Gardener;
using Karesansui;

if (args.Length == 0)
{
  Console.WriteLine("No JSON file provided, exiting now...");
  return;
}


var serializer = new JsonSerializer
{
  TypeNameHandling = TypeNameHandling.All,
  SerializationBinder = new AstBinder()
};
var testFunc = new AstFunc<BatfishBgpRoute, BatfishBgpRoute>("x", new Return<BatfishBgpRoute>(new Var<BatfishBgpRoute>("x")));
var writer = new StringWriter();
serializer.Serialize(writer, testFunc);
Console.WriteLine($"Test func serialization: {writer}");
var json = new JsonTextReader(new StreamReader(args[0]));
Console.WriteLine($"Input JSON: {json}");
var ast = serializer.Deserialize<Ast>(json);
json.Close();

Console.WriteLine($"#Nodes: {ast.Nodes.Keys.Count}");
Console.WriteLine(JsonConvert.SerializeObject(ast));
Profile.RunCmp(ast.ToNetwork<BatfishBgpRoute>());
