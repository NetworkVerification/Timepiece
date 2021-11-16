using System;
using System.Collections.Generic;
using System.Numerics;
using ZenLib;

namespace ZenDemo
{
    public class FaultTolerance<T> : Network<Option<T>>
    {
        /// <summary>
        /// Edges which have failed in the given topology.
        /// </summary>
        private Dictionary<Tuple<string, string>, bool> failedEdges;
        public FaultTolerance(string[] nodes, Dictionary<string, List<string>> edges,
            Func<Zen<Option<T>>, Zen<Option<T>>> transferFunction,
            Func<Zen<Option<T>>, Zen<Option<T>>, Zen<Option<T>>> mergeFunction,
            Dictionary<string, Option<T>> initialValues,
            Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> annotations,
            Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularAssertions,
            Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicAssertions, Dictionary<Tuple<string, string>, bool> failedEdges) : base(nodes, edges,
            transferFunction, mergeFunction, initialValues, annotations, modularAssertions, monolithicAssertions)
        {
            this.failedEdges = failedEdges;
        }
    }
}