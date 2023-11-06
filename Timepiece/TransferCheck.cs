#nullable enable
using System;
using ZenLib;

namespace Timepiece;

public class TransferCheck<RouteType> where RouteType : notnull
{
  public Func<Zen<RouteType>, Zen<RouteType>> Transfer { get; set; }

  public TransferCheck(Func<Zen<RouteType>, Zen<RouteType>> transfer)
  {
    Transfer = transfer;
  }

  /// <summary>
  /// Verify that for all routes <paramref name="route"/> satisfying the assumption <paramref name="assumption"/>,
  /// the transfer function produces a route such that the guarantee <paramref name="guarantee"/> holds.
  /// Returns a counterexample if there exists a route that violates the guarantee, and null otherwise.
  /// </summary>
  /// <param name="route"></param>
  /// <param name="assumption"></param>
  /// <param name="guarantee"></param>
  /// <returns></returns>
  public TransferResult<RouteType>? Verify(Zen<RouteType> route, Func<Zen<RouteType>, Zen<bool>> assumption,
    Func<Zen<RouteType>, Zen<bool>> guarantee)
    => Solve(route, assumption, r => Zen.Not(guarantee(r)));

  /// <summary>
  /// Check that if the assumption <paramref name="assumption"/> holds on the given <paramref name="route"/>,
  /// then the transfer function produces an output route such that the <paramref name="guarantee"/> holds on that output.
  /// Returns the (input,output) route pair if it exists, and null otherwise.
  /// </summary>
  /// <param name="route"></param>
  /// <param name="assumption"></param>
  /// <param name="guarantee"></param>
  /// <returns></returns>
  public TransferResult<RouteType>? Solve(Zen<RouteType> route, Func<Zen<RouteType>, Zen<bool>> assumption,
    Func<Zen<RouteType>, Zen<bool>> guarantee)
  {
    var transferred = Transfer(route);
    var model = Zen.And(assumption(route), guarantee(transferred)).Solve();
    return model.IsSatisfiable()
      ? new TransferResult<RouteType>(model.Get(route), model.Get(transferred))
      : null;
  }
}

public record TransferResult<RouteType>(RouteType route, RouteType result)
  where RouteType : notnull
{
  public readonly RouteType route = route;
  public readonly RouteType result = result;

  public override string ToString()
  {
    return $"TransferResult({route} --> {result})";
  }
}
