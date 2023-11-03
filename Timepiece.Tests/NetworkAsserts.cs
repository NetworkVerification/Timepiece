using System;
using Timepiece.Networks;
using Xunit;
using ZenLib;

namespace Timepiece.Tests;

/// <summary>
///   Helper assertions for checking annotated networks.
/// </summary>
public static class NetworkAsserts
{
  /// <summary>
  ///   Assert that the network checks hold.
  /// </summary>
  /// <param name="net">The annotated network.</param>
  /// <param name="check">A specific check to perform; if not given, performs all modular checks.</param>
  /// <param name="node">A specific node to check; if not given, check all nodes.</param>
  /// <typeparam name="RouteType">The type of routes.</typeparam>
  /// <typeparam name="NodeType">The type of nodes.</typeparam>
  /// <exception cref="ArgumentOutOfRangeException">If an invalid check is specified.</exception>
  public static void Sound<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net,
    SmtCheck check = SmtCheck.Modular, NodeType? node = default)
    where NodeType : notnull
  {
    var expected = Option.None<State<RouteType, NodeType>>();
    Assert.Equal(expected, check switch
    {
      SmtCheck.Monolithic => node is null
        ? net.CheckMonolithic()
        : throw new ArgumentException("Monolithic check cannot be performed at a particular node!"),
      SmtCheck.Initial => node is null ? net.CheckInitial() : net.CheckInitial(node),
      SmtCheck.Inductive => node is null ? net.CheckInductive() : net.CheckInductive(node),
      SmtCheck.InductiveDelayed => node is null
        ? net.CheckInductiveDelayed()
        : throw new NotImplementedException("Inductive delayed check not possible at a specific node."),
      SmtCheck.Safety => node is null ? net.CheckSafety() : net.CheckSafety(node),
      SmtCheck.Modular => node is null ? net.CheckAnnotations() : net.CheckAnnotations(node),
      SmtCheck.ModularDelayed => node is null
        ? net.CheckAnnotationsDelayed()
        : throw new NotImplementedException("Inductive delayed check not possible at a specific node."),
      _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
    });
  }

  /// <summary>
  ///   Assert that the network checks <i>fail to</i> hold.
  /// </summary>
  /// <param name="net">The annotated network.</param>
  /// <param name="check">A specific check to perform; if not given, performs all modular checks.</param>
  /// <typeparam name="RouteType">The type of routes.</typeparam>
  /// <typeparam name="NodeType">The type of nodes.</typeparam>
  /// <exception cref="ArgumentOutOfRangeException">If an invalid check is specified.</exception>
  public static void Unsound<RouteType, NodeType>(AnnotatedNetwork<RouteType, NodeType> net,
    SmtCheck check = SmtCheck.Modular)
    where NodeType : notnull
  {
    Assert.True(check switch
    {
      SmtCheck.Monolithic => net.CheckMonolithic().HasValue,
      SmtCheck.Initial => net.CheckInitial().HasValue,
      SmtCheck.Inductive => net.CheckInductive().HasValue,
      SmtCheck.Safety => net.CheckSafety().HasValue,
      SmtCheck.InductiveDelayed => net.CheckInductiveDelayed().HasValue,
      SmtCheck.Modular => net.CheckAnnotations().HasValue,
      SmtCheck.ModularDelayed => net.CheckAnnotationsDelayed().HasValue,
      _ => throw new ArgumentOutOfRangeException(nameof(check), check, null)
    });
  }
}
