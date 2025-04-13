using DDSPC.Data;

namespace DDSPC.Generators;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GraphGenerator
{
    private readonly Random _random;

    public GraphGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public DDSPCInput GenerateConnectedGraph(int nodeCount, double edgeDensity, double conflictProbability,
        string? name = null)
    {
        DDSPCInput graph;
        int attempt = 0;
        const int maxAttempts = 100;

        do
        {
            graph = GenerateGraph(nodeCount, edgeDensity, conflictProbability, name);
            attempt++;

            if (attempt >= maxAttempts)
            {
                throw new Exception(
                    $"Nije moguće generisati povezan graf sa {nodeCount} čvorova i gustinom {edgeDensity} nakon {maxAttempts} pokušaja");
            }
        } while (!graph.IsConnected());

        return graph;
    }

    public DDSPCInput GenerateGraph(int nodeCount, double edgeDensity, double conflictProbability, string? name = null)
    {
        var edges = GenerateConnectedEdges(nodeCount, edgeDensity);
        var conflicts = GenerateConflicts(nodeCount, conflictProbability);

        return new DDSPCInput
        {
            NumNodes = nodeCount,
            Edges = edges,
            Conflicts = conflicts,
            InstanceName = name ?? GenerateGraphName(nodeCount, edgeDensity, conflictProbability)
        };
    }

    private List<(int, int)> GenerateConnectedEdges(int nodeCount, double edgeDensity)
    {
        var edges = new List<(int, int)>();
        for (int i = 1; i < nodeCount; i++)
        {
            edges.Add((_random.Next(i), i));
        }

        int targetEdgeCount = (int)(edgeDensity * nodeCount * (nodeCount - 1) / 2);
        int currentEdgeCount = edges.Count;

        while (currentEdgeCount < targetEdgeCount)
        {
            int u = _random.Next(nodeCount);
            int v = _random.Next(nodeCount);

            if (u != v && !edges.Contains((u, v)) && !edges.Contains((v, u)))
            {
                edges.Add((u, v));
                currentEdgeCount++;
            }
        }

        return edges;
    }

    private List<(int, int)> GenerateEdges(int nodeCount, double edgeDensity)
    {
        var edges = new List<(int, int)>();
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (_random.NextDouble() < edgeDensity)
                {
                    edges.Add((i, j));
                }
            }
        }

        return edges;
    }

    private List<(int, int)> GenerateConflicts(int nodeCount, double conflictProbability)
    {
        var conflicts = new List<(int, int)>();
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (_random.NextDouble() < conflictProbability)
                {
                    conflicts.Add((i, j));
                }
            }
        }

        return conflicts;
    }

    private string GenerateGraphName(int nodes, double density, double conflict)
    {
        return $"G{nodes}_D{density:0.00}_C{conflict:0.00}";
    }
}