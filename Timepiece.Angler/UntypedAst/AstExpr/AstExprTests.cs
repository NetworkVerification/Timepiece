using Xunit;

namespace Timepiece.Angler.UntypedAst.AstExpr;

public static class AstExprTests
{
  [Fact]
  public static void TestEmptyAndFromEnumerator()
  {
    var elements = new Expr[] { };
    var and = new And(elements);
    Assert.Equal(true, ((BoolExpr) and.expr1).value);
    Assert.Equal(true, ((BoolExpr) and.expr2).value);
  }

  [Fact]
  public static void TestSingleAndFromEnumerator()
  {
    var element = new Var("element");
    var and = new And(new Expr[] {element});
    var and2 = (AssociativeBinaryOpExpr) and.expr1;
    Assert.Equal(true, ((BoolExpr) and.expr2).value);
    Assert.Equal(true, ((BoolExpr) and2.expr1).value);
    Assert.Equal(element, and2.expr2);
  }

  [Fact]
  public static void TestDoubleAndFromEnumerator()
  {
    var first = new Var("first");
    var second = new Var("second");
    var and = new And(new Expr[] {first, second});
    var and2 = (AssociativeBinaryOpExpr) and.expr1;
    Assert.Equal(true, ((BoolExpr) and.expr2).value);
    Assert.Equal(second, and2.expr2);
    var and3 = (AssociativeBinaryOpExpr) and2.expr1;
    Assert.Equal(true, ((BoolExpr) and3.expr1).value);
    Assert.Equal(first, and3.expr2);
  }

  [Fact]
  public static void TestTripleAndFromEnumerator()
  {
    var first = new Var("first");
    var second = new Var("second");
    var third = new Var("third");
    var and = new And(new Expr[] {first, second, third});
    var and2 = (AssociativeBinaryOpExpr) and.expr1;
    var and3 = (AssociativeBinaryOpExpr) and2.expr1;
    var and4 = (AssociativeBinaryOpExpr) and3.expr1;
    Assert.Equal(first, and4.expr2);
    Assert.Equal(second, and3.expr2);
    Assert.Equal(third, and2.expr2);
  }
}
