using DDSPC.Data;
using DDSPC.Util;

namespace DDSPC.Generators;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DDSPCGraphGenerator
{
    private readonly Random _random;

    public DDSPCGraphGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public DDSPCInput GenerateGraph(
        int numNodes,
        double edgeProbability = 0.3,
        double conflictProbability = 0.1)
    {
        var nodes = Enumerable.Range(0, numNodes).ToList();

        var edges = new List<(int, int)>();
        for (int i = 0; i < numNodes; i++)
        {
            for (int j = i + 1; j < numNodes; j++)
            {
                if (_random.NextDouble() < edgeProbability)
                {
                    edges.Add((i, j));
                }
            }
        }

        var conflicts = new List<(int, int)>();
        for (int i = 0; i < numNodes; i++)
        {
            for (int j = 0; j < numNodes; j++)
            {
                if (i != j && _random.NextDouble() < conflictProbability)
                {
                    conflicts.Add((i, j));
                }
            }
        }

        return new DDSPCInput
        {
            NumNodes = numNodes,
            Edges = edges,
            Conflicts = conflicts
        };
    }

    public DDSPCInput GenerateConnectedGraph(
        int numNodes,
        double edgeProbability = 0.3,
        double conflictProbability = 0.1)
    {
        DDSPCInput graph;
        do
        {
            graph = GenerateGraph(numNodes, edgeProbability, conflictProbability);
        } while (!IsGraphConnected(graph));

        return graph;
    }

    private bool IsGraphConnected(DDSPCInput graph)
    {
        if (graph.NumNodes == 0) return true;

        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(0);

        while (stack.Count > 0)
        {
            int node = stack.Pop();
            if (visited.Contains(node)) continue;

            visited.Add(node);
            foreach (var neighbor in DDSPCUtil.GetNeighbors(node, graph.Edges))
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return visited.Count == graph.NumNodes;
    }

    public DDSPCInput GenerateCyclicGraph(int numNodes, int maxConflicts)
    {
        var input = new DDSPCInput
        {
            NumNodes = numNodes,
            Edges = new List<(int, int)>(),
            Conflicts = new List<(int, int)>()
        };

        for (int i = 0; i < numNodes; i++)
        {
            input.Edges.Add((i, (i + 1) % numNodes));
        }

        for (int i = 0; i < maxConflicts; i++)
        {
            int node1;
            int node2;
            if (_random.Next(0, 1) < 0.5f)
            {
                node1 = _random.Next(0, numNodes / 2) * 2;
                node2 = _random.Next(0, numNodes / 2) * 2;
            }
            else
            {
                node1 = _random.Next(0, numNodes / 2) * 2 + 1;
                node2 = _random.Next(0, numNodes / 2) * 2 + 1;
            }

            if (node1 != node2)
            {
                input.Conflicts.Add((node1, node2));
            }
        }

        return input;
    }
}