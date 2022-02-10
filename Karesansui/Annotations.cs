using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ZenLib;

namespace Karesansui;

public class Annotations<T>
{
  /// <summary>
  ///   Mapping from node names to annotations.
  /// </summary>
  public Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations;

  // TODO: change to a parser
  public Annotations(Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations)
  {
    this.annotations = annotations;
  }

  public override string ToString()
  {
    var sb = new StringBuilder();
    foreach (var (node, annotation) in annotations)
      // FIXME: Zen funcs don't actually print in any helpful user-readable way
      sb.Append($"A({node}, t) = {annotation}").AppendLine();

    return sb.ToString();
  }
}
