using DDSPC.Data;

namespace DDSPC.Solver;

public abstract class DDSPCSolver
{
    public abstract DDSPCOutput? SolveWithMetrics(DDSPCInput input);
}