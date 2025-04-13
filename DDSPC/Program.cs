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

        if (args.Length > 0 && args[0] == "process")
        {
            string experimentPath = args.Length > 1
                ? args[1]
                : Path.Combine("Data", "Experiments", "Latest_Experiment");

            new BatchProcessor().ProcessExperiment(experimentPath);
            Console.WriteLine($"Processing completed. Results in: {experimentPath}");
            return;
        }
    }
}