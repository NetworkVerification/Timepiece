// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Gardener;
using Karesansui;

if (args.Length == 0)
{
  Console.WriteLine("No JSON file provided, exiting now...");
  return;
}

var options = new JsonSerializerOptions
{
  PropertyNameCaseInsensitive = true,
};
var json = File.ReadAllText(args[0]);
Console.WriteLine($"Input JSON: {json}");
var ast = JsonSerializer.Deserialize<Ast>(json, options)!;

Console.WriteLine($"#Nodes: {ast.Nodes.Keys.Count}");
Console.WriteLine(JsonSerializer.Serialize(ast));
// Profile.RunCmp(ast.ToNetwork<dynamic, dynamic>());
