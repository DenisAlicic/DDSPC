using DDSPC.Data;

namespace DDSPC.Loaders;

public class InstanceLoader
{
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