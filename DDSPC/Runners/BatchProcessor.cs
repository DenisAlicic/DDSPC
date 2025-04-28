using System.Diagnostics;
using System.Text.Json;
using DDSPC.Data;
using DDSPC.Solver;
using DDSPC.Util;

public class BatchProcessor
{
    private readonly DDSPCCplexSolver _cplexSolver;
    private readonly JsonSerializerOptions _jsonOptions;

    public BatchProcessor()
    {
        _cplexSolver = new DDSPCCplexSolver();

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new TupleListJsonConverter() }
        };
    }

    public void ProcessExperimentParallel(string experimentPath, string solver)
    {
        string resultsDir = Path.Combine(experimentPath, "Results");
        Directory.CreateDirectory(resultsDir);

        // Prikupimo sve grafove koje treba obraditi
        var allGraphFiles = new List<(string graphPath, string graphFile, string paramDir, string resultsDir)>();

        foreach (string sizeDir in Directory.GetDirectories(experimentPath, "N*"))
        {
            foreach (string paramDir in Directory.GetDirectories(sizeDir))
            {
                foreach (string graphFile in Directory.GetFiles(paramDir, "G*.json"))
                {
                    allGraphFiles.Add((Path.Combine(paramDir, graphFile), graphFile, paramDir, resultsDir));
                }
            }
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount // Možeš prilagoditi broj niti
        };

        Console.WriteLine(options.MaxDegreeOfParallelism);

        Parallel.ForEach(allGraphFiles, options, item =>
        {
            try
            {
                var (graphPath, graphFile, _, resultsDir) = item;
                Console.WriteLine($"Processing: {graphPath}");

                DDSPCInput graph = LoadGraph(graphPath);
                DDSPCOutput? result = null;

                switch (solver.ToLower())
                {
                    case "grasp":
                        var graspSolver =
                            new DDSPCGRASP(alpha: 0.3, maxIterations: 1000, maxLocalSearchIterations: 200);
                        result = graspSolver.SolveWithMetrics(graph);
                        break;
                    case "greedy":
                        var greedySolver = new DDSCPGreedySolver(alpha: 0.3, 42);
                        result = greedySolver.SolveWithMetrics(graph);
                        break;
                }

                if (result != null)
                {
                    SaveResult(resultsDir, graphFile, result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {item.graphFile}: {ex.Message}");
            }
        });

        Console.WriteLine($"Processing complete. Results in: {experimentPath}");
    }

    public void ProcessCplexExperiment(string experimentPath, string solver)
    {
        string resultsDir = Path.Combine(experimentPath, "Results");
        Directory.CreateDirectory(resultsDir);


        foreach (string sizeDir in Directory.GetDirectories(experimentPath, "N*"))
        {
            foreach (string paramDir in Directory.GetDirectories(sizeDir))
            {
                string graphFile = Directory.GetFiles(paramDir, "G*.json").First();

                try
                {
                    string graphPath = Path.Combine(paramDir, graphFile);
                    Console.WriteLine($"Processing: {graphPath}");

                    DDSPCInput graph = LoadGraph(graphPath);
                    DDSPCOutput? result = null;
                    if (solver == "cplex")
                    {
                        result = _cplexSolver.SolveWithMetrics(graph);
                    }

                    if (result != null)
                    {
                        SaveResult(resultsDir, graphFile, result);
                    }
                }
                catch
                    (Exception ex)
                {
                    Console.WriteLine($"Error processing {graphFile}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"Processing complete. Results in: {experimentPath}");
    }

    public void RunPythonAnalysis(string experimentPath)
    {
        string pythonScript =
            Path.Combine(ProjectPathHelper.GetProjectRootPath(), "DDSPC", "Scripts", "analyze_grasp.py");
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{pythonScript}\" \"{experimentPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using (Process? process = Process.Start(startInfo))
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
        }
    }

    private double CalculateGap(int optimalValue, int heuristicValue)
    {
        return (heuristicValue - optimalValue) / (double)optimalValue * 100;
    }

    private DDSPCInput LoadGraph(string graphPath)
    {
        string json = File.ReadAllText(graphPath);
        return JsonSerializer.Deserialize<DDSPCInput>(json, _jsonOptions);
    }

    private void SaveResult(string resultsDir, string graphFile, DDSPCOutput result)
    {
        string baseName = Path.GetFileNameWithoutExtension(graphFile);

        File.WriteAllText(
            Path.Combine(resultsDir, $"{baseName}_{result.Solver}.json"),
            JsonSerializer.Serialize(result, _jsonOptions));
    }
}