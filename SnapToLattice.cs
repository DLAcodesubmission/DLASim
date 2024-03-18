using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ExtensionMethods;
using System.Drawing;
using System.Linq.Expressions;

namespace DLA_Simulation
{
    public class SnapToLatticeGridNearest : OffLattice_1 //snapping to grid 2Zx2Z
    {
        public SnapToLatticeGridNearest(int maxPoints, int seed) : base(maxPoints, seed)
        {
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-outputSnapNear.txt";
        }
        public override void Snap() //snapping rule for point
        {
            Vector2 gridSnap = LastAdded.Floor(); //bottom left grid point to point being added
            Vector2 currentSnap = new();
            Vector2 point;
            float minDist = 2; //garenteed true
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    point = gridSnap + new Vector2(i, j);
                    if (NeighbourInCluster(point)) continue;
                    float dist = Distance(LastAdded, point);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        currentSnap = point;
                    }
                }
            }
            LastAdded = currentSnap;
        }
    }
    public class SnapToTriangle :OffLattice_1 //snapping to "triangle" grid of l1
    {
        static List<Vector2> OffGridSnap = new() //snap points for case floor point is not on grid
        {
            new(1,1),
            new(-1,1),
            new(-1,-1),
            new(1,-1)
        }
            ;
        public SnapToTriangle(int maxPoints,int seed) : base(maxPoints, seed) 
        {
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-outputTriangle.txt";
        }
        public override void Snap()
        {
            Vector2 gridSnap = LastAdded.Floor();
            float minDist = 4;
            float dist;
            Vector2 currentSnap = new();
            Vector2 translated = LastAdded - gridSnap;
            if (IsOnGrid(gridSnap)) //need to determine which region the point is inside and adjust gridSnap accordingly
            {
                
                if (translated.Y > translated.X) gridSnap += Vector2.UnitY;
                else gridSnap += Vector2.UnitX;
            }
            else
            {
                if (translated.X + translated.Y > 1) gridSnap += Vector2.One;
            }
            foreach (Vector2 nearPoint in OffGridSnap)
            {
                if (IsInCluster(nearPoint + gridSnap)) continue;
                dist = Distance(nearPoint + gridSnap, LastAdded);
                if (!(dist<=minDist)) continue;
                minDist = dist;
                currentSnap = nearPoint + gridSnap;
            }
            LastAdded = currentSnap;
        }
        bool IsOnGrid(Vector2 v) {return v.X % 2 == v.Y % 2;}
    }
    public class SnapAlways : OffLattice_1 //snap after every step made to the square lattice
    {
        public SnapAlways(int maxPoints,int seed) :base(maxPoints, seed) 
        {
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-outputSnapAlways.txt";
        }
        public override void Snap() //snapping rule for point
        {
            Vector2 gridSnap = LastAdded.Floor(); //bottom left grid point to point being added
            Vector2 currentSnap = new();
            Vector2 point;
            float minDist = 2; //garenteed true
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    point = gridSnap + new Vector2(i, j);
                    if (NeighbourInCluster(point)) continue;
                    float dist = Distance(LastAdded, point);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        currentSnap = point;
                    }
                }
            }
            LastAdded = currentSnap;
        }
        public override void Snap2()
        {
            Vector2 gridSnap = CurrentPoint.Floor();
            float minDist = 2;
            Vector2 point;
            float dist;
            Vector2 currentSnap=new();
            for (int i=0; i < 2; i++)
            {
                for(int j=0; j < 2; j++)
                {
                    point = gridSnap + new Vector2(i, j);
                    if (NeighbourInCluster(point)) continue;
                    dist = Distance(CurrentPoint, point);
                    if (dist < minDist)
                    {
                        minDist= dist;
                        currentSnap = point;
                    }
                }
            }
            CurrentPoint = currentSnap;
        }
    }
}
