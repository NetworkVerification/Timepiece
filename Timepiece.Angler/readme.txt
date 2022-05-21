--- Angler deserialization in Timepiece ---

Timepiece is in the process of adding support for deserializing networks passed in from Angler.
As of May 2022, this support is not yet complete.

Currently, we take as input an angler.json file, which defines network transfer and assert behaviors
using statements and expressions.
The angler.json must also specify the prefixes of each node to determine the initial routes.

For now, we define a set type of routes and merge function: at some point, we will want to relax this to allow
users to provide this with Angler.

For constructing the statements and expressions, we need to first determine the type of statement or expression
to construct.
Each type of statement and expression has particular fields which need to be deserialized as well.
Currently, we use Newtonsoft to try and do this, but it has some issues when dealing with expressions which
reference types, like GetField or Some.
Because of this, we need deserialization to extract these arguments and pass them in when dynamically constructing
the expressions.
This might be easier to do using System.Text.Json.
