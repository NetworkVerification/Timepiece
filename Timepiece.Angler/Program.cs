using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json;
using Timepiece;
using Timepiece.Angler.Ast;
using Timepiece.Angler.DataTypes;
using Timepiece.Angler.Specifications;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

var rootCommand = new RootCommand("Timepiece benchmark runner");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically (simulating Minesweeper)");
var queryOption = new System.CommandLine.Option<bool>(
  new[] {"--query", "-q"},
  "If given, print the query formulas to stdout");
var trackTermsOption = new System.CommandLine.Option<bool>(
  new[] {"--track-terms", "-t"},
  "If given, turn on tracking of the visited terms of a route.");
var fileArgument = new Argument<string>(
  "file",
  "The .angler.json file to use");
var queryArgument =
  new Argument<Specification>("query",
    description: "The type of query to check",
    parse: result => SpecificationExtensions.Parse(result.Tokens.Single().Value));
rootCommand.Add(fileArgument);
rootCommand.Add(queryArgument);
rootCommand.Add(monoOption);
rootCommand.Add(queryOption);
rootCommand.Add(trackTermsOption);
rootCommand.SetHandler(
  (file, queryType, mono, printQuery, trackTerms) =>
  {
    var json = new JsonTextReader(new StreamReader(file));

    var ast = AstSerializationBinder.JsonSerializer().Deserialize<AnglerNetwork>(json);

    Console.WriteLine($"Successfully deserialized JSON file {file}");
    Debug.WriteLine("Running in debug mode...");
    Debug.WriteLine("Warning: additional assertions in debug mode may substantially slow running time!");
    json.Close();
    if (ast != null)
    {
      var (topology, transfer) = ast.TopologyAndTransfer(trackTerms: trackTerms);
      var externalNodes = ast.Externals.Select(i => i.Name).ToArray();
      dynamic net = queryType switch
      {
        Specification.Internet2BlockToExternal => Internet2Specification.BlockToExternal(topology, externalNodes,
          transfer),
        Specification.Internet2NoMartians => Internet2Specification.NoMartians(topology, externalNodes, transfer),
        Specification.Internet2NoPrivateAs => Internet2Specification.NoPrivateAs(topology, externalNodes, transfer),
        Specification.Internet2Reachable => Internet2Specification.Reachable(topology, externalNodes, transfer),
        Specification.Internet2ReachableInternal => Internet2Specification.ReachableInternal(topology, transfer),
        Specification.FatReachable => FatTreeSpecification<RouteEnvironment>.Reachable(FatTree.LabelFatTree(topology),
          transfer),
        Specification.FatPathLength => FatTreeSpecification<RouteEnvironment>.MaxPathLength(
          FatTree.LabelFatTree(topology),
          transfer),
        Specification.FatValleyFreedom => FatTreeSpecification<RouteEnvironment>
          .ValleyFreedom(FatTree.LabelFatTree(topology), transfer),
        Specification.FatHijackFiltering => FatTreeSpecification<Pair<RouteEnvironment, bool>>
          .FatTreeHijackFiltering(FatTree.LabelFatTree(topology, externalNodes), externalNodes, transfer,
            ast.Nodes.ToDictionary(p => p.Key, p => p.Value.Prefixes)),
        _ => throw new ArgumentOutOfRangeException(nameof(queryType), queryType, "Query type not supported!")
      };

      // turn on query printing if true
      net.PrintFormulas = printQuery;
      if (mono)
        Profile.RunMonoWithStats(net);
      else
        Profile.RunAnnotatedWithStats(net);
    }
    else
    {
      Console.WriteLine("Failed to deserialize contents of {file} (received null).");
    }
  }, fileArgument, queryArgument, monoOption, queryOption, trackTermsOption);

await rootCommand.InvokeAsync(args);
