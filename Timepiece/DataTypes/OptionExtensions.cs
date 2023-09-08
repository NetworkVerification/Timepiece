using System;
using ZenLib;

namespace Timepiece.DataTypes;

/// <summary>
///   Extensions to Zen's Option type.
/// </summary>
public static class OptionExtensions
{
  public static void May<T>(this Option<T> o, Action<T> f)
  {
    if (o.HasValue) f(o.Value);
  }

  public static Option<T2> Select<T1, T2>(this Option<T1> o, Func<T1, T2> f)
  {
    return o.HasValue ? Option.Some(f(o.Value)) : Option.None<T2>();
  }

  public static Option<T2> Bind<T1, T2>(this Option<T1> o, Func<T1, Option<T2>> f)
  {
    return o.HasValue ? f(o.Value) : Option.None<T2>();
  }

  public static Option<T> OrElse<T>(this Option<T> o1, Func<Option<T>> f)
  {
    return o1.HasValue ? o1 : f();
  }
}
