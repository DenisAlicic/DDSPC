#nullable enable
using DDSPC.Data;
using DDSPC.Generators;
using DDSPC.Runners;
using DDSPC.Solver;
using DDSPC.Visualization;

namespace DDSPC;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "multiseed")
        {
            new ExperimentManager().RunMultiSeedExperiment();
            return;
        }

        Console.WriteLine("Dostupne komande:");
        Console.WriteLine("dotnet run -- multiseed - Pokreće eksperiment sa više seedova");
    }
}