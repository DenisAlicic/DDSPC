#nullable enable
using DDSPC.Data;
using DDSPC.Generators;
using DDSPC.Runners;
using DDSPC.Solver;
using DDSPC.Util;
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
            string experimentName = args.Length > 1
                ? args[1]
                : Path.Combine("Data", "Experiments", "Latest_Experiment");

            string solver = args.Length > 2 ? args[2] : "grasp";

            BatchProcessor processor = new BatchProcessor();
            string experimentPath = Path.Combine(
                ProjectPathHelper.GetDataPath(),
                "Experiments",
                experimentName);

            if (solver == "cplex")
            {
                processor.ProcessCplexExperiment(experimentPath, solver);
            }
            else
            {
                processor.ProcessExperimentParallel(experimentPath, solver);
            }

            Console.WriteLine($"Processing completed. Results in: {experimentPath}");
            return;
        }

        if (args.Length > 0 && args[0] == "generate_table")
        {
            string experimentName = args.Length > 1
                ? args[1]
                : Path.Combine("Data", "Experiments", "Latest_Experiment");
            TableGenerator tableGenerator = new TableGenerator();
            string experimentPath = Path.Combine(
                ProjectPathHelper.GetDataPath(),
                "Experiments",
                experimentName);
            tableGenerator.LoadDataAndGenerateTable(experimentPath);
        }
    }
}