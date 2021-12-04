using System;
using System.Collections.Generic;
using System.Linq;
using ZenLib;

namespace ZenDemo;

public class TotalMap<TKey, TValue> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     Construct a new TotalMap over the given (key,value) pairs.
    /// </summary>
    /// <param name="members">An enumerable of (key,value) pairs.</param>
    public TotalMap(IEnumerable<Pair<TKey, TValue>> members)
    {
        Members = members.ToList();
    }

    /// <summary>
    ///     Allowed members of the enumeration.
    /// </summary>
    public IList<Pair<TKey, TValue>> Members { get; set; }

    // private TValue _defaultValue;

    public TotalMap<TKey, TValue> Empty(IEnumerable<TKey> keys, TValue defaultValue)
    {
        Members = keys.Select(k => new Pair<TKey, TValue>
            {
                Item1 = k,
                Item2 = defaultValue
            }).ToList();
        // _defaultValue = defaultValue;
        return this;
    }

    /// <summary>
    ///     Return the number of members of the map.
    /// </summary>
    /// <returns>An int representing the number of members.</returns>
    public int Cardinality()
    {
        return Members.Count;
    }

    public TValue Lookup(TKey key)
    {
        return Members[IndexOf(key)].Item2;
    }

    /// <summary>
    ///     Return a new TotalMap with the given key remapped to the
    ///     value val.
    ///     Does nothing if the given key is not found.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public TotalMap<TKey, TValue> Update(TKey key, TValue val)
    {
        return new TotalMap<TKey, TValue>(Members.Select(kv =>
            kv.Item1.Equals(key) ? (kv.Item1, val) : kv));
    }

    /// <summary>
    ///     Return the index associated with the given key.
    ///     Throw an exception if the key is not found.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private int IndexOf(TKey key)
    {
        for (var i = 0; i < Cardinality(); i++)
            if (Members[i].Item1.Equals(key))
                return i;
        throw new IndexOutOfRangeException($"Key {key} not found in map.");
    }
}

public static class DictExtensions
{

    /// <summary>
    /// Return the Members list of a TotalMap.
    /// </summary>
    /// <param name="m">The TotalMap.</param>
    /// <typeparam name="TKey">The type of the TotalMap's keys.</typeparam>
    /// <typeparam name="TValue">The type of the TotalMap's values.</typeparam>
    /// <returns>The internal list of key-value pairs.</returns>
    public static Zen<IList<Pair<TKey, TValue>>> Values<TKey, TValue>(this Zen<Dict<TKey, TValue>> m)
    {
        return m.GetField<Dict<TKey, TValue>, IList<Pair<TKey, TValue>>>("Values");
    }
    public static Zen<Dict<TKey, TValue2>> ForEach<TKey, TValue, TValue2>(this Zen<Dict<TKey, TValue>> m,
        Func<Zen<TValue>, Zen<TValue2>> f)
    {
        var members = m.Values().Select(Lang.Product(Lang.Identity<TKey>(), f));
        return Language.Create<Dict<TKey, TValue2>>(("Values", members));
    }

    public static Zen<Dict<TKey, TValue3>> Zip<TKey, TValue1, TValue2, TValue3>(this Zen<Dict<TKey, TValue1>> m1,
        Zen<Dict<TKey, TValue2>> m2, Func<Zen<TValue1>, Zen<TValue2>, Zen<TValue3>> f)
    {
        var members1 = m1.Values();
        var members2 = m2.Values();
        return Language.Create<Dict<TKey, TValue3>>(("Values", members1.Zip(members2, f)));
    }

    private static Zen<IList<Pair<TKey, TValue3>>> Zip<TKey, TValue1, TValue2, TValue3>(this Zen<IList<Pair<TKey, TValue1>>> m1,
        Zen<IList<Pair<TKey, TValue2>>> m2, Func<Zen<TValue1>, Zen<TValue2>, Zen<TValue3>> f)
    {
        return m1.Case(
            empty: Language.EmptyList<Pair<TKey, TValue3>>(),
            cons: (hd1, tl1) =>
            {
                return m2.Case(
                    empty: Language.EmptyList<Pair<TKey, TValue3>>(),
                    cons: (hd2, tl2) =>
                    {
                        // NOTE: assumes the keys are the same!
                        var hd = Lang.Product2<TKey, TValue1, TKey, TValue2, TKey, TValue3>((k1, _) => k1, f)(hd1, hd2);
                        return tl1.Zip(tl2, f).AddFront(hd);
                    });
            });
    }

    public static Zen<bool> All<TKey, TValue>(this Zen<Dict<TKey, TValue>> m,
        Func<Zen<Pair<TKey, TValue>>, Zen<bool>> f)
    {
        return m.Values().All(f);
    }

    public static Zen<TAcc> Fold<TKey, TValue, TAcc>(this Zen<Dict<TKey, TValue>> m, Zen<TAcc> initialValue,
        Func<Zen<Pair<TKey, TValue>>, Zen<TAcc>, Zen<TAcc>> f)
    {
        return m.Values().Fold(initialValue, f);
    }
}