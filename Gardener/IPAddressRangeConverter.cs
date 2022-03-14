using NetTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gardener;

public class IpAddressRangeConverter : CustomCreationConverter<IPAddressRange>
{
  public override IPAddressRange Create(Type objectType)
  {
    return new IPAddressRange();
  }

  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(string);
  }

  public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
    JsonSerializer serializer) =>
    IPAddressRange.Parse(serializer.Deserialize<string>(reader));

  public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
    serializer.Serialize(writer, ((IPAddressRange) value).ToString());
}
