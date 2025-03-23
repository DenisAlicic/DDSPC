#nullable enable
using DDSPC.Data;
using DDSPC.Generators;
using DDSPC.Solver;

namespace DDSPC;

class Program
{
    [STAThread]
    public static void Main()
    {
        // DDSPCInput input = DDSPCInput.Example01();
        DDSPCInput input = InstanceGenerator.GenerateCyclicGraph(7, 4, 42);
        DDSPCOutput? output = DDSPCSolver.Solve(input);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(input, output));
    }
}