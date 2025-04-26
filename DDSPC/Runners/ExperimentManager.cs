using System.Text.Json;
using DDSPC.Generators;
using DDSPC.Util;

namespace DDSPC.Runners;

public class ExperimentManager
{
    public void RunMultiSeedExperiment()
    {
        // Parametri eksperimenta
        int[] nodeCounts = { 20, 50, 100, 200 };
        double[] edgeDensities = { 0.2, 0.4, 0.6 };
        double[] conflictProbabilities = { 0.05, 0.1, 0.2 };
        int seedsCount = 10;

        string experimentName = $"MultiSeedExperiment_{DateTime.Now:yyyyMMdd_HHmm}";
        string experimentPath = Path.Combine(ProjectPathHelper.GetDataPath(), "Experiments", experimentName);

        Directory.CreateDirectory(experimentPath);
        var manifestLines = new List<string>
        {
            "GraphName,Seed,Nodes,Edges,Conflicts,Density,ConflictProbability,IsConnected"
        };

        foreach (int nodes in nodeCounts)
        {
            string sizeFolder = Path.Combine(experimentPath, $"N{nodes}");
            Directory.CreateDirectory(sizeFolder);

            foreach (double density in edgeDensities)
            {
                foreach (double conflict in conflictProbabilities)
                {
                    for (int seed = 1; seed <= seedsCount; seed++)
                    {
                        var generator = new GraphGenerator(seed);
                        var graph = generator.GenerateConnectedGraph(nodes, density, conflict);

                        string graphFolder = Path.Combine(sizeFolder,
                            $"D{(int)(density * 100)}_C{(int)(conflict * 100)}");
                        Directory.CreateDirectory(graphFolder);

                        string graphPath = Path.Combine(graphFolder,
                            $"{graph.InstanceName}_S{seed}.json");

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Converters = { new TupleListJsonConverter() }
                        };
                        File.WriteAllText(graphPath, JsonSerializer.Serialize(graph,
                            options));

                        manifestLines.Add(
                            $"{graph.InstanceName}," +
                            $"{seed}," +
                            $"{nodes}," +
                            $"{graph.Edges.Count}," +
                            $"{graph.Conflicts.Count}," +
                            $"{density}," +
                            $"{conflict}," +
                            $"{graph.IsConnected()}"
                        );
                    }
                }
            }
        }

        File.WriteAllLines(Path.Combine(experimentPath, "manifest.csv"), manifestLines);
        Console.WriteLine($"Eksperiment sačuvan u: {Path.GetFullPath(experimentPath)}");
    }
}