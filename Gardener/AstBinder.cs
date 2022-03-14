using Gardener.AstExpr;
using Gardener.AstStmt;
using Newtonsoft.Json.Serialization;
using ZenLib;

namespace Gardener;

public class AstBinder : ISerializationBinder
{
  private IDictionary<string, Type> AliasToType { get; }

  public AstBinder()
  {
    AliasToType = new Dictionary<string, Type>
    {
      // {"Return(Pair(Bool;Var(Route)))", typeof(Return<AstExpr.Pair<bool, BatfishBgpRoute, BatfishBgpRoute>>)},
      {"Return(Var(Route))", typeof(Return<BatfishBgpRoute>)},
      {"Assign", typeof(Assign<>)},
      {"Seq", typeof(Seq<,>)},
      {"If", typeof(IfThenElse<,>)},
      {"Var(Route)", typeof(Var<BatfishBgpRoute>)},
      {"True", typeof(ConstantExpr<bool, BatfishBgpRoute>)},
      {"False", typeof(ConstantExpr<bool, BatfishBgpRoute>)},
      {"And", typeof(And<BatfishBgpRoute>)},
      {"Or", typeof(Or<BatfishBgpRoute>)},
      {"Not", typeof(Not<BatfishBgpRoute>)},
      {"Havoc", typeof(Havoc<BatfishBgpRoute>)},
      {"Int32", typeof(ConstantExpr<int, BatfishBgpRoute>)},
      {"Plus32", typeof(Plus<int, BatfishBgpRoute>)},
      // {"Pair(Bool;Var(Route))", typeof(AstExpr.Pair<bool, BatfishBgpRoute, BatfishBgpRoute>)},
      {"GetField(Var(Route);Int32)", typeof(GetField<BatfishBgpRoute, int, BatfishBgpRoute>)},
      {"GetField(Var(Route);Bool)", typeof(GetField<BatfishBgpRoute, bool, BatfishBgpRoute>)},
    };
  }

  public Type BindToType(string? assemblyName, string typeName)
  {
    // TODO: split up typeName and parse in sections?
    return AliasToType[typeName];
  }

  public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
  {
    assemblyName = null;
    typeName = serializedType.Name;
  }
}
