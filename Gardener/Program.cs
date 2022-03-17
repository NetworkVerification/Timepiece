// See https://aka.ms/new-console-template for more information

using System.Net;
using Newtonsoft.Json;
using Gardener;
using Gardener.AstExpr;
using Gardener.AstFunction;
using Gardener.AstStmt;
using Karesansui;
using NetTools;
using ZenLib;
using Route = ZenLib.Pair<bool, Gardener.BatfishBgpRoute>;

if (args.Length == 0)
{
  Console.WriteLine("No JSON file provided, exiting now...");
  return;
}

// default export behavior for a route
var defaultExport =
  new AstFunction<Route>("arg",
    new Return<Route>(
      new PairExpr<bool, BatfishBgpRoute, Route>(
        new First<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
        new WithField<BatfishBgpRoute, int, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
          "AsPathLength",
          new Plus<int, Route>(
            new GetField<BatfishBgpRoute, int, Route>(new Second<bool, BatfishBgpRoute, Route>(new Var<Route>("arg")),
              "AsPathLength"), new ConstantExpr<int, Route>(1))))));
// default import behavior for a route
var defaultImport = AstFunction<Route>.Identity();

var destination = IPAddress.Parse("127.0.0.1");

Zen<Route> InitFunction(List<IPAddressRange> prefixes) =>
  Pair.Create<bool, BatfishBgpRoute>(prefixes.Any(range => range.Contains(destination)), new BatfishBgpRoute());

JsonSerializer Serializer()
{
  return new JsonSerializer
  {
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = new AstSerializationBinder<BatfishBgpRoute, Route>()
  };
}

var file = args[0];
var json = new JsonTextReader(new StreamReader(file));
var ast = Serializer().Deserialize<Ast<Route, Unit>>(json);
Console.WriteLine($"Successfully deserialized JSON file {file}");
json.Close();

Console.WriteLine($"Parsed an AST with JSON:");
Console.WriteLine(JsonConvert.SerializeObject(ast));
if (ast != null)
  Profile.RunCmp(ast.ToNetwork(InitFunction, BatfishBgpRouteExtensions.MinPair, defaultExport,
    defaultImport));
else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
