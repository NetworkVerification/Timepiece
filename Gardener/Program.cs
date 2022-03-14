// See https://aka.ms/new-console-template for more information

using System.Diagnostics.Metrics;using System.Net;
using Newtonsoft.Json;
using Gardener;
using Karesansui;
using NetTools;

if (args.Length == 0)
{
  Console.WriteLine("No JSON file provided, exiting now...");
  return;
}

// var range = IPAddressRange.Parse("127.0.0.1");
// var writer = new StringWriter();
// Ast.Serializer().Serialize(writer, range);
// Console.WriteLine($"Range serialized: {writer}");
// var testFunc = new AstFunc<BatfishBgpRoute, BatfishBgpRoute>("x", new Return<BatfishBgpRoute>(new Var<BatfishBgpRoute>("x")));
// Console.WriteLine($"Test func serialization: {writer}");
var file = args[0];
var json = new JsonTextReader(new StreamReader(file));
var ast = Ast.Serializer().Deserialize<Ast>(json);
Console.WriteLine($"Successfully deserialized JSON file {file}");
json.Close();

Console.WriteLine($"Parsed an AST with JSON:");
Console.WriteLine(JsonConvert.SerializeObject(ast));
if (ast != null) Profile.RunCmp(ast.ToNetwork<BatfishBgpRoute>(IPAddress.Parse("127.0.0.1")));
else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
