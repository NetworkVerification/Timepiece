using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json;
using Timepiece;
using Timepiece.Angler.Ast;
using Timepiece.Angler.Networks;
using ZenLib;

ZenSettings.UseLargeStack = true;
ZenSettings.LargeStackSize = 30_000_000;

var rootCommand = new RootCommand("Timepiece benchmark runner");
var listQueriesCommand = new Command("list-queries", "Print the types of queries available to check");
listQueriesCommand.SetHandler(() => { Console.WriteLine(QueryTypeExtensions.AcceptableQueryValues()); });
var runCommand = new Command("run", "Run the given benchmark");
var monoOption = new System.CommandLine.Option<bool>(
  new[] {"--mono", "--ms", "-m"},
  "If given, run the benchmark monolithically (simulating Minesweeper)");
// var checkOption = new System.CommandLine.Option<SmtCheck>(new[] {"-c", "--check"},
// result => SmtCheckExtensions.Parse(result.Tokens.Single().Value),
// description: "The SMT check to perform");
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
  new Argument<QueryType>("query",
    description: "The type of query to check",
    parse: result => result.Tokens.Single().Value.ToQueryType());
rootCommand.AddCommand(listQueriesCommand);
rootCommand.AddCommand(runCommand);
runCommand.Add(fileArgument);
runCommand.Add(queryArgument);
runCommand.Add(monoOption);
runCommand.Add(queryOption);
runCommand.Add(trackTermsOption);
runCommand.SetHandler(
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
      var (topology, transfer) = Internet2Nodes.ShrinkInternet2(ast, 2);
      // var (topology, transfer) = ast.TopologyAndTransfer(trackTerms: trackTerms);
      var externalNodes = ast.Externals.Select(i => i.Name).ToArray();
      dynamic net = queryType switch
      {
        QueryType.Internet2BlockToExternal => AnglerInternet2.BlockToExternal(topology, externalNodes, transfer),
        QueryType.Internet2BlockToExternalFaultTolerant => AnglerInternet2.FaultTolerance(
          AnglerInternet2.BlockToExternal(topology, externalNodes, transfer)),
        QueryType.Internet2NoMartians => AnglerInternet2.NoMartians(topology, externalNodes, transfer),
        QueryType.Internet2NoMartiansFaultTolerant => AnglerInternet2.FaultTolerance(
          AnglerInternet2.NoMartians(topology, externalNodes, transfer)),
        QueryType.Internet2NoPrivateAs => AnglerInternet2.NoPrivateAs(topology, externalNodes, transfer),
        QueryType.Internet2Reachable => AnglerInternet2.Reachable(topology, externalNodes, transfer),
        QueryType.Internet2ReachableInternal => AnglerInternet2.ReachableInternal(topology, transfer),
        QueryType.FatReachable => AnglerFatTreeNetwork.Reachable(FatTree.LabelFatTree(topology),
          transfer),
        QueryType.FatReachableAllToR => AnglerFatTreeNetwork.ReachableAllToR(
          FatTree.LabelFatTree(topology),
          transfer),
        QueryType.FatPathLength => AnglerFatTreeNetwork.MaxPathLength(
          FatTree.LabelFatTree(topology),
          transfer),
        QueryType.FatPathLengthAllToR => AnglerFatTreeNetwork.MaxPathLengthAllToR(
          FatTree.LabelFatTree(topology),
          transfer),
        QueryType.FatValleyFreedom => AnglerFatTreeNetwork
          .ValleyFreedom(FatTree.LabelFatTree(topology), transfer),
        QueryType.FatValleyFreedomAllToR => AnglerFatTreeNetwork
          .ValleyFreedomAllToR(FatTree.LabelFatTree(topology), transfer),
        QueryType.FatHijackFiltering => AnglerFatTreeNetwork.FatTreeHijackFiltering(
          FatTree.LabelFatTree(topology, externalNodes), externalNodes, transfer,
          ast.Nodes.ToDictionary(p => p.Key, p => p.Value.Prefixes)),
        QueryType.FatHijackFilteringAllToR => AnglerFatTreeNetwork.FatTreeHijackFilteringAllToR(
          FatTree.LabelFatTree(topology, externalNodes), externalNodes, transfer,
          ast.Nodes.ToDictionary(p => p.Key, p => p.Value.Prefixes)),
        _ => throw new ArgumentOutOfRangeException(nameof(queryType), queryType, "Query type not supported!")
      };

      // turn on query printing if true
      net.PrintFormulas = printQuery;
      // net.Check(SmtCheck.Inductive, "atla-re1");
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
