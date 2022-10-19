using System.Numerics;
using Newtonsoft.Json.Serialization;
using Timepiece.Angler.UntypedAst;
using Timepiece.Angler.UntypedAst.AstExpr;
using Timepiece.Angler.UntypedAst.AstFunction;
using Timepiece.Angler.UntypedAst.AstStmt;
using Timepiece.Datatypes;
using Timepiece.Networks;

namespace Timepiece.Angler;

public class RouteEnvironmentAst : Ast
{
  /// <summary>
  /// Default predicates to test for this AST.
  /// </summary>
  public static readonly AstPredicate IsValid = new("route",
    new GetField(typeof(RouteEnvironment), typeof(bool), new Var("route"), "Value"));

  /// <summary>
  /// Default import behavior for a route.
  /// </summary>
  private static readonly AstFunction<RouteEnvironment> DefaultImport = AstFunction<RouteEnvironment>.Identity();

  /// <summary>
  /// Default export behavior for a route.
  /// Increment the path length and set it as returned.
  /// </summary>
  private static readonly AstFunction<RouteEnvironment> DefaultExport = new("arg", new[]
  {
    new Assign("arg",
      new WithField(
        new WithField(
          new WithField(new Var("arg"),
            "AsPathLength",
            new Plus(
              new GetField(typeof(RouteEnvironment), typeof(BigInteger),
                new Var("arg"),
                "AsPathLength"), new BigIntExpr(BigInteger.One))),
          "Returned", new BoolExpr(true)),
        "Value", new BoolExpr(true)))
  });

  public RouteEnvironmentAst(Dictionary<string, NodeProperties> nodes,
    Ipv4Prefix? destination,
    Dictionary<string, AstPredicate> predicates, Dictionary<string, AstPredicate> symbolics,
    BigInteger? convergeTime) : base(nodes,
    symbolics, predicates, destination, convergeTime)
  {
  }

  public Network<RouteEnvironment, RouteEnvironment> ToNetwork()
  {
    return ToNetwork(RouteEnvironmentExtensions.MinOptional, DefaultExport, DefaultImport);
  }

  public static ISerializationBinder Binder()
  {
    return new AstSerializationBinder();
  }

  public static IContractResolver Resolver()
  {
    return new AstContractResolver();
  }
}
