using System.Text;
using System.Text.Json;
using DDSPC.Data;
using DDSPC.Solver;
using DDSPC.Util;

public class BatchProcessor
{
    private readonly DDSPCCplexSolver _cplexSolver;
    private readonly DDSPCGRASP _graspSolver;
    private readonly JsonSerializerOptions _jsonOptions;

    public BatchProcessor()
    {
        _cplexSolver = new DDSPCCplexSolver();
        _graspSolver = new DDSPCGRASP(alpha: 0.3, maxIterations: 100);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new TupleListJsonConverter() }
        };
    }

    public void ProcessExperiment(string experimentName)
    {
        string experimentPath = Path.Combine(
            ProjectPathHelper.GetDataPath(),
            "Experiments",
            experimentName);

        string resultsDir = Path.Combine(experimentPath, "Results");
        Directory.CreateDirectory(resultsDir);

        var allResults = new List<DDSPCOutput>();

        foreach (var sizeDir in Directory.GetDirectories(experimentPath, "N*"))
        {
            int nodes = int.Parse(new string(Path.GetFileName(sizeDir).Skip(1).ToArray()));

            foreach (var paramDir in Directory.GetDirectories(sizeDir))
            {
                var dirName = Path.GetFileName(paramDir);
                double density = int.Parse(dirName.Split('_')[0][1..]) / 100.0;
                double conflict = int.Parse(dirName.Split('_')[1][1..]) / 100.0;

                foreach (var graphFile in Directory.GetFiles(paramDir, "G*.json"))
                {
                    try
                    {
                        string graphPath = Path.Combine(paramDir, graphFile);
                        Console.WriteLine($"Processing: {graphPath}");

                        var graph = LoadGraph(graphPath);
                        var results = ProcessGraph(graph);

                        if (results.CplexResult != null && results.GraspResult != null)
                        {
                            // 3. Sačuvaj pojedinačne rezultate
                            SaveResults(resultsDir, graphFile, results);
                            allResults.Add(results.CplexResult);
                            allResults.Add(results.GraspResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {graphFile}: {ex.Message}");
                    }
                }
            }
        }

        GenerateSummaryTable(experimentPath, allResults);
        Console.WriteLine($"Processing complete. Results in: {experimentPath}");
    }

    private (DDSPCOutput CplexResult, DDSPCOutput GraspResult) ProcessGraph(DDSPCInput graph)
    {
        var cplexResult = _cplexSolver.SolveWithMetrics(graph);
        var graspResult = _graspSolver.SolveWithMetrics(graph);

        if (cplexResult != null && graspResult != null)
        {
            graspResult.GapPercent = CalculateGap(cplexResult.Value, graspResult.Value);
        }

        return (cplexResult, graspResult);
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

    private void SaveResults(string resultsDir, string graphFile, (DDSPCOutput Cplex, DDSPCOutput Grasp) results)
    {
        string baseName = Path.GetFileNameWithoutExtension(graphFile);

        File.WriteAllText(
            Path.Combine(resultsDir, $"{baseName}_CPLEX.json"),
            JsonSerializer.Serialize(results.Cplex, _jsonOptions));

        File.WriteAllText(
            Path.Combine(resultsDir, $"{baseName}_GRASP.json"),
            JsonSerializer.Serialize(results.Grasp, _jsonOptions));
    }

    private void GenerateSummaryTable(string experimentPath, List<DDSPCOutput> results)
    {
        var summaryPath = Path.Combine(experimentPath, "summary_table.tex");
        var latexBuilder = new StringBuilder();

        // 1. Zaglavlje tabele
        latexBuilder.AppendLine(@"\begin{tabular}{|l|c|c|c|c|c|c|}
\hline
\textbf{Graph} & \textbf{Nodes} & \textbf{CPLEX Obj.} & \textbf{GRASP Obj.} & \textbf{Gap (\%)} & \textbf{CPLEX Time (s)} & \textbf{GRASP Time (s)} \\
\hline");

        // 2. Grupiši rezultate po grafu (bez seed-a u nazivu)
        var groupedResults = results
            .GroupBy(r => r.GraphName?.Split('_')[0..3].Aggregate((a, b) => $"{a}_{b}"))
            .OrderBy(g => g.Key);

        foreach (var group in groupedResults)
        {
            var cplexResults = group.Where(r => r.Solver == "CPLEX").ToList();
            var graspResults = group.Where(r => r.Solver == "GRASP").ToList();

            if (cplexResults.Any() && graspResults.Any())
            {
                // 3. Izračunaj prosečne vrednosti za sve seedove
                double avgCplexObj = cplexResults.Average(r => r.Value);
                double avgGraspObj = graspResults.Average(r => r.Value);
                double avgGap = graspResults.Average(r => r.GapPercent);
                double avgCplexTime = cplexResults.Average(r => r.Runtime.TotalSeconds);
                double avgGraspTime = graspResults.Average(r => r.Runtime.TotalSeconds);

                // 4. Dodaj red u tabelu
                latexBuilder.AppendLine($@"{group.Key} & 
{cplexResults.First().NumNodes} & 
{avgCplexObj:F1} & 
{avgGraspObj:F1} & 
{avgGap:F2} & 
{avgCplexTime:F2} & 
{avgGraspTime:F2} \\");
            }
        }

        // 5. Zatvaranje tabele
        latexBuilder.AppendLine(@"\hline
\end{tabular}");
        File.WriteAllText(summaryPath, latexBuilder.ToString());
    }
}