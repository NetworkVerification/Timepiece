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
    var checkAnnotationsDelayed = net.Check(check, node);
    Assert.Equal(expected, checkAnnotationsDelayed);
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
    Assert.True(net.Check(check).HasValue);
  }
}
