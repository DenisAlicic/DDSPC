namespace DDSPC.Data;

public class DDSPCInput
{
    public int NumNodes;
    public List<(int, int)> Edges;
    public List<(int, int)> Conflicts;

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