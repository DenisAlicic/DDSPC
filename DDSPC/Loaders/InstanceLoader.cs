using DDSPC.Data;

namespace DDSPC.Loaders;

public class InstanceLoader
{
    public static void SaveInstance(DDSPCInput input, string filePath)
    {
        using StreamWriter writer = new StreamWriter(filePath);
        writer.WriteLine(input.NumNodes);
        writer.WriteLine(input.Edges.Count);
        foreach ((int, int) edge in input.Edges)
            writer.WriteLine($"{edge.Item1} {edge.Item2}");

        writer.WriteLine(input.Conflicts.Count);
        foreach ((int, int) conflict in input.Conflicts)
            writer.WriteLine($"{conflict.Item1} {conflict.Item2}");
    }

    public static DDSPCInput LoadInstance(string filePath)
    {
        List<(int, int)> edges = new();
        List<(int, int)> conflicts = new();

        using StreamReader reader = new StreamReader(filePath);

        int numNodes = int.Parse(reader.ReadLine());
        int numEdges = int.Parse(reader.ReadLine());

        for (int i = 0; i < numEdges; i++)
        {
            string[] parts = reader.ReadLine().Split();
            edges.Add((int.Parse(parts[0]), int.Parse(parts[1])));
        }

        int numConflicts = int.Parse(reader.ReadLine());
        for (int i = 0; i < numConflicts; i++)
        {
            string[] parts = reader.ReadLine().Split();
            conflicts.Add((int.Parse(parts[0]), int.Parse(parts[1])));
        }

        return new DDSPCInput
        {
            NumNodes = numNodes,
            Edges = edges,
            Conflicts = conflicts
        };
    }
}