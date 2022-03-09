using Newtonsoft.Json.Serialization;

namespace Gardener;

public class AstBinder : ISerializationBinder
{
  private IDictionary<string, Type> AliasToType { get; }

  public AstBinder()
  {
    AliasToType = new Dictionary<string, Type>
    {
      { "ReturnStatement", typeof(Return<object>) },
      { "AssignStatement", typeof(Assign<object>) },
      { "SeqStatement", typeof(Seq<object>) },
      { "IfStatement", typeof(IfThenElse<object>) },
      { "Var", typeof(Var<>) },
      { "And", typeof(And) },
      { "Havoc", typeof(Havoc)},
      { "Plus", typeof(Plus<int, ZenLib.Signed>)},
      { "Int", typeof(IntExpr<int,ZenLib.Signed>)}
    };
  }
  public Type BindToType(string? assemblyName, string typeName)
  {
    return AliasToType[typeName];
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }
}
