#nullable enable
using DDSPC.Data;


namespace DDSPC.Solver;

using System;
using System.Collections.Generic;
using ILOG.Concert;
using ILOG.CPLEX;

class DDSPCSolver
{
    public static DDSPCOutput? Solve(DDSPCInput inputData)
    {
        Cplex cplex = new Cplex();
        INumVar[] x = new INumVar[inputData.NumNodes];
        INumVar[] y = new INumVar[inputData.NumNodes];

        // Definisanje promenljivih
        for (int i = 0; i < inputData.NumNodes; i++)
        {
            x[i] = cplex.BoolVar($"x_{i}");
            y[i] = cplex.BoolVar($"y_{i}");
        }

        // Ciljna funkcija
        ILinearNumExpr objective = cplex.LinearNumExpr();
        for (int i = 0; i < inputData.NumNodes; i++)
        {
            objective.AddTerm(1.0, x[i]);
            objective.AddTerm(1.0, y[i]);
        }

        cplex.AddMinimize(objective);

        // Ograničenja za dominantne skupove
        for (int i = 0; i < inputData.NumNodes; i++)
        {
            ILinearNumExpr exprX = cplex.LinearNumExpr();
            exprX.AddTerm(1.0, x[i]);
            foreach ((int u, int v) in inputData.Edges)
            {
                if (u == i) exprX.AddTerm(1.0, x[v]);
                if (v == i) exprX.AddTerm(1.0, x[u]);
            }

            cplex.AddGe(exprX, 1);

            ILinearNumExpr exprY = cplex.LinearNumExpr();
            exprY.AddTerm(1.0, y[i]);
            foreach ((int u, int v) in inputData.Edges)
            {
                if (u == i) exprY.AddTerm(1.0, y[v]);
                if (v == i) exprY.AddTerm(1.0, y[u]);
            }

            cplex.AddGe(exprY, 1);
        }

        // Ograničenje za disjunktnost
        for (int i = 0; i < inputData.NumNodes; i++)
        {
            cplex.AddLe(cplex.Sum(x[i], y[i]), 1);
        }

        // Ograničenja za konflikte
        foreach ((int m, int n) in inputData.Conflicts)
        {
            cplex.AddLe(cplex.Sum(x[m], y[n]), 1);
            cplex.AddLe(cplex.Sum(y[m], x[n]), 1);
        }

        DDSPCOutput output = new DDSPCOutput
        {
            D1 = new HashSet<int>(),
            D2 = new HashSet<int>(),
        };

        if (cplex.Solve())
        {
            Console.WriteLine($"Solution found: {cplex.ObjValue}");
            output.Value = (int)cplex.ObjValue;
            for (int i = 0; i < inputData.NumNodes; i++)
            {
                if (cplex.GetValue(x[i]) > 0.5)
                {
                    output.D1.Add(i);
                }

                if (cplex.GetValue(y[i]) > 0.5)
                {
                    output.D2.Add(i);
                }
            }
        }
        else
        {
            Console.WriteLine($"Solution not found!");
            return null;
        }

        cplex.End();
        return output;
    }
}