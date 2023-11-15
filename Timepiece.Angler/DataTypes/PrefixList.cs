using Newtonsoft.Json;
using Timepiece.DataTypes;

namespace Timepiece.Angler.DataTypes;

public record PrefixList(string Name, List<Ipv4Prefix> Prefixes)
{
  [JsonProperty("List")] public string Name { get; set; } = Name;
  public List<Ipv4Prefix> Prefixes { get; set; } = Prefixes;
}

public static class PrefixListExtensions
{
  public static IEnumerable<PrefixList>? DeserializePrefixes(string fileName)
  {
    var reader = new JsonTextReader(new StreamReader(fileName));
    return JsonSerializer.Create().Deserialize<IEnumerable<PrefixList>>(reader);
  }
}
