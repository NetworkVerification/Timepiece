using Newtonsoft.Json;
using Timekeeper.Datatypes;
using Timekeeper.Json.TypedAst.AstExpr;

namespace Timekeeper.Json.TypedAst;

/// <summary>
/// Constants defined for the given node.
/// Map constant names to their values.
/// </summary>
public class Constants
{
  public readonly IReadOnlyDictionary<string, Expr<string>> stringConstants;
  public readonly IReadOnlyDictionary<string, Expr<Ipv4Prefix>> prefixConstants;

  public Constants()
  {
    stringConstants = new Dictionary<string, Expr<string>>();
    prefixConstants = new Dictionary<string, Expr<Ipv4Prefix>>();
  }

  public Constants(IReadOnlyDictionary<string, object> constants)
  {
    var strings = new Dictionary<string, Expr<string>>();
    var prefixes = new Dictionary<string, Expr<Ipv4Prefix>>();
    foreach (var (name, value) in constants)
    {
      switch (value)
      {
        case Expr<string> expr:
          strings.Add(name, expr);
          break;
        case Expr<Ipv4Prefix> expr:
          prefixes.Add(name, expr);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(constants));
      }
    }

    stringConstants = strings;
    prefixConstants = prefixes;
  }

  public Expr<string> LookupString(string name) => stringConstants[name];
  public Expr<Ipv4Prefix> LookupPrefix(string name) => prefixConstants[name];

  internal Dictionary<string, object> ToHeterogeneousDict()
  {
    var everything = new Dictionary<string, object>();
    foreach (var (key, value) in stringConstants)
    {
      everything.Add(key, value);
    }

    foreach (var (key, value) in prefixConstants)
    {
      everything.Add(key, value);
    }

    return everything;
  }
}

public class ConstantsConverter : JsonConverter<Constants>
{
  public override void WriteJson(JsonWriter writer, Constants? value, JsonSerializer serializer)
  {
    if (value is not null) serializer.Serialize(writer, value.ToHeterogeneousDict());
  }

  public override Constants? ReadJson(JsonReader reader, Type objectType, Constants? existingValue,
    bool hasExistingValue,
    JsonSerializer serializer)
  {
    if (objectType != typeof(Dictionary<string, object>)) throw new JsonSerializationException("Cannot convert a non-dictionary.");
    var d = (Dictionary<string, object>?) reader.Value;
    return d is null ? null : new Constants(d);
  }
}
