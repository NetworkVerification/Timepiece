using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Timepiece.Angler.Ast;

/// <summary>
/// Implementation of <c>ISerializationBinder</c> for the Angler AST.
/// Type binding defers to <see cref="TypeParsing"/>.
/// </summary>
public class AstSerializationBinder : ISerializationBinder
{
  public Type BindToType(string? assemblyName, string typeName) => TypeParsing.ParseType(typeName).MakeType();

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }

  /// <summary>
  /// Return a <c>JsonSerializer</c> using this binder.
  /// </summary>
  /// <returns></returns>
  public static JsonSerializer JsonSerializer() => new()
  {
    // use $type for type names, and the given binder
    TypeNameHandling = TypeNameHandling.All,
    SerializationBinder = new AstSerializationBinder()
    // uncomment to throw an error when members are missing from the object instead of ignoring them
    // MissingMemberHandling = MissingMemberHandling.Error
  };
}
