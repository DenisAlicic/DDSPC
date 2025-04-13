using System.Text.Json;
using System.Text.Json.Serialization;
using DDSPC.Util;

namespace DDSPC.Data;

public class DDSPCInput
{
    public int NumNodes { get; set; }

    [JsonConverter(typeof(TupleListJsonConverter))]
    public List<(int, int)> Edges { get; set; }

    [JsonConverter(typeof(TupleListJsonConverter))]
    public List<(int, int)> Conflicts { get; set; }

    public string InstanceName { get; set; }

    public bool IsConnected()
    {
        if (NumNodes == 0) return false;

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited.Add(current);

            foreach (var (u, v) in Edges)
            {
                if (u == current && !visited.Contains(v))
                    queue.Enqueue(v);
                else if (v == current && !visited.Contains(u))
                    queue.Enqueue(u);
            }
        }

        return visited.Count == NumNodes;
    }

    public static DDSPCInput Example01()
    {
        return new DDSPCInput
        {
            NumNodes = 5,
            Edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0)],
            Conflicts = [(0, 2), (1, 3)]
        };
    }

    public static DDSPCInput Example02()
    {
        return new DDSPCInput
        {
            NumNodes = 10,
            Edges =
            [
                (0, 1), (1, 2), (2, 3), (3, 4), (4, 5),
                (5, 6), (6, 7), (7, 8), (8, 9), (9, 0),
                (0, 5), (1, 6), (2, 7), (3, 8), (4, 9)
            ],
            Conflicts = [(0, 2), (1, 3), (4, 6), (5, 7), (8, 0)]
        };
    }
}