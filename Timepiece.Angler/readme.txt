--- Angler deserialization in Timepiece ---

Timepiece is in the process of adding support for deserializing networks passed in from Angler.
As of December 2022, this support is partially completed: it is possible to take networks like Internet2
and import them using Timepiece.Angler.

Currently, we take as input an angler.json file, which defines network transfer and assert behaviors
using statements and expressions.
The angler.json must also specify the initial routes of each node.

For now, we define a set type of routes (BatfishBgpRoute.cs) and merge function:
at some point, we will want to relax this to allow users to provide this with Angler.

For constructing the statements and expressions, we need to first determine the type of statement or expression
to construct.
We do this in the UntypedAst.TypeParsing class.
Type parsing identifies the desired class given a JSON "$type" key in a JSON object.
We perform reflection to handle AST expressions that reference other types, like GetField and Some.
This means there may be runtime exceptions that occur if these types are not formulated correctly.
