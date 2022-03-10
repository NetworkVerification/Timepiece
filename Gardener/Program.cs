// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Gardener;
using Karesansui;

if (args.Length == 0)
{
  Console.WriteLine("No JSON file provided, exiting now...");
  return;
}


// var testFunc = new AstFunc<BatfishBgpRoute, BatfishBgpRoute>("x", new Return<BatfishBgpRoute>(new Var<BatfishBgpRoute>("x")));
// var writer = new StringWriter();
// serializer.Serialize(writer, testFunc);
// Console.WriteLine($"Test func serialization: {writer}");
var file = args[0];
var json = new JsonTextReader(new StreamReader(file));
var ast = Ast.Serializer().Deserialize<Ast>(json);
Console.WriteLine($"Successfully deserialized JSON file {file}");
json.Close();

Console.WriteLine($"Parsed an AST with JSON:");
Console.WriteLine(JsonConvert.SerializeObject(ast));
if (ast != null) Profile.RunCmp(ast.ToNetwork<BatfishBgpRoute>());
else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
