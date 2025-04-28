using DDSPC.Data;

namespace DDSPC.Util;

public static class DDSPCUtil
{
    public static bool IsValidSolution(DDSPCInput input, DDSPCOutput? solution)
    {
        // Check that D1 and D2 are disjoint
        if (solution.D1.Intersect(solution.D2).Any()) return false;

        // Check that D1 is a dominating set
        if (!IsDominatingSet(solution.D1, input)) return false;

        // Check that D2 is a dominating set
        if (!IsDominatingSet(solution.D2, input)) return false;

        // Check conflict constraints
        foreach (int d1Node in solution.D1)
        {
            foreach (int d2Node in solution.D2)
            {
                if (input.Conflicts.Contains((d1Node, d2Node)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool IsDominatingSet(HashSet<int> set, DDSPCInput input)
    {
        HashSet<int> dominated = new(set);

        foreach (int node in set)
        {
            foreach (int neighbor in GetNeighbors(node, input.Edges))
            {
                dominated.Add(neighbor);
            }
        }

        return dominated.Count == input.NumNodes;
    }

    public static IEnumerable<int> GetNeighbors(int node, List<(int, int)> edges)
    {
        foreach ((int u, int v) in edges)
        {
            if (u == node) yield return v;
            if (v == node) yield return u;
        }
    }
}