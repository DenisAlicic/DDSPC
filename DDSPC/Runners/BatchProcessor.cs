using System.Diagnostics;
using System.Text.Json;
using DDSPC.Data;
using DDSPC.Solver;
using DDSPC.Util;

public class BatchProcessor
{
    private readonly DDSPCCplexSolver _cplexSolver;
    private readonly DDSPCGRASP _graspSolver;
    private readonly DDSCPGreedySolver _greedySolver;
    private readonly JsonSerializerOptions _jsonOptions;

    public BatchProcessor()
    {
        _cplexSolver = new DDSPCCplexSolver();
        _graspSolver = new DDSPCGRASP(alpha: 0.3, maxIterations: 200, maxLocalSearchIterations: 100, 42);
        _greedySolver = new DDSCPGreedySolver(alpha: 0.3, 42);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new TupleListJsonConverter() }
        };
    }

    public void ProcessExperiment(string experimentPath, string solver)
    {
        string resultsDir = Path.Combine(experimentPath, "Results");
        Directory.CreateDirectory(resultsDir);


        foreach (string sizeDir in Directory.GetDirectories(experimentPath, "N*"))
        {
            int nodes = int.Parse(new string(Path.GetFileName(sizeDir).Skip(1).ToArray()));

            foreach (string paramDir in Directory.GetDirectories(sizeDir))
            {
                string dirName = Path.GetFileName(paramDir);
                double density = int.Parse(dirName.Split('_')[0][1..]) / 100.0;
                double conflict = int.Parse(dirName.Split('_')[1][1..]) / 100.0;

                foreach (string graphFile in Directory.GetFiles(paramDir, "G*.json"))
                {
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
                        else if (solver == "grasp")
                        {
                            result = _graspSolver.SolveWithMetrics(graph);
                        }
                        else if (solver == "greedy")
                        {
                            result = _greedySolver.SolveWithMetrics(graph);
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