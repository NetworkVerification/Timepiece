using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenDemo
{
    /// <summary>
    /// Represents the topology of an NV network.
    /// </summary>
    public class Topology
    {
        /// <summary>
        /// The nodes in the network and their names.
        /// </summary>
        public readonly string[] nodes;

        /// <summary>
        /// The edges for each node in the network.
        /// </summary>
        public readonly Dictionary<string, List<string>> neighbors;

        public Topology(Dictionary<string, List<string>> edges)
        {
            neighbors = edges;
            nodes = neighbors.Keys.ToArray();
        }
    }

    public static class DefaultTopologies
    {
        // helper method to generate node names ala Excel columns
        // adapted from https://stackoverflow.com/a/5384627
        private static string ToBase26(long i)
        {
            // the recursion adds the prefix
            if (i == 0) return "";
            i--;
            // the modulo is used to get the next char, looping back from 'Z' to 'A'
            return ToBase26(i / 26) + (char) ('A' + i % 26);
        }

        public static Topology Chain(int length)
        {
            var neighbors = new Dictionary<string, List<string>>();
            for (var i = 0; i < length; i++)
            {
                neighbors.Add(ToBase26(i + 1), new List<string>());
            }

            var nodes = neighbors.Keys.ToArray();
            for (var i = 1; i < length; i++)
            {
                // add a pair of edges in sequence
                neighbors[nodes[i - 1]].Add(nodes[i]);
                neighbors[nodes[i]].Add(nodes[i - 1]);
            }

            return new Topology(neighbors);
        }
    }
}