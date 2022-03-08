using Newtonsoft.Json.Serialization;

namespace Gardener;

public class AstBinder : ISerializationBinder
{
  private IDictionary<string, Type> AliasToType { get; }

  public AstBinder()
  {
    AliasToType = new Dictionary<string, Type>
    {
      { "return", typeof(Return<BatfishBgpRoute>) },
      { "assign", typeof(Assign<>) },
      { "seq", typeof(Seq<>) },
      { "if", typeof(IfThenElse<>) },
      { "var", typeof(Var<BatfishBgpRoute>) },
      { "and", typeof(And) },
      { "havoc", typeof(Havoc)},
      { "plus", typeof(Plus<int, ZenLib.Signed>)},
      { "int", typeof(IntExpr<int,ZenLib.Signed>)}
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
