using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Diagnostics;

namespace DLA_Simulation
{
    public class SimpleLattice //simplistic example single step random walk algorithm for lattice DLA
    {
        Vector2 CurrentPoint;
        Random Random;
        int NumPoints;
        int MaxPoints;
        int Steps;
        float BoundingRadius; //max radius of cluster
        string FileName;
        HashSet<Vector2> Points;
        List<Vector2> Neighbours = new List<Vector2> 
        { 
            Vector2.UnitY,
            Vector2.UnitX,
            -1*Vector2.UnitY,
            -1*Vector2.UnitX
        };

        public SimpleLattice(int maxPoints, int seed)
        {
            MaxPoints = maxPoints;
            Random r = new Random(seed);
            Random = new Random(r.Next() ^ r.Next()); //delineates seeding while maintaining reproducability
            NumPoints = 1;
            Steps = 0;
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-outputSimL.txt";
            BoundingRadius = 1;
            Points = new HashSet<Vector2> { Vector2.Zero };
            NewPoint();
        }
        float Distance(Vector2 point1, Vector2 point2) //distance 
        {
            return Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y);
        }
        void NewPoint() //make new point
        {
            Steps= 0;
            int Xval = Random.Next(0, (int)BoundingRadius + 1);
            Vector2 move = new Vector2(Xval, BoundingRadius - Xval);
            CurrentPoint =  Vector2.Transform(move, Matrix3x2.CreateRotation((float)Math.PI * Random.Next(4) / 2));
        }
        bool StepPoint()
        {
            if (Steps > 100000)
            {
                NewPoint();
            }
            Steps++;
            foreach (Vector2 neighbour in Neighbours)
            {
                if (Points.Contains(CurrentPoint + neighbour))
                {
                    return false;
                }
            }
            CurrentPoint += Neighbours[Random.Next(4)];
            return true;
        }
        public double Run()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DLAfiles";
            using (StreamWriter DLAoutput = new StreamWriter(Path.Combine(docPath, FileName)))
            {
                DLAoutput.WriteLine("0:0");
                for (int i = 0; i < MaxPoints - 1; i++)
                {
                    while (true) //iteration of random point movement
                    {
                        if (!StepPoint())
                        {
                            break;
                        }
                    }
                    Points.Add(CurrentPoint);
                    float distance = Distance(CurrentPoint, Vector2.Zero);
                    BoundingRadius = Math.Max(BoundingRadius, distance + 1);
                    NewPoint();
                    DLAoutput.WriteLine("{0}:{1}", CurrentPoint.X, CurrentPoint.Y);
                }
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
    }
}
