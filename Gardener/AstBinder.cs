using Newtonsoft.Json.Serialization;

namespace Gardener;

public class AstBinder : ISerializationBinder
{
  private IDictionary<string, Type> AliasToType { get; }

  public AstBinder()
  {
    AliasToType = new Dictionary<string, Type>
    {
      { "ReturnStatement[Pair[...]]", typeof(Return<>) },
      { "AssignStatement", typeof(Assign<>) },
      { "SeqStatement", typeof(Seq<,>) },
      { "IfStatement", typeof(IfThenElse<,>) },
      { "Var", typeof(Var<BatfishBgpRoute>) },
      { "And", typeof(And<BatfishBgpRoute>) },
      { "Havoc", typeof(Havoc<BatfishBgpRoute>)},
      { "Plus", typeof(Plus<int, ZenLib.Signed, BatfishBgpRoute>)},
      { "Int", typeof(IntExpr<int,ZenLib.Signed, BatfishBgpRoute>)}
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
