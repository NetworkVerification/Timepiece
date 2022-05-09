using QuikGraph;
using QuikGraph.Collections;

namespace Timepiece.Angler.UntypedAst;

/// <summary>
/// A CFG representing the statements as blocks.
/// </summary>
public class ControlFlowGraph
{
  private const int Entry = 0;
  private const int Exit = -1;
  private int _blockNumber = Entry;

  public ControlFlowGraph()
  {
    Graph = new BidirectionalGraph<int, Edge<int>>();
    Graph.AddVertex(Entry);
    Graph.AddVertex(Exit);
    Blocks = new Dictionary<int, List<Statement>>();
  }

  public BidirectionalGraph<int, Edge<int>> Graph { get; set; }
  public Dictionary<int, List<Statement>> Blocks { get; set; }

  /// <summary>
  /// Add a function to the CFG.
  /// </summary>
  /// <param name="statements">The function's body.</param>
  /// <exception cref="ArgumentOutOfRangeException">If the function contains an unrecognized statement.</exception>
  public void AddFunction(IEnumerable<Statement> statements)
  {
    // add a link from the previous block to this new one
    Graph.AddEdge(new Edge<int>(_blockNumber, ++_blockNumber));
    // begin filling in a new block
    Blocks[_blockNumber] = new List<Statement>();
    foreach (var statement in statements)
    {
      switch (statement)
      {
        case Assign assign:
          Blocks[_blockNumber].Add(assign);
          break;
        case IfThenElse ifThenElse:
          // the if is added to the current block,
          // and each child gets a new block
          Blocks[_blockNumber].Add(ifThenElse);
          if (ifThenElse.ThenCase.Count > 0)
          {
            // do then block
            AddFunction(ifThenElse.ThenCase);
          }

          if (ifThenElse.ElseCase.Count > 0)
          {
            // do else block
            AddFunction(ifThenElse.ThenCase);
          }

          break;
        case Return @return:
          Blocks[_blockNumber].Add(@return);
          Graph.AddEdge(new Edge<int>(_blockNumber, Exit));
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(statements));
      }
    }
  }

  /// <summary>
  /// Construct the dominator tree for the CFG.
  /// </summary>
  /// <returns></returns>
  public Dictionary<int, List<int>> DominatorTree()
  {
    var dominators = new ForestDisjointSet<int>();
    foreach (var v in Graph.Vertices)
    {
      dominators.MakeSet(v);
    }

    // TODO: union dominators
    throw new NotImplementedException();
  }
}
