using System;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

public static class NetworkAssert
{
  public static void CheckSound<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.Equal(Option.None<State<T, TV, TS>>(), net.CheckAnnotations());
  }

  public static void CheckSoundMonolithic<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.Equal(Option.None<State<T, TV, TS>>(), net.CheckMonolithic());
  }

  public static void CheckUnsound<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net)
  {
    Assert.True(net.CheckAnnotations().HasValue);
  }

  public static void CheckUnsoundCheck<T, TV, TS>(AnnotatedNetwork<T, TV, TS> net, SmtCheck check)
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
