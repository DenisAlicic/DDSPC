using DDSPC.Data;

namespace DDSPC.Generators;

using System;
using System.Collections.Generic;
using System.IO;

class InstanceGenerator
{
    // Generiši ciklični graf sa garantovanim rešenjem
    public static DDSPCInput GenerateCyclicGraph(int numNodes, int maxConflicts, int seed)
    {
        var input = new DDSPCInput
        {
            NumNodes = numNodes,
            Edges = new List<(int, int)>(),
            Conflicts = new List<(int, int)>()
        };

        // Dodaj grane za ciklus
        for (int i = 0; i < numNodes; i++)
        {
            input.Edges.Add((i, (i + 1) % numNodes));
        }

        var random = new Random(seed);
        for (int i = 0; i < maxConflicts; i++)
        {
            int node1;
            int node2;
            if (random.Next(0, 1) < 0.5f)
            {
                node1 = random.Next(0, numNodes / 2) * 2;
                node2 = random.Next(0, numNodes / 2) * 2;
            }
            else
            {
                node1 = random.Next(0, numNodes / 2) * 2 + 1;
                node2 = random.Next(0, numNodes / 2) * 2 + 1;
            }

            if (node1 != node2)
            {
                input.Conflicts.Add((node1, node2));
            }
        }

        return input;
    }

    public static void SaveInstance(DDSPCInput input, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine(input.NumNodes);
            writer.WriteLine(input.Edges.Count);
            foreach ((int, int) edge in input.Edges)
                writer.WriteLine($"{edge.Item1} {edge.Item2}");

            writer.WriteLine(input.Conflicts.Count);
            foreach ((int, int) conflict in input.Conflicts)
                writer.WriteLine($"{conflict.Item1} {conflict.Item2}");
        }
    }
}