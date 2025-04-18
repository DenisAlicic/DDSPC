using System.Text.Json.Serialization;

namespace DDSPC.Data;

public class DDSPCOutput
{
    public int NumNodes { get; set; }
    public HashSet<int> D1 { get; set; } = new HashSet<int>();
    public HashSet<int> D2 { get; set; } = new HashSet<int>();
    public int Value { get; set; }
    public string Solver { get; set; }

    [JsonIgnore] public TimeSpan Runtime { get; set; }

    [JsonIgnore] public string GraphName { get; set; }

    public double GapPercent { get; set; }

    public DDSPCOutput()
    {
    }
}