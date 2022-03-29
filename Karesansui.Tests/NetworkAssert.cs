using System;
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

  public static void CheckSoundMonolithic<T, TS>(Network<T, TS> net)
  {
    Assert.Equal(Option.None<State<T, TS>>(), net.CheckMonolithic());
  }

  public static void CheckUnsound<T, TS>(Network<T, TS> net)
  {
    Assert.True(net.CheckAnnotations().HasValue);
  }

  public static void CheckUnsoundCheck<T, TS>(Network<T, TS> net, SmtCheck check)
  {
    Assert.True(check switch
    {
      SmtCheck.Monolithic => net.CheckMonolithic().HasValue,
      SmtCheck.Base => net.CheckBaseCase().HasValue,
      SmtCheck.Inductive => net.CheckInductive().HasValue,
      SmtCheck.Safety => net.CheckAssertions().HasValue,
      _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
    });
  }
}
