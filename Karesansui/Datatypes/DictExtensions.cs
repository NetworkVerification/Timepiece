using System;
using System.Collections.Generic;
using ZenLib;

namespace Karesansui.Datatypes;

public static class DictExtensions
{
  /// <summary>
  ///     Return the Values of a Dict.
  /// </summary>
  /// <param name="m">The Dict.</param>
  /// <typeparam name="TKey">The type of the Dict's keys.</typeparam>
  /// <typeparam name="TValue">The type of the Dict's values.</typeparam>
  /// <returns>The internal list of key-value pairs.</returns>
  public static Zen<FSeq<Pair<TKey, TValue>>> Values<TKey, TValue>(this Zen<FMap<TKey, TValue>> m)
  {
    return m.GetField<FMap<TKey, TValue>, FSeq<Pair<TKey, TValue>>>("Values");
  }

  public static Zen<FMap<TKey, TValue2>> ForEach<TKey, TValue, TValue2>(this Zen<FMap<TKey, TValue>> m,
    Func<Zen<TValue>, Zen<TValue2>> f)
  {
    var members = m.Values().Select(Lang.Product(Lang.Identity<TKey>(), f));
    return Zen.Create<FMap<TKey, TValue2>>(("Values", members));
  }

  public static Zen<FMap<TKey, TValue3>> Zip<TKey, TValue1, TValue2, TValue3>(this Zen<FMap<TKey, TValue1>> m1,
    Zen<FMap<TKey, TValue2>> m2, Func<Zen<TValue1>, Zen<TValue2>, Zen<TValue3>> f)
  {
    var members1 = m1.Values();
    var members2 = m2.Values();
    return Zen.Create<FMap<TKey, TValue3>>(("Values", members1.Zip(members2, f)));
  }

  /// <summary>
  ///     Zips the values of two key-value lists together.
  ///     Behavior is undefined if the two lists don't have the same keys!
  /// </summary>
  /// <param name="m1">The first list of key-value pairs.</param>
  /// <param name="m2">The second list of key-value pairs.</param>
  /// <param name="f">The zipping function between the two lists.</param>
  /// <typeparam name="TKey">The type of the keys.</typeparam>
  /// <typeparam name="TValue1">The type of the first list's values.</typeparam>
  /// <typeparam name="TValue2">The type of the second list's values.</typeparam>
  /// <typeparam name="TValueResult">The type of the resulting list's values.</typeparam>
  /// <returns>The resulting list of key-value pairs.</returns>
  private static Zen<FSeq<Pair<TKey, TValueResult>>> Zip<TKey, TValue1, TValue2, TValueResult>(
    this Zen<FSeq<Pair<TKey, TValue1>>> m1,
    Zen<FSeq<Pair<TKey, TValue2>>> m2, Func<Zen<TValue1>, Zen<TValue2>, Zen<TValueResult>> f)
  {
    return m1.Case(
      FSeq.Empty<Pair<TKey, TValueResult>>(),
      (hd1, tl1) =>
      {
        return m2.Case(
          FSeq.Empty<Pair<TKey, TValueResult>>(),
          (hd2, tl2) =>
          {
            // NOTE: assumes the keys are the same!
            var hd = Lang.Product2<TKey, TValue1, TKey, TValue2, TKey, TValueResult>((k1, _) => k1, f)(hd1, hd2);
            return tl1.Zip(tl2, f).AddFront(hd);
          });
      });
  }

  public static Zen<bool> All<TKey, TValue>(this Zen<FMap<TKey, TValue>> m,
    Func<Zen<Pair<TKey, TValue>>, Zen<bool>> f)
  {
    return m.Values().All(f);
  }

  public static Zen<TAcc> Fold<TKey, TValue, TAcc>(this Zen<FMap<TKey, TValue>> m, Zen<TAcc> initialValue,
    Func<Zen<Pair<TKey, TValue>>, Zen<TAcc>, Zen<TAcc>> f)
  {
    return m.Values().Fold(initialValue, f);
  }

  public static Zen<bool> Compare<TKey, TValue>(this Zen<FMap<TKey, TValue>> m1,
    Zen<FMap<TKey, TValue>> m2, Func<Zen<TValue>, Zen<TValue>, Zen<bool>> cmpf)
  {
    return m1.Zip(m2, cmpf).Values().All(p => p.Item2());
  }
}
