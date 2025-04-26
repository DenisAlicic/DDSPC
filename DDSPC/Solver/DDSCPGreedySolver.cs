using System.Diagnostics;
using DDSPC.Data;
using DDSPC.Util;

namespace DDSPC.Solver;

public class DDSCPGreedySolver : DDSPCSolver
{
    private readonly Random _random;
    private readonly double _alpha; // Parameter for restricted candidate list (0 = greedy, 1 = random)

    public DDSCPGreedySolver(double alpha, int? seed = null)
    {
        _alpha = alpha;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public override DDSPCOutput? SolveWithMetrics(DDSPCInput input)
    {
        var stopwatch = Stopwatch.StartNew();

        var result = ConstructGreedyRandomizedSolution(input);
        if (result is null)
        {
            Console.WriteLine("No result");
            return null;
        }

        result.Solver = "GREEDY";
        result.Runtime = stopwatch.Elapsed;
        result.GraphName = input.InstanceName;
        result.NumNodes = input.NumNodes;
        Console.WriteLine("Solution found");
        return result;
    }

    public DDSPCOutput? ConstructGreedyRandomizedSolution(DDSPCInput input)
    {
        DDSPCOutput solution = new DDSPCOutput
        {
            D1 = new HashSet<int>(),
            D2 = new HashSet<int>(),
            Value = 0
        };

        // We'll build the solution in two phases:
        // 1. First select D1 to be a dominating set
        // 2. Then select D2 to be a dominating set that doesn't conflict with D1

        // Phase 1: Build D1 as a dominating set
        HashSet<int> uncovered = [..Enumerable.Range(0, input.NumNodes)];
        while (uncovered.Count > 0)
        {
            // Evaluate all candidate nodes that can cover uncovered nodes
            List<int> candidates = new();
            Dictionary<int, int> coverage = new(); // node -> how many uncovered it covers

            foreach (int node in Enumerable.Range(0, input.NumNodes))
            {
                if (solution.D1.Contains(node)) continue;

                // Count how many uncovered nodes this node would cover
                int count = 1; // node itself
                foreach (int neighbor in DDSPCUtil.GetNeighbors(node, input.Edges))
                {
                    if (uncovered.Contains(neighbor)) count++;
                }

                if (count > 0)
                {
                    candidates.Add(node);
                    coverage[node] = count;
                }
            }

            if (candidates.Count == 0) break; // should not happen for connected graphs

            // Create restricted candidate list (RCL)
            int minCoverage = coverage.Values.Min();
            int maxCoverage = coverage.Values.Max();
            double threshold = minCoverage + _alpha * (maxCoverage - minCoverage);

            List<int> rcl = candidates.Where(c => coverage[c] >= threshold).ToList();

            // Randomly select from RCL
            int selected = rcl[_random.Next(rcl.Count)];
            solution.D1.Add(selected);
            solution.Value++;

            // Update uncovered nodes
            uncovered.Remove(selected);
            foreach (int neighbor in DDSPCUtil.GetNeighbors(selected, input.Edges))
            {
                uncovered.Remove(neighbor);
            }
        }

        // Phase 2: Build D2 as a dominating set that doesn't conflict with D1
        uncovered = new HashSet<int>(Enumerable.Range(0, input.NumNodes));
        while (uncovered.Count > 0)
        {
            // Evaluate all candidate nodes that can cover uncovered nodes and don't conflict with D1
            List<int> candidates = new();
            Dictionary<int, int> coverage = new(); // node -> how many uncovered it covers

            foreach (int node in Enumerable.Range(0, input.NumNodes))
            {
                if (solution.D2.Contains(node)) continue;
                if (solution.D1.Contains(node)) continue; // D1 and D2 must be disjoint

                // Check if this node conflicts with any node in D1
                bool hasConflict = false;
                foreach (int d1Node in solution.D1)
                {
                    if (input.Conflicts.Contains((d1Node, node)) || input.Conflicts.Contains((node, d1Node)))
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (hasConflict) continue;

                // Count how many uncovered nodes this node would cover
                int count = 1; // node itself
                foreach (int neighbor in DDSPCUtil.GetNeighbors(node, input.Edges))
                {
                    if (uncovered.Contains(neighbor)) count++;
                }

                if (count > 0)
                {
                    candidates.Add(node);
                    coverage[node] = count;
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // Create restricted candidate list (RCL)
            int minCoverage = coverage.Values.Min();
            int maxCoverage = coverage.Values.Max();
            double threshold = minCoverage + _alpha * (maxCoverage - minCoverage);

            List<int> rcl = candidates.Where(c => coverage[c] >= threshold).ToList();

            // Randomly select from RCL
            int selected = rcl[_random.Next(rcl.Count)];
            solution.D2.Add(selected);
            solution.Value++;

            // Update uncovered nodes
            uncovered.Remove(selected);
            foreach (int neighbor in DDSPCUtil.GetNeighbors(selected, input.Edges))
            {
                uncovered.Remove(neighbor);
            }
        }

        return solution;
    }
}