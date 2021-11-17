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

        /// <summary>
        /// Create a path digraph topology.
        /// </summary>
        /// <param name="numNodes">Number of nodes in topology.</param>
        /// <returns></returns>
        public static Topology Path(int numNodes)
        {
            var neighbors = new Dictionary<string, List<string>>();
            for (var i = 0; i < numNodes; i++)
            {
                neighbors.Add(ToBase26(i + 1), new List<string>());
            }

            var nodes = neighbors.Keys.ToArray();
            for (var i = 1; i < numNodes; i++)
            {
                // add a pair of edges in sequence
                neighbors[nodes[i - 1]].Add(nodes[i]);
                neighbors[nodes[i]].Add(nodes[i - 1]);
            }

            return new Topology(neighbors);
        }

        /// <summary>
        /// Create a complete digraph topology.
        /// </summary>
        /// <param name="numNodes">Number of nodes in topology.</param>
        /// <returns></returns>
        public static Topology Complete(int numNodes)
        {
            var neighbors = new Dictionary<string, List<string>>();
            for (var i = 0; i < numNodes; i++)
            {
                neighbors.Add(ToBase26(i + 1), new List<string>());
            }

            var nodes = neighbors.Keys;
            foreach (var (node,adj) in neighbors)
            {
                // add all other nodes except the current one
                adj.AddRange(nodes.Where(n => n != node));
            }

            return new Topology(neighbors);
        }
    }
}