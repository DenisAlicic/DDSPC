#nullable enable
using DDSPC.Data;
using DDSPC.Generators;
using DDSPC.Solver;
using DDSPC.Visualization;

namespace DDSPC;

class Program
{
    [STAThread]
    public static void Main()
    {
        DDSPCGraphGenerator generator = new DDSPCGraphGenerator(42);
        DDSPCInput input = generator.GenerateConnectedGraph(15, 0.3, 0.05);

        DDSPCOutput? cplexOutput = DDSPCCplexSolver.Solve(input);
        DDSPCGRASP graspSolver = new DDSPCGRASP(seed: 42, alpha: 0.5f);
        DDSPCOutput? graspOutput = graspSolver.Solve(input);

        if (cplexOutput != null && graspOutput != null)
        {
            if (cplexOutput.Value == graspOutput.Value)
            {
                Console.WriteLine("Grasp solved same as cplex!");
            }
            else if (cplexOutput.Value < graspOutput.Value)
            {
                Console.WriteLine("Grasp didn't find optimal solution!");
            }
            else
            {
                Console.WriteLine("This should never happen! Grasp found better solution than cplex!");
            }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(input, graspOutput));
    }
}