namespace Gardener;

public static class VarCounter
{
  private static uint _count;

  /// <summary>
  /// Request a number from the counter.
  /// </summary>
  /// <returns></returns>
  public static uint Request()
  {
    return _count++;
  }
}