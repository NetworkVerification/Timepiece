using System.Collections.Immutable;
using ZenLib;

namespace Timepiece.Angler.Ast;

/// <summary>
///   An immutable struct storing all information needed from a given AST node to produce a network.
/// </summary>
public readonly struct NetworkNode<T>
{
  public readonly ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> imports;
  public readonly ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> exports;

  public NetworkNode(ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> imports,
    ImmutableDictionary<string, Func<Zen<T>, Zen<T>>> exports)
  {
    this.imports = imports;
    this.exports = exports;
  }
}
