using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gardener;

public class EdgesJsonConverter : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    if (!typeToConvert.IsGenericType)
    {
      return false;
    }

    if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>))
    {
      return false;
    }

    return typeToConvert.GetGenericArguments()[0] == typeof(string);
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var valueType = typeToConvert.GetGenericArguments()[0];

    var converter = (JsonConverter) Activator.CreateInstance(
      typeof(EdgesConverterInner<>).MakeGenericType(valueType),
      BindingFlags.Instance | BindingFlags.Public,
      binder: null,
      args: new object[] {options},
      culture: null)!;

    return converter;
  }

  private class EdgesConverterInner<TValue> : JsonConverter<Dictionary<(string, string), TValue>>
  {
    private readonly JsonConverter<TValue> _valueConverter;
    private readonly Type _valueType;

    public EdgesConverterInner(JsonSerializerOptions options)
    {
      // For performance, use the existing converter if available.
      _valueConverter = (JsonConverter<TValue>) options
        .GetConverter(typeof(TValue));

      // Cache the value type.
      _valueType = typeof(TValue);
    }

    private static (string, string) ParseEdge(string edge)
    {
      var nodes = edge.Split(",");
      if (nodes.Length != 2)
      {
        throw new JsonException($"Unable to convert {edge} to a pair of nodes (splitting on ',').");
      }

      return (nodes[0], nodes[1]);
    }

    public override Dictionary<(string, string), TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert,
      JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        throw new JsonException();
      }

      var dictionary = new Dictionary<(string, string), TValue>();

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject)
        {
          return dictionary;
        }

        // Get the key.
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
          throw new JsonException();
        }

        var propertyName = reader.GetString()!;

        var key = ParseEdge(propertyName);

        // Get the value.
        TValue value;
        if (_valueConverter != null)
        {
          reader.Read();
          value = _valueConverter.Read(ref reader, _valueType, options)!;
        }
        else
        {
          value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        }

        // Add to dictionary.
        dictionary.Add(key, value);
      }

      throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<(string, string), TValue> value,
      JsonSerializerOptions options)
    {
      throw new NotImplementedException();
    }
  }
}
