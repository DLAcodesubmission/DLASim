using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static alglib;

namespace DLA_Simulation
{
    public class DiscretePoissonSolver //ONLY INPUT ODD n,used for precalcing jump distributions on squares
    {
        sparsematrix Poisson;
        int Size;
        public DiscretePoissonSolver(int n) 
        {
            Size = n;
            Poisson = new sparsematrix();
            SetMatrix();
        }
        void SetMatrix() //setting the block matrix for calculations
        {
            sparsecreate(Size*Size, Size*Size, out Poisson);
            for (int index = 0; index < Size*Size; index++)
            {
                sparseset(Poisson, index,index,4);
                if (index % Size !=0)
                {
                    sparseset(Poisson, index - 1, index, -1);
                    sparseset(Poisson, index, index - 1, -1);
                }
                if (index > Size - 1)
                {
                    sparseset(Poisson,index, index - Size, -1);
                    sparseset(Poisson,index - Size,index, -1);
                }
            }
            sparseconverttocrs(Poisson);
        }
        void Solve(out double[] distribution)
        {
            double[] vars;
            double[] values = new double[Size * Size];
            for (int i = 0; i < Size * Size; i++)
            {
                values[i] = 0;
            }
            values[(Size * Size - 1) / 2] = 1;

            sparsesolverreport rep; 
            sparsesolve(Poisson, values, out vars, out rep);
            distribution = vars[0..Size];
        }
        void Cumulative(out double[] cumDist)
        {
            Solve(out double[] dist);
            cumDist = new double[Size];
            cumDist[0] = dist[0];
            for (int i = 1; i < Size;i++)
            {
                cumDist[i] = cumDist[i-1]+dist[i];
            }
            double total = dist.Sum();
            for (int i = 1;i < Size; i++)
            {
                cumDist[i] = cumDist[i] / total;
            }
        }
        public static void PreCalc(int n) //precalcs the distributions for lattice
        {
            if (n % 2 == 0)
            {
                Console.WriteLine("not even value");
                return;
            }
            DiscretePoissonSolver poisson = new(n);
            poisson.Cumulative(out double[] dist);
            Console.WriteLine("{");
            for (int index = 0; index < dist.Length; index++)
            {
                Console.WriteLine(dist[index].ToString() + ",");
            }
            Console.WriteLine("}");
        }
    }
}
