using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtensionMethods;
using System.Diagnostics;
using System.IO;
using System.Data.Common;
using static System.Formats.Asn1.AsnWriter;

namespace DLA_Simulation
{
    public class OffLattice_1 //l1 off lattice
    {
        public List<HashSet<Vector2>> Scales; //list of points at each non base points scales
        public Vector2 CurrentPoint;
        public Vector2 LastAdded; //last added point
        public float BoundingRadius; //max radius of cluster
        public Random Random;
        public int NumPoints;
        public int MaxPoints;
        public int Steps;
        public string FileName;
        bool Save;
        static HashSet<Vector2> OneDistNeighbours = new()
        {
            Vector2.UnitX,
            Vector2.UnitY,
            -Vector2.UnitX,
            -Vector2.UnitY
        };
        public Dictionary<Vector2, HashSet<Vector2>> Regions; //points stored by location in plane
        static List<Vector2> Neighbours = new List<Vector2> 
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.Negate(Vector2.One),
            new Vector2(1,-1),
            new Vector2(-1,1),
            Vector2.UnitX,
            Vector2.UnitY,
            Vector2.Negate(Vector2.UnitX),
            Vector2.Negate(Vector2.UnitY) 
        }; //neighbours hardcoded to not remake every time

        public OffLattice_1(int maxPoints, int seed,bool save=true)
        {
            MaxPoints = maxPoints;
            Random r = new Random(seed);
            Random = new Random(r.Next()^r.Next()); //delineates seeding while maintaining reproducability
            NumPoints = 1;
            Save = save;
            Steps = 0;
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-output1.txt";
            BoundingRadius = 1f; //points are balls radius 1
            NewPoint();
            Scales = new List<HashSet<Vector2>> //using 6 levels of resolution to check
            {

                new HashSet<Vector2> { Vector2.Zero },//radius 2
                new HashSet<Vector2> { Vector2.Zero },//radius 4
                new HashSet<Vector2> { Vector2.Zero },//radius 8
                new HashSet<Vector2> { Vector2.Zero },//radius 16
                new HashSet<Vector2> { Vector2.Zero },//radius 32
                new HashSet<Vector2> { Vector2.Zero } //radius 64
            };
            Regions = new Dictionary<Vector2, HashSet<Vector2>>
            {
                {Vector2.Zero, new HashSet<Vector2> {Vector2.Zero} }
            };
        }
        public float Distance(Vector2 point1,Vector2 point2) //distance 
        {
            return Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y-point2.Y);
        }
        public Vector2 MovePoint()
        {
            return Vector2.Transform(Vector2.UnitX, Matrix3x2.CreateRotation(2*MathF.PI * (float)Random.NextDouble()));
        }

        public void NewPoint()
        {
            CurrentPoint = (BoundingRadius+10)*MovePoint();
            Steps = 0;
        }
        bool PoissonJump() //moves point poisson distributed jump to an l2 bounding radius of cluster MBIUS TRANSFORM "CRUSHES" IMAG COMPONENT ON JUMP
        {
            Complex insidePointCpx = (BoundingRadius+1)/CurrentPoint.ToComplex();
            Complex randomPoint = MovePoint().ToComplex();
            CurrentPoint = ((Complex.Conjugate(insidePointCpx)*randomPoint-1)/(randomPoint-insidePointCpx)).ToVector2()*(BoundingRadius+1);
            return true;
        }
        bool InsideJump() //handles optimised jumps inside bounding region,scale regions based off of https://doi.org/10.48550/arXiv.1407.2586
        {
            for (int scale = 5;scale>=0;scale--) //iterate on largest scale 1st, working down
            {
                Vector2 scaleRegion = Vector2.Divide(CurrentPoint,1<<(scale+2)).Floor(); //+2 needed to get the region coords to the correct scale
                bool hasNeighbour = false;
                foreach (Vector2 neighbour in Neighbours) //check own region then next ring of regions out
                {
                    if (Scales[scale].Contains(neighbour + scaleRegion)) 
                    {
                        hasNeighbour = true;
                        break;
                    }
                }
                if (hasNeighbour) continue; //go to next scale to check for open region

                //jump to edge of checked neighbour regions
                Vector2 centre = (scaleRegion * 2 + new Vector2(1, 1)) * (1 << (scale + 1));
                Vector2 diff = centre - CurrentPoint;
                float centreDist = Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y));
                float dist = (1 << (scale + 2)) + (1 << (scale + 1)) - centreDist;
                if (dist > 2) //bcs of ball assumsions require the jump to be at least 2
                {
                    CurrentPoint += MovePoint() * (dist-2); 
                    return true;
                }

                throw new Exception("points too close");
                
            }
            //manual search required within local radius
            Vector2 region =  Vector2.Divide(CurrentPoint,4).Floor();
            float minDistance = DistanceToCluster(region,1,true); //local manual search
            if (minDistance < 2 + 1e-9)
            {
                return false;
            }

            CurrentPoint += MovePoint() * DistanceToClusterEuclidian(region); //calcs minimum distance can jump without colliding with cluster with l2 metric, and jumps
            return true;
        }
        public bool StepPoint()
        {
            if (Steps++ > 10000)
            {
                NewPoint();//timeout to prevent too long looping on 1 point
            }
            float distanceSquared = Vector2.DistanceSquared(CurrentPoint, Vector2.Zero);
            //outside bounding region of cluster
            if (distanceSquared > (BoundingRadius + 1) * (BoundingRadius + 1) + 1e-9) 
            {
                PoissonJump();
                Snap2();
                return true;
            }
            //within bounding region of cluster
            bool val = InsideJump();
            Snap2();
            return val;//if return false then add point to cluster(as close enough)
        }
        public virtual void Snap() { return; } //empty for subclassing
        public virtual void Snap2() { return; }
        
        public void AddPoint() //add the point to the cluster
        {
            //increment values
            NumPoints++;
            LastAdded = CurrentPoint;
            Snap();
            //taking maximum euclidian distance to origin along boundary line segment of LastAdded
            Vector2 absPoint = Vector2.Abs(LastAdded);
            BoundingRadius = Math.Max(BoundingRadius, Vector2.Distance(LastAdded,Vector2.Zero)+1);

            //add regions for the point:
            Vector2 scaleRegion = (CurrentPoint/4).Floor();
            if (!Scales[0].Contains(scaleRegion)) //seperate handling due to adding the point to a region
            {
                Scales[0].Add(scaleRegion);
                Regions.Add(scaleRegion, new HashSet<Vector2> { LastAdded });
            }
            else Regions[scaleRegion].Add(LastAdded);

            for (int scale = 5; scale >= 1; scale--) //iterate on largest scale 1st, working down
            {
                scaleRegion = Vector2.Divide(CurrentPoint, 1 << (scale+2)).Floor();
                if (!Scales[scale].Contains(scaleRegion)) Scales[scale].Add(scaleRegion); //add region to list of scaled regions with points if needed
            }
            if (NumPoints < MaxPoints)
            {
                NewPoint(); //continue looping if not at goal number of points
            }
        }
        public bool IsInCluster(Vector2 point) //finds if a given point is in the cluster, used for hybrid simulation
        {
            Vector2 region = Vector2.Divide(CurrentPoint, 4).Floor(); //gets region of point
            if (!Scales[0].Contains(region)) return false;
            return Regions[region].Contains(point); //return if region contains point
        }
        public bool NeighbourInCluster(Vector2 point) //checks for point or neighbour in cluster
        {
            Vector2 region = (point / 4).Floor();
            HashSet<Vector2> localPoints = new HashSet<Vector2>();
            foreach (Vector2 offset in OneDistNeighbours)
            {
                if (!Scales[0].Contains(region + offset)) continue;
                localPoints.UnionWith(Regions[region + offset]);

            }
            foreach (Vector2 offset in OneDistNeighbours)
            {
                if (localPoints.Contains(offset + point)) return true;
            }
            return false;
        }
        public float DistanceToCluster(Vector2 region,int range = 1, bool earlyReturn = false) //only searches local area,range of local search(in grid)
        {
            Vector2 RegionCentre = 4 * region + new Vector2(2, 2);
            float minDistance = 2 + 4*range - Distance(RegionCentre, CurrentPoint); //larger radius could escape the checked regions(when used to jump)
            float intitialDist = minDistance;
            HashSet<Vector2> neighbourRegion = new();
            foreach (Vector2 neighbour in Neighbours)
            {
                if (Regions.TryGetValue(neighbour + region, out neighbourRegion))
                {
                    foreach (Vector2 point in neighbourRegion)
                    {
                        minDistance = Math.Min(minDistance, Distance(CurrentPoint, point));
                        if (earlyReturn && minDistance < 2+1e-9) //condition for adding point to cluster
                        {
                            LastAdded = CurrentPoint;
                            return minDistance;
                        }
                    }
                }
            }
            if (earlyReturn)
            {
                return minDistance;
            }
            if (intitialDist==minDistance)
            { //next ring of relative regions
                HashSet<Vector2> nextNeighbourRing = new HashSet<Vector2> 
                {
                    new Vector2(2, 2),
                    new Vector2(2,1),
                    new Vector2(2,0),
                    new Vector2(2,-1),
                    new Vector2(2,-2),
                    new Vector2(1,-2),
                    new Vector2(0,-2),
                    new Vector2(-1,-2),
                    new Vector2(-2,-2),
                    new Vector2(-2,-1),
                    new Vector2(-2,0),
                    new Vector2(-2,1),
                    new Vector2(-2,2),
                    new Vector2(-1,2),
                    new Vector2(0,2),
                    new Vector2(1,2)
                };
                foreach (Vector2 neighbour in nextNeighbourRing)
                {
                    if (Regions.ContainsKey(neighbour + region))
                    {
                        foreach (Vector2 point in Regions[neighbour + region])
                        {
                            minDistance = Math.Min(minDistance, Distance(CurrentPoint, point));
                        }
                    }
                } 
            }
            return minDistance;
        }
        public float DistanceToClusterEuclidian(Vector2 region)
        {
            Vector2 regionCentre = 4*region + new Vector2(2, 2);
            Vector2 diff = Vector2.Abs(CurrentPoint - regionCentre);
            float maxDist = 4 - Math.Max(diff.X,diff.Y); //cannot escape the bounded region checked
            float minDistSquared = maxDist*maxDist;
            HashSet<Vector2> neighbourRegion;
            foreach (Vector2 neighbour in Neighbours)
            {
                if (Regions.TryGetValue(neighbour + region, out neighbourRegion))
                {
                    foreach (Vector2 point in neighbourRegion)
                    {
                        minDistSquared = Math.Min(EuclSqDistToL1Ball(point - CurrentPoint), minDistSquared);
                    }
                }
            }
            return MathF.Sqrt(minDistSquared);


        }
        float EuclSqDistToL1Ball(Vector2 point)
        {
            point = Vector2.Abs(point);
            float minimiser = Math.Min(Math.Max(0, (point.Y - point.X) / 2 + 1),2);
            return (point.X - 2 + minimiser) * (point.X - 2 + minimiser) + (point.Y - minimiser) * (point.Y - minimiser);
        }
        public double Run()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+ "\\DLAfiles";
            using (StreamWriter DLAoutput = new StreamWriter(Path.Combine(docPath, FileName)))
            {
                DLAoutput.WriteLine("0:0");
                for (int i = 0; i < MaxPoints-1; i++)
                {
                    while (true) //iteration of random point movement
                    {
                        if (!StepPoint()) break;
                    }
                    AddPoint();
                    if (!Save) continue; //skip writing to file for speed testing to save on storage
                    DLAoutput.WriteLine("{0}:{1}",LastAdded.X,LastAdded.Y); //removed writing bounding radius to file for memory
                }
                if (!Save) DLAoutput.WriteLine(stopwatch.Elapsed.ToString());
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
    }
    public class OffLattice_2 //l2 off lattice modified off l1 case, noticiably less efficient
    {
        public List<HashSet<Vector2>> Scales; //list of points at each non base points scales
        public Vector2 CurrentPoint;
        public Vector2 LastAdded; //last added point
        public float BoundingRadius; //max radius of cluster
        public Random Random;
        public int NumPoints;
        public int MaxPoints;
        public int Steps;
        public string FileName;
        public Dictionary<Vector2, HashSet<Vector2>> Regions; //points stored by location in plane
        static List<Vector2> Neighbours = new List<Vector2>
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.Negate(Vector2.One),
            new Vector2(1,-1),
            new Vector2(-1,1),
            Vector2.UnitX,
            Vector2.UnitY,
            Vector2.Negate(Vector2.UnitX),
            Vector2.Negate(Vector2.UnitY)
        }; //neighbours hardcoded to not remake every time


        public OffLattice_2(int maxPoints, int seed)
        {
            MaxPoints = maxPoints;
            Random r = new Random(seed);
            Random = new Random(r.Next() ^ r.Next()); //delineates seeding while maintaining reproducability
            NumPoints = 1;
            Steps = 0;
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-output2.txt";
            BoundingRadius = 1f; //points are balls radius 1
            NewPoint();
            Scales = new List<HashSet<Vector2>> //using 6 levels of resolution to check
            {

                new HashSet<Vector2> { Vector2.Zero },//radius 2
                new HashSet<Vector2> { Vector2.Zero },//radius 4
                new HashSet<Vector2> { Vector2.Zero },//radius 8
                new HashSet<Vector2> { Vector2.Zero },//radius 16
                new HashSet<Vector2> { Vector2.Zero },//radius 32
                new HashSet<Vector2> { Vector2.Zero } //radius 64
            };
            Regions = new Dictionary<Vector2, HashSet<Vector2>>
            {
                {Vector2.Zero, new HashSet<Vector2> {Vector2.Zero} }
            };
        }
        public Vector2 MovePoint()
        {
            return Vector2.Transform(Vector2.UnitX, Matrix3x2.CreateRotation((float)Random.NextDouble()*MathF.PI*2));
        }

        public void NewPoint()
        {
            CurrentPoint = (BoundingRadius + 1) * MovePoint();
            Steps = 0;
        }
        public bool InsideJump() //handles optimised jumps inside bounding region, based off of https://doi.org/10.48550/arXiv.1407.2586
        {
            for (int scale = 5; scale >= 0; scale--) //iterate on largest scale 1st, working down
            {
                Vector2 scaleRegion = Vector2.Divide(CurrentPoint, 1 << (scale + 2)).Floor(); //+2 needed to get the region coords to the correct scale
                bool hasNeighbour = false;
                foreach (Vector2 neighbour in Neighbours) //check own region then next ring of regions out
                {
                    if (Scales[scale].Contains(neighbour + scaleRegion))
                    {
                        hasNeighbour = true;
                        break;
                    }
                }
                if (hasNeighbour) continue; //go to next scale to check for open region

                //jump to edge of checked neighbour regions
                Vector2 regionCentre = (scaleRegion * 2 + new Vector2(1, 1)) * (1 << (scale + 1));
                Vector2 diff_ = regionCentre - CurrentPoint;
                float centreDist_ = Math.Max(Math.Abs(diff_.X), Math.Abs(diff_.Y));
                float dist_ = (1 << (scale + 2)) + (1 << (scale + 1)) - centreDist_;
                if (dist_ > 2) //bcs of ball assumsions require the jump to be at least 2
                {
                    CurrentPoint += MovePoint() * (dist_ - 2);
                    return true;
                }

                throw new Exception("points too close");

            }
            //manual search required within local radius
            Vector2 region = Vector2.Divide(CurrentPoint, 4).Floor();
            Vector2 centre = region * 4 + new Vector2(2, 2);
            Vector2 diff = centre - CurrentPoint;
            float centreDist = Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y));
            float dist = 6 - centreDist;
            float minDistanceSquared = dist*dist; //a larger radius could escape the checked region
            HashSet<Vector2> neighbourRegion = new();
            foreach (Vector2 neighbour in Neighbours)
            {
                if (Regions.TryGetValue(neighbour + region, out neighbourRegion))
                {
                    foreach (Vector2 point in Regions[neighbour + region])
                    {
                        minDistanceSquared = Math.Min(minDistanceSquared, Vector2.DistanceSquared(CurrentPoint, point));
                        if (minDistanceSquared <= 4+1e-9) //condition for adding point to cluster
                        {
                            LastAdded = CurrentPoint;
                            return false;
                        }
                    }
                }
            }
            CurrentPoint += MovePoint() * (MathF.Sqrt(minDistanceSquared) - 2);
            return true;
        }
        public bool StepPoint()
        {
            if (Steps++ > 10000) NewPoint();//timeout to prevent too long looping on 1 point

            float distanceSquared = Vector2.DistanceSquared(CurrentPoint, Vector2.Zero);
            //outside bounding region of cluster
            if (Steps++ > 10000) //timeout to prevent too long looping on 1 point
            {
                NewPoint();
                return true;
            }
            if (distanceSquared > (BoundingRadius+1)*(BoundingRadius+1)+1e-9) return PoissonJump();

            //within bounding region of cluster
            return InsideJump();//if return false then add point to cluster(as close enough)
        }
        bool PoissonJump() //moves point poisson distributed jump to an l2 bounding radius of cluster MBIUS TRANSFORM "CRUSHES" IMAG COMPONENT ON JUMP
        {
            Complex insidePointCpx = (BoundingRadius + 1) / CurrentPoint.ToComplex();
            Complex randomPoint = MovePoint().ToComplex();
            CurrentPoint = ((Complex.Conjugate(insidePointCpx) * randomPoint - 1) / (randomPoint - insidePointCpx)).ToVector2() * (BoundingRadius + 1);
            return true;
        }
        public void AddPoint() //add the point to the cluster
        {
            //increment values
            NumPoints++;
            LastAdded = CurrentPoint;
            float distance = Vector2.Distance(LastAdded, Vector2.Zero);
            BoundingRadius = Math.Max(BoundingRadius, distance + 1); //+1 for radius of ball

            //add regions for the point:
            Vector2 scaleRegion = Vector2.Divide(CurrentPoint,4).Floor();
            if (!Scales[0].Contains(scaleRegion)) //seperate handling due to adding the point to a region
            {
                Scales[0].Add(scaleRegion);
                Regions.Add(scaleRegion, new HashSet<Vector2> { LastAdded });
            }
            else Regions[scaleRegion].Add(LastAdded);

            for (int scale = 5; scale >= 1; scale--) //iterate on largest scale 1st, working down
            {
                scaleRegion = Vector2.Divide(CurrentPoint, 1 << (scale + 2)).Floor();
                if (!Scales[scale].Contains(scaleRegion)) Scales[scale].Add(scaleRegion); //add region to list of scaled regions with points if needed
            }
            if (NumPoints < MaxPoints)
            {
                NewPoint(); //continue looping if not at goal number of points
            }
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
                        if (!StepPoint()) break;
                    }
                    AddPoint();
                    //Console.WriteLine(NumPoints);
                    DLAoutput.WriteLine("{0}:{1}", LastAdded.X, LastAdded.Y);
                }
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
    }


}
