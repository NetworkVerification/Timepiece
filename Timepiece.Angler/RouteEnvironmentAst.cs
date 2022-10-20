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
  /// Set the route as accepted and returned.
  /// </summary>
  private static readonly AstFunction<RouteEnvironment> DefaultImport = new("env", new[]
  {
    new Assign("env", new WithField(new WithField(new Var("env"), "Value", new BoolExpr(true)),
      "Returned", new BoolExpr(true)))
  });

  /// <summary>
  /// Default export behavior for a route.
  /// If external is true, increment the path length.
  /// In either case, set the route as accepted and returned.
  /// </summary>
  private static AstFunction<RouteEnvironment> DefaultExport(bool external)
  {
    const string arg = "env";
    return new AstFunction<RouteEnvironment>(arg, new[]
    {
      new Assign(arg,
        new WithField(
          new WithField(
            // if we are exporting to an external peer, increment the path length here
            external
              ? new WithField(new Var(arg),
                "AsPathLength",
                new Plus(
                  new GetField(typeof(RouteEnvironment), typeof(BigInteger),
                    new Var(arg),
                    "AsPathLength"), new BigIntExpr(BigInteger.One)))
              : new Var(arg),
            "Returned", new BoolExpr(true)),
          "Value", new BoolExpr(true)))
    });
  }

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
}
