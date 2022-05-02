using Newtonsoft.Json;
using Timekeeper.Datatypes;
using Timekeeper.Json.TypedAst.AstExpr;

namespace Timekeeper.Json;

/// <summary>
/// Constants defined for the given node.
/// Map constant names to their values.
/// </summary>
/// <typeparam name="TState">The type of routes.</typeparam>
public class Constants<TState>
{
  public readonly IReadOnlyDictionary<string, Expr<string, TState>> stringConstants;
  public readonly IReadOnlyDictionary<string, Expr<Ipv4Prefix, TState>> prefixConstants;

  public Constants()
  {
    stringConstants = new Dictionary<string, Expr<string, TState>>();
    prefixConstants = new Dictionary<string, Expr<Ipv4Prefix, TState>>();
  }

  [JsonConstructor]
  public Constants(IReadOnlyDictionary<string, object> constants)
  {
    var strings = new Dictionary<string, Expr<string, TState>>();
    var prefixes = new Dictionary<string, Expr<Ipv4Prefix, TState>>();
    foreach (var (name, value) in constants)
    {
      switch (value)
      {
        case Expr<string,TState> expr:
          strings.Add(name, expr);
          break;
        case Expr<Ipv4Prefix,TState> expr:
          prefixes.Add(name, expr);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(constants));
      }
    }

    stringConstants = strings;
    prefixConstants = prefixes;
  }

  public Expr<string, TState> LookupString(string name) => stringConstants[name];
  public Expr<Ipv4Prefix, TState> LookupPrefix(string name) => prefixConstants[name];
}
