using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using DDSPC.Data;
using DDSPC.Util;

namespace DDSPC.Runners;

public class TableGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    public TableGenerator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new TupleListJsonConverter() }
        };
    }

    public void LoadDataAndGenerateTable(string experimentPath)
    {
        List<DDSPCOutput> allResults = new();
        string resultsDir = Path.Combine(experimentPath, "Results");
        foreach (var outputFile in Directory.GetFiles(resultsDir))
        {
            string path = Path.Combine(resultsDir, outputFile);
            string json = File.ReadAllText(path);
            DDSPCOutput output = JsonSerializer.Deserialize<DDSPCOutput>(json, _jsonOptions);
            string graphName = Path.GetFileNameWithoutExtension(outputFile);
            // int lastIndex = graphName.IndexOf('S') - 1;
            // graphName = graphName.Substring(0, lastIndex);
            output.GraphName = graphName;
            allResults.Add(output);
        }

        GenerateSummaryTable(experimentPath, allResults);
    }

    private void GenerateSummaryTable(string experimentPath, List<DDSPCOutput> results)
    {
        string summaryPath = Path.Combine(experimentPath, "summary_table.tex");
        StringBuilder latexBuilder = new StringBuilder();

        // 1. Modifikovano zaglavlje tabele sa ugnježdenim kolonama
        latexBuilder.AppendLine(@"\begin{tabular}{l c c c c c c c c }
\hline
\textbf{Nodes} & \textbf{Density} & \textbf{Conflict prob.} & \multicolumn{3}{c }{\textbf{Objective}} & \multicolumn{3}{c }{\textbf{Time (s)}} \\
\cline{4-6} \cline{7-9}
 & & & \textbf{CPLEX} & \textbf{GRASP} & \textbf{GREEDY} & \textbf{CPLEX} & \textbf{GRASP} & \textbf{GREEDY} \\
\hline");

        // 2. Grupiši rezultate po grafu (bez seed-a u nazivu)
        IOrderedEnumerable<IGrouping<string, DDSPCOutput>> groupedResults = results
            .GroupBy(r => r.GraphName?.Split('_')[0..3].Aggregate((a, b) => $"{a}\\_{b}"))
            .OrderBy(g => g.Key);

        foreach (IGrouping<string, DDSPCOutput> group in groupedResults)
        {
            List<DDSPCOutput> cplexResults = group.Where(r => r.Solver == "CPLEX").ToList();
            List<DDSPCOutput> graspResults = group.Where(r => r.Solver == "GRASP").ToList();
            List<DDSPCOutput> greedyResults = group.Where(r => r.Solver == "GREEDY").ToList();

            if (cplexResults.Any() && graspResults.Any() && greedyResults.Any())
            {
                // 3. Izračunaj prosečne vrednosti za sve seedove
                double avgCplexObj = cplexResults.Average(r => r.Value);
                double avgGraspObj = graspResults.Average(r => r.Value);
                double avgGreedyObj = greedyResults.Average(r => r.Value);
                double avgCplexTime = cplexResults.Average(r => r.Runtime.TotalSeconds);
                double avgGraspTime = graspResults.Average(r => r.Runtime.TotalSeconds);
                double avgGreedyTime = greedyResults.Average(r => r.Runtime.TotalSeconds);

                string[] graphNameTokens = cplexResults.First().GraphName.Split('_');
                float density = float.Parse(graphNameTokens[1].Substring(1, graphNameTokens[1].Length - 1));
                float conflictProbability = float.Parse(graphNameTokens[2].Substring(1, graphNameTokens[2].Length - 1));

                // 4. Dodaj red u tabelu
                latexBuilder.AppendLine(
                    $@"{cplexResults.First().NumNodes} & {density} & {conflictProbability} & {avgCplexObj:F1} & {avgGraspObj:F1} & {avgGreedyObj:F1} & {avgCplexTime:F2} & {avgGraspTime:F2} & {avgGreedyTime:F2} \\");
            }

            if (!cplexResults.Any() && graspResults.Any() && greedyResults.Any())
            {
                // 3. Izračunaj prosečne vrednosti za sve seedove
                double avgGraspObj = graspResults.Average(r => r.Value);
                double avgGraspTime = graspResults.Average(r => r.Runtime.TotalSeconds);
                double avgGreedyObj = greedyResults.Average(r => r.Value);
                double avgGreedyTime = greedyResults.Average(r => r.Runtime.TotalSeconds);

                string[] graphNameTokens = graspResults.First().GraphName.Split('_');
                float density = float.Parse(graphNameTokens[1].Substring(1, graphNameTokens[1].Length - 1));
                float conflictProbability = float.Parse(graphNameTokens[2].Substring(1, graphNameTokens[2].Length - 1));

                // 4. Dodaj red u tabelu
                latexBuilder.AppendLine(
                    $@"{graspResults.First().NumNodes} & {density} & {conflictProbability} & - & {avgGraspObj:F1} & {avgGreedyObj:F1} & - & {avgGraspTime:F2} & {avgGreedyTime:F2} \\");
            }

            if (!cplexResults.Any() && graspResults.Any() && !greedyResults.Any())
            {
                // 3. Izračunaj prosečne vrednosti za sve seedove
                double avgGraspObj = graspResults.Average(r => r.Value);
                double avgGraspTime = graspResults.Average(r => r.Runtime.TotalSeconds);

                string[] graphNameTokens = graspResults.First().GraphName.Split('_');
                float density = float.Parse(graphNameTokens[1].Substring(1, graphNameTokens[1].Length - 1));
                float conflictProbability = float.Parse(graphNameTokens[2].Substring(1, graphNameTokens[2].Length - 1));

                // 4. Dodaj red u tabelu
                latexBuilder.AppendLine(
                    $@"{graspResults.First().NumNodes} & {density} & {conflictProbability} & - & {avgGraspObj:F1} & - & - & {avgGraspTime:F2} & - \\");
            }
        }

        // 5. Zatvaranje tabele
        latexBuilder.AppendLine(@"\hline
\end{tabular}");
        File.WriteAllText(summaryPath, latexBuilder.ToString());
    }
}