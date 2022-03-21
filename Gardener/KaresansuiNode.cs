using System.Collections.Immutable;
using System.Numerics;
using ZenLib;

namespace Gardener;

/// <summary>
/// An immutable struct storing all information needed from a given AST node to produce a Karesansui network.
/// </summary>
public readonly struct KaresansuiNode<T>
{
  public readonly Zen<T> initialValue;
  public readonly Func<Zen<T>, Zen<bool>> safetyProperty;
  public readonly Func<Zen<T>, Zen<BigInteger>, Zen<bool>> annotation;
  public readonly ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> imports;
  public readonly ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> exports;

  public KaresansuiNode(Zen<T> initialValue, Func<Zen<T>, Zen<bool>> safetyProperty,
    Func<Zen<T>, Zen<BigInteger>, Zen<bool>> annotation, ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> imports,
    ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> exports)
  {
    this.initialValue = initialValue;
    this.safetyProperty = safetyProperty;
    this.annotation = annotation;
    this.imports = imports;
    this.exports = exports;
  }
}
