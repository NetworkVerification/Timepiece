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

        public FaultTolerance(Topology topology,
            Dictionary<(string, string), Func<Zen<Option<T>>, Zen<Option<T>>>> transferFunction,
            Func<Zen<Option<T>>, Zen<Option<T>>, Zen<Option<T>>> mergeFunction,
            Dictionary<string, Option<T>> initialValues,
            Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> annotations,
            Dictionary<string, Func<Zen<Option<T>>, Zen<BigInteger>, Zen<bool>>> modularProperties,
            Dictionary<string, Func<Zen<Option<T>>, Zen<bool>>> monolithicProperties,
            Dictionary<Tuple<string, string>, bool> failedEdges) : base(topology,
            transferFunction, mergeFunction, initialValues, annotations, modularProperties, monolithicProperties)
        {
            this.failedEdges = failedEdges;
        }
    }
}