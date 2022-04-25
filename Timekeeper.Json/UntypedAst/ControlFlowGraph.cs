
using Timekeeper.Json.UntypedAst;

namespace Timekeeper.Json;

public class ControlFlowGraph<T>
{
  public ControlFlowGraph(Statement statement)
  {

  }

  public Dictionary<Statement, List<Statement>> Graph { get; set; }

  private void AddToGraph(Statement statement)
  {
    switch (statement)
    {
      case Assign assign:
        Graph.Add(assign, new List<Statement>());
        break;
      case IfThenElse ifThenElse:
        AddToGraph(ifThenElse.ThenCase);
        AddToGraph(ifThenElse.ElseCase);
        Graph.Add(ifThenElse, new List<Statement> {ifThenElse.ThenCase, ifThenElse.ElseCase});
        break;
      case Return @return:
        Graph.Add(@return, new List<Statement>());
        break;
      case Seq seq:
        AddToGraph(seq.First);
        AddToGraph(seq.Second);
        Graph[seq.First].Add(seq.Second);
        break;
      case Skip:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(statement));
    }
  }
}
