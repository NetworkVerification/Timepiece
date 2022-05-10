using Newtonsoft.Json.Serialization;
using Timepiece.Angler.TypedAst.AstFunction;

namespace Timepiece.Angler.UntypedAst;

public class AstSerializationBinder<TState> : ISerializationBinder
{
  private static readonly Type State = typeof(TState);

  public Type BindToType(string? assemblyName, string typeName)
  {
    return typeName switch
    {
      "Finally" => typeof(Finally<>).MakeGenericType(State),
      "Globally" => typeof(Globally<>).MakeGenericType(State),
      "Until" => typeof(Until<>).MakeGenericType(State),
      _ => TypeParsing.ParseType(typeName).MakeType()
    };
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }
}
