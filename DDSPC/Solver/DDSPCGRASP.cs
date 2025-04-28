using System.Diagnostics;
using DDSPC.Data;
using DDSPC.Solver;
using DDSPC.Util;

public class DDSPCGRASP : DDSPCSolver
{
    private readonly int _maxIterations;
    private readonly int _maxLocalSearchIterations;
    private readonly DDSCPGreedySolver _greedySolver;

    public DDSPCGRASP(double alpha = 0.3, int maxIterations = 100, int maxLocalSearchIterations = 100,
        int? seed = null)
    {
        _maxIterations = maxIterations;
        _maxLocalSearchIterations = maxLocalSearchIterations;
        _greedySolver = new DDSCPGreedySolver(alpha, seed);
    }

    public override DDSPCOutput? SolveWithMetrics(DDSPCInput input)
    {
        var stopwatch = Stopwatch.StartNew();

        var result = Solve(input);
        if (result is null)
        {
            Console.WriteLine("No result");
            return null;
        }

        result.Runtime = stopwatch.Elapsed;
        result.GraphName = input.InstanceName;
        result.NumNodes = input.NumNodes;
        Console.WriteLine("Solution found");
        return result;
    }

    public DDSPCOutput? Solve(DDSPCInput input)
    {
        DDSPCOutput? bestSolution = null;
        int bestValue = int.MaxValue;

        for (int i = 0; i < _maxIterations; i++)
        {
            _greedySolver.Alpha = i / (double)_maxIterations;
            // Construction phase
            DDSPCOutput? solution = _greedySolver.ConstructGreedyRandomizedSolution(input);
            if (solution is null)
            {
                continue;
            }

            // Local search phase
            solution = LocalSearch(input, solution);


            // Update best solution
            if (solution != null && solution.Value < bestValue)
            {
                bestSolution = solution;
                bestValue = solution.Value;

                // Early exit if perfect solution found
                if (bestValue == 2) break; // Theoretical minimum is 2 (one node in each set)
            }
        }

        if (bestSolution != null)
        {
            bestSolution.Solver = "GRASP";
        }

        return bestSolution;
    }


    private DDSPCOutput? LocalSearch(DDSPCInput input, DDSPCOutput? solution)
    {
        if (solution != null && solution.Value == int.MaxValue) return solution; // invalid solution

        if (solution != null)
        {
            int currentValue = solution.Value;
            bool improved;
            int iterations = 0;

            do
            {
                improved = false;
                iterations++;

                // Try all possible 1-exchange moves (add/remove/swaps between D1 and D2)
                IEnumerable<DDSPCOutput> neighbors = GenerateNeighbors(input, solution);

                foreach (DDSPCOutput neighbor in neighbors)
                {
                    if (neighbor.Value < currentValue)
                    {
                        solution = neighbor;
                        currentValue = neighbor.Value;
                        improved = true;
                        break; // take first improving move
                    }
                }
            } while (improved && iterations < _maxLocalSearchIterations);
        }

        return solution;
    }

    private IEnumerable<DDSPCOutput> GenerateNeighbors(DDSPCInput input, DDSPCOutput solution)
    {
        // Generate neighboring solutions by:
        // Removing a node from D1 or D2

        // Try removing nodes from D1
        foreach (int node in solution.D1)
        {
            HashSet<int> newD1 = new(solution.D1);
            newD1.Remove(node);

            // Check if D1 is still a dominating set
            if (DDSPCUtil.IsDominatingSet(newD1, input))
            {
                DDSPCOutput newSolution = new DDSPCOutput
                {
                    D1 = newD1,
                    D2 = new HashSet<int>(solution.D2),
                    Value = solution.Value - 1
                };

                if (DDSPCUtil.IsValidSolution(input, newSolution))
                {
                    yield return newSolution;
                }
            }
        }

        // Try removing nodes from D2
        foreach (int node in solution.D2)
        {
            HashSet<int> newD2 = new(solution.D2);
            newD2.Remove(node);

            // Check if D2 is still a dominating set
            if (DDSPCUtil.IsDominatingSet(newD2, input))
            {
                DDSPCOutput newSolution = new DDSPCOutput
                {
                    D1 = new HashSet<int>(solution.D1),
                    D2 = newD2,
                    Value = solution.Value - 1
                };

                if (DDSPCUtil.IsValidSolution(input, newSolution))
                {
                    yield return newSolution;
                }
            }
        }

        /*
        // Try swapping nodes between D1 and D2
        foreach (int d1Node in solution.D1)
        {
            foreach (int d2Node in solution.D2)
            {
                // Check if swapping would create conflicts
                bool hasConflict = false;

                // Check if d2Node conflicts with any remaining D1 nodes
                foreach (int remainingD1Node in solution.D1.Where(n => n != d1Node))
                {
                    if (input.Conflicts.Contains((remainingD1Node, d2Node)) ||
                        input.Conflicts.Contains((d2Node, remainingD1Node)))
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (hasConflict) continue;

                // Check if d1Node conflicts with any remaining D2 nodes
                foreach (int remainingD2Node in solution.D2.Where(n => n != d2Node))
                {
                    if (input.Conflicts.Contains((d1Node, remainingD2Node)) ||
                        input.Conflicts.Contains((remainingD2Node, d1Node)))
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (hasConflict) continue;

                // Perform the swap
                HashSet<int> newD1 = new(solution.D1);
                newD1.Remove(d1Node);
                newD1.Add(d2Node);

                HashSet<int> newD2 = new(solution.D2);
                newD2.Remove(d2Node);
                newD2.Add(d1Node);

                DDSPCOutput newSolution = new DDSPCOutput
                {
                    D1 = newD1,
                    D2 = newD2,
                    Value = solution.Value // same size
                };

                if (DDSPCUtil.IsValidSolution(input, newSolution))
                {
                    yield return newSolution;
                }
            }
        }*/
    }
}