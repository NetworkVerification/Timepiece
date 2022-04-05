// See https://aka.ms/new-console-template for more information

using Karesansui.Benchmarks;

var b = CommandLine.Parse(args);
if (b is null)
{
  Console.WriteLine("Failed to parse benchmark.");
  return;
}
b.Run(true);
