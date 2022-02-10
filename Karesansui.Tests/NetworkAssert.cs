using Karesansui.Networks;
using Xunit;
using ZenLib;

namespace Karesansui.Tests;

public static class NetworkAssert
{
  public static void CheckSound<T, TS>(Network<T, TS> net)
  {
    Assert.Equal(Option.None<State<T, TS>>(), net.CheckAnnotations());
  }

  public static void CheckUnsound<T, TS>(Network<T, TS> net)
  {
    Assert.True(net.CheckAnnotations().HasValue);
  }
}
