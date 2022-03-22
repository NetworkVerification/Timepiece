using Newtonsoft.Json;
using Gardener;
using Gardener.AstExpr;
using Gardener.AstFunction;
using Gardener.AstStmt;
using Karesansui;
using ZenLib;
using Route = ZenLib.Pair<bool, Gardener.BatfishBgpRoute>;

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

Zen<Route> InitFunction(bool isDestination) =>
  Pair.Create<bool, BatfishBgpRoute>(isDestination, new BatfishBgpRoute());

JsonSerializer Serializer()
{
  return new JsonSerializer
  {
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = new AstSerializationBinder<BatfishBgpRoute, Route>()
  };
}

foreach (var file in args)
{
  var json = new JsonTextReader(new StreamReader(file));
  var ast = Serializer().Deserialize<Ast<Route, Unit>>(json);
  Console.WriteLine($"Successfully deserialized JSON file {file}");
  json.Close();
  if (ast != null)
    Profile.RunCmp(ast.ToNetwork(InitFunction, BatfishBgpRouteExtensions.MinPair, defaultExport,
      defaultImport));
  else Console.WriteLine("Failed to deserialize contents of {file} (received null).");
}
