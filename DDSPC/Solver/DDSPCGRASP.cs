using DDSPC.Data;
using DDSPC.Util;

public class DDSPCGRASP
{
    private readonly Random _random;
    private readonly double _alpha; // Parameter for restricted candidate list (0 = greedy, 1 = random)
    private readonly int _maxIterations;
    private readonly int _maxLocalSearchIterations;

    public DDSPCGRASP(double alpha = 0.3, int maxIterations = 100, int maxLocalSearchIterations = 100,
        int? seed = null)
    {
        _alpha = alpha;
        _maxIterations = maxIterations;
        _maxLocalSearchIterations = maxLocalSearchIterations;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public DDSPCOutput? Solve(DDSPCInput input)
    {
        DDSPCOutput? bestSolution = null;
        int bestValue = int.MaxValue;

        for (int i = 0; i < _maxIterations; i++)
        {
            // Construction phase
            DDSPCOutput? solution = ConstructGreedyRandomizedSolution(input);

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

    private DDSPCOutput? ConstructGreedyRandomizedSolution(DDSPCInput input)
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
                // If we can't find a valid candidate, we need to backtrack or mark as invalid
                // For simplicity, we'll just return a solution with very high value
                return new DDSPCOutput { Value = int.MaxValue };
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
                IEnumerable<DDSPCOutput?> neighbors = GenerateNeighbors(input, solution);

                foreach (DDSPCOutput? neighbor in neighbors)
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

    private IEnumerable<DDSPCOutput?> GenerateNeighbors(DDSPCInput input, DDSPCOutput? solution)
    {
        // Generate neighboring solutions by:
        // 1. Adding a node to D1 or D2
        // 2. Removing a node from D1 or D2
        // 3. Swapping a node between D1 and D2

        // Try adding nodes to D1
        foreach (int node in Enumerable.Range(0, input.NumNodes))
        {
            if (solution.D1.Contains(node)) continue;
            if (solution.D2.Contains(node)) continue;

            // Check if adding to D1 would create conflicts with D2
            bool hasConflict = false;
            foreach (int d2Node in solution.D2)
            {
                if (input.Conflicts.Contains((node, d2Node)) || input.Conflicts.Contains((d2Node, node)))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict) continue;

            // Create new solution with this node added to D1
            DDSPCOutput newSolution = new DDSPCOutput
            {
                D1 = new HashSet<int>(solution.D1) { node },
                D2 = new HashSet<int>(solution.D2),
                Value = solution.Value + 1
            };

            // Check if it's still a valid solution
            if (DDSPCUtil.IsValidSolution(input, newSolution))
            {
                yield return newSolution;
            }
        }

        // Try adding nodes to D2 (similar to above)
        foreach (int node in Enumerable.Range(0, input.NumNodes))
        {
            if (solution.D2.Contains(node)) continue;
            if (solution.D1.Contains(node)) continue;

            // Check if adding to D2 would create conflicts with D1
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

            // Create new solution with this node added to D2
            DDSPCOutput newSolution = new DDSPCOutput
            {
                D1 = new HashSet<int>(solution.D1),
                D2 = new HashSet<int>(solution.D2) { node },
                Value = solution.Value + 1
            };

            if (DDSPCUtil.IsValidSolution(input, newSolution))
            {
                yield return newSolution;
            }
        }

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
        }
    }
}