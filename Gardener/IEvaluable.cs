using ZenLib;

namespace Gardener;

public interface IEvaluable<T1, T2>
{
  Func<Zen<T1>, Zen<T2>> Evaluate(State<T1> state);
}

public interface IEvaluable<T1, T2, T3>
{
  Func<Zen<T1>, Zen<T2>, Zen<T3>> Evaluate(State<T1> state);
}
