using ZenLib;

namespace Timekeeper.Json.TypedAst;

public interface IEvaluable<T1, T2> : IEvaluated<T1>
{
  public Func<Zen<T1>, Zen<T2>> Evaluate(AstState astState);
}

public interface IEvaluatesTo<T2>
{
  public IEvaluable<T1, T2> Evaluate<T1>(AstState astState);
}

public interface IEvaluated<T1>
{
  public Func<Zen<T1>, Zen<T2>> Evaluate<T2>(AstState astState);
}
