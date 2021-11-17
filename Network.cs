using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ZenLib;
using static ZenLib.Language;

namespace ZenDemo
{
    /// <summary>
    /// Represents an NV network.
    /// </summary>
    /// <typeparam name="T">The type of the routes.</typeparam>
    public class Network<T>
    {
        /// <summary>
        /// The nodes in the network and their names.
        /// </summary>
        protected string[] nodes;

        /// <summary>
        /// The edges for each node in the network.
        /// </summary>
        protected Dictionary<string, List<string>> neighbors;

        /// <summary>
        /// The transfer function (assume the same for each node for now)
        /// </summary>
        protected Func<Zen<T>, Zen<T>> transferFunction;

        /// <summary>
        /// The merge function for routes.
        /// </summary>
        protected Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction;

        /// <summary>
        /// The initial values for each node.
        /// </summary>
        protected Dictionary<string, T> initialValues;

        /// <summary>
        /// The invariant/annotation function for each node. Takes a route and a time and returns a boolean.
        /// </summary>
        protected Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations;

        /// <summary>
        /// The modular safety properties that we want to check (includes time).
        /// </summary>
        protected Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> modularProperties;

        /// <summary>
        /// The monolithic safety properties that we want to check (assumes stable states).
        /// </summary>
        protected Dictionary<string, Func<Zen<T>, Zen<bool>>> monolithicProperties;

        public Network(
            Topology topology,
            Func<Zen<T>, Zen<T>> transferFunction,
            Func<Zen<T>, Zen<T>, Zen<T>> mergeFunction,
            Dictionary<string, T> initialValues,
            Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> annotations,
            Dictionary<string, Func<Zen<T>, Zen<BigInteger>, Zen<bool>>> modularProperties,
            Dictionary<string, Func<Zen<T>, Zen<bool>>> monolithicProperties)
        {
            nodes = topology.nodes;
            neighbors = topology.neighbors;
            this.transferFunction = transferFunction;
            this.mergeFunction = mergeFunction;
            this.initialValues = initialValues;
            this.annotations = annotations;
            this.modularProperties = modularProperties;
            this.monolithicProperties = monolithicProperties;
        }

        /// <summary>
        /// Check that the annotations are sound.
        /// </summary>
        /// <returns>True if the annotations pass, false otherwise.</returns>
        public bool CheckAnnotations()
        {
            return CheckBaseCase() && CheckAssertions() && CheckInductive();
        }

        /// <summary>
        /// Ensure that all the base check pass.
        /// </summary>
        /// <returns>True if the annotations pass, false otherwise.</returns>
        public bool CheckBaseCase()
        {
            foreach (var node in nodes)
            {
                var route = Symbolic<T>();

                // if the route is the initial value, then the annotation holds (i.e., the annotation contains the route at time 0).
                var check = Implies(route == initialValues[node],
                    annotations[node](route, new BigInteger(0)));

                // negate and try to prove unsat.
                var model = Not(check).Solve();

                if (model.IsSatisfiable())
                {
                    Console.WriteLine($"Base check failed at node: {node}");
                    return false;
                }
            }

            Console.WriteLine($"All the base checks passed!");
            return true;
        }

        /// <summary>
        /// Ensure that the inductive invariants imply the assertions.
        /// </summary>
        /// <returns>True if the annotations pass, false otherwise.</returns>
        public bool CheckAssertions()
        {
            foreach (var node in nodes)
            {
                var route = Symbolic<T>();
                var time = Symbolic<BigInteger>();

                // ensure the inductive invariant implies the assertions we want to prove.
                var check = Implies(annotations[node](route, time), modularProperties[node](route, time));

                // negate and try to prove unsat.
                var model = Not(check).Solve();

                if (model.IsSatisfiable())
                {
                    Console.WriteLine($"Assertion check failed at node: {node}");
                    return false;
                }
            }

            Console.WriteLine($"All the assertions checks passed!");
            return true;
        }

        /// <summary>
        /// Ensure that the inductive checks all pass.
        /// </summary>
        /// <returns>True if the annotations pass, false otherwise.</returns>
        public bool CheckInductive()
        {
            // create symbolic values for each node.
            var routes = new Dictionary<string, Zen<T>>();
            foreach (var node in nodes)
            {
                routes[node] = Symbolic<T>();
            }

            // create a symbolic time variable.
            var time = Symbolic<BigInteger>();

            // check the inductiveness of the invariant for each node.
            foreach (var node in nodes)
            {
                // get the new route as the merge of all neighbors
                var newNodeRoute = neighbors[node].Aggregate(Constant(initialValues[node]),
                    (current, neighbor) => mergeFunction(current, transferFunction(routes[neighbor])));

                // collect all of the assumptions from neighbors.
                var assume = new List<Zen<bool>> {time > new BigInteger(0)};
                assume.AddRange(neighbors[node].Select(neighbor =>
                    this.annotations[neighbor](routes[neighbor], time - new BigInteger(1))));

                // now we need to ensure the new route after merging implies the annotation for this node.
                var check = Implies(And(assume.ToArray()), annotations[node](newNodeRoute, time));

                // negate and try to prove unsat.
                var model = Not(check).Solve();

                if (model.IsSatisfiable())
                {
                    Console.WriteLine($"Inductive check failed at node: {node} for time: {model.Get(time)}");

                    foreach (var neighbor in neighbors[node])
                    {
                        Console.WriteLine($"neighbor {neighbor} had route: {model.Get(routes[neighbor])}");
                    }

                    return false;
                }
            }

            Console.WriteLine($"All the inductive checks passed!");
            return true;
        }

        /// <summary>
        /// Check the network using a stable routes encoding.
        /// </summary>
        /// <returns>True if the network verifies, false otherwise.</returns>
        public bool CheckMonolithic()
        {
            // create symbolic values for each node.
            var routes = new Dictionary<string, Zen<T>>();
            foreach (var node in nodes)
            {
                routes[node] = Symbolic<T>();
            }

            // add the assertions
            var assertions = nodes.Select(node => monolithicProperties[node](routes[node]));

            // add constraints for each node, that its route is the merge of all the neighbors and init
            var constraints = nodes.Select(node =>
                routes[node] == neighbors[node].Aggregate(Constant(initialValues[node]),
                    (current, neighbor) => mergeFunction(current, transferFunction(routes[neighbor]))));

            var check = And(And(constraints.ToArray()), Not(And(assertions.ToArray())));

            // negate and try to prove unsat.
            var model = check.Solve();

            if (model.IsSatisfiable())
            {
                Console.WriteLine($"Monolithic check failed!");

                foreach (var node in nodes)
                {
                    Console.WriteLine($"node {node} had route: {model.Get(routes[node])}");
                }

                return false;
            }

            Console.WriteLine($"The monolithic checks passed!");
            return true;
        }
    }
}