using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenDemo;

public class TotalMap<TKey, TValue> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     Construct a new TotalMap over the given (key,value) pairs.
    /// </summary>
    /// <param name="members">An enumerable of (key,value) pairs.</param>
    public TotalMap(IEnumerable<(TKey, TValue)> members)
    {
        Members = members.ToList();
    }

    /// <summary>
    ///     Allowed members of the enumeration.
    /// </summary>
    public IList<(TKey, TValue)> Members { get; set; }

    // private TValue _defaultValue;

    public TotalMap<TKey, TValue> Empty(IEnumerable<TKey> keys, TValue defaultValue)
    {
        Members = keys.Select(k => (k, defaultValue)).ToList();
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

    public TotalMap<TKey, TValue2> ForEach<TValue2>(Func<TKey, TValue, TValue2> f)
    {
        return new TotalMap<TKey, TValue2>(Members.Select(kv => (kv.Item1, f(kv.Item1, kv.Item2))));
    }

    public TotalMap<TKey, TValue3> Zip<TValue2, TValue3>(Func<TValue, TValue2, TValue3> f,
        TotalMap<TKey, TValue2> other)
    {
        return new TotalMap<TKey, TValue3>(Members.Zip(other.Members,
            (kv1, kv2) => (kv1.Item1, f(kv1.Item2, kv2.Item2))));
    }

    public TAccumulator Aggregate<TAccumulator>(TAccumulator initialValue,
        Func<TAccumulator, (TKey, TValue), TAccumulator> f)
    {
        return Members.Aggregate(initialValue, f);
    }
}