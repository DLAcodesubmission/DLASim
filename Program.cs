using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DLA_Simulation
{
    public class Program
    {
        static void Main()
        {
            Console.WriteLine(System.DateTime.Now);
            SnapAlways lattice2 = new(5000000, 9933734);
            Console.WriteLine(lattice2.Run());
            //for (int seed = 997344470; seed < 997344500; seed++)
            {
            //   SnapToTriangle lattice = new(500000, seed);
            //    Console.WriteLine(lattice.Run());
            }   
        }
    }
}