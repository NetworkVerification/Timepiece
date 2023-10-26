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

  public TransferResult<RouteType>? Check(Zen<RouteType> route, Func<Zen<RouteType>, Zen<bool>> assumption,
    Func<Zen<RouteType>, Zen<bool>> guarantee)
  {
    var transferred = Transfer(route);
    var model = Zen.And(assumption(route), Zen.Not(guarantee(transferred))).Solve();
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
