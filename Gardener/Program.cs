// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Gardener;
using Karesansui;

var ast = JsonSerializer.Deserialize<Ast>(args[0]);
if (ast is null)
{
  throw new ArgumentNullException();
}

Profile.RunCmp(ast.ToNetwork());
