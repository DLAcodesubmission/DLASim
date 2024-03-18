using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DLA_Simulation
{
    public class Lattice //optimised algorithm for lattice DLA
    {
        List<HashSet<Vector2>> Scales; //list of points at each non base points scales
        Vector2 CurrentPoint;
        Vector2 LastAdded; //last added point
        float BoundingRadius; //max radius of cluster
        Random Random;
        int NumPoints;
        int MaxPoints;
        int Steps;
        string FileName;
        Dictionary<Vector2, HashSet<Vector2>> Regions; //points stored by location in plane
        static List<KeyValuePair<int,Vector2>> GaussianBlurDistribution = new()
        {
            new KeyValuePair<int, Vector2> (24,Vector2.UnitX),
            new KeyValuePair<int,Vector2> (6,2*Vector2.UnitX),
            new KeyValuePair<int, Vector2> (16,Vector2.One),
            new KeyValuePair<int, Vector2>(4,new(2,1)),
            new KeyValuePair<int, Vector2>(1,new(2,2))
        };
        static List<double[]> JumpDistributions = new()
            { 
            new double[] {0.0625,0.75,1}, //3,radius 2
            new double[] { //7, radius 4
                            0.016544117647058824,
                            0.1985294117647059,
                            0.38970588235294124,
                            0.6102941176470589,
                            0.8014705882352942,
                            0.9338235294117647,
                            1
                        },
            new double[] //15, radius 8
            {
                0.004234620077050111,
0.050815440924601316,
0.10149584381480672,
0.168439536841233,
0.25031578028603113,
0.34458093847016474,
0.44720132516714706,
0.5527986748328528,
0.6554190615298352,
0.7496842197139688,
0.8315604631587669,
0.8985041561851932,
0.9491845590753987,
0.9830615196917996,
1
            },
            new double[] { //31, radius 16
0.0010659935304669185,
0.012791922365603038,
0.025581591383504715,
0.04262396778476725,
0.06389654190336533,
0.08935443727317804,
0.11891964256192054,
0.15246881131712495,
0.18982041424880547,
0.23072241733913462,
0.274842056233174,
0.32175957401289196,
0.3709678337897314,
0.42187934985699976,
0.47384140487930654,
0.5261585951206931,
0.578120650143,
0.6290321662102684,
0.6782404259871078,
0.7251579437668259,
0.7692775826608652,
0.8101795857511944,
0.8475311886828749,
0.8810803574380794,
0.910645562726822,
0.9361034580966346,
0.9573760322152327,
0.9744184086164953,
0.987208077634397,
0.9957360258781324,
1
},
            new double[] { //63, radius 32
0.0002669805333484599,
0.003203766400181547,
0.00640749691282939,
0.010678970121202561,
0.01601782717495012,
0.022423350523286773,
0.029894285001934157,
0.03842862390615744,
0.04802336092087461,
0.058674209295283296,
0.07037529032850874,
0.08311879408723648,
0.09689461631112374,
0.11168997667113902,
0.12748902490990027,
0.14427244287412538,
0.16201705198952215,
0.18069543724724657,
0.2002756001647232,
0.22072065432715116,
0.24198857786817568,
0.2640320374602953,
0.28679829791421413,
0.3102292302098766,
0.3342614286192402,
0.35882644451075285,
0.3838511405035301,
0.40925816400865084,
0.4349665340885475,
0.46089233029525245,
0.48694946708268516,
0.5130505329173136,
0.5391076697047463,
0.5650334659114512,
0.5907418359913478,
0.6161488594964686,
0.6411735554892458,
0.6657385713807585,
0.6897707697901222,
0.7132017020857847,
0.7359679625397036,
0.7580114221318233,
0.7792793456728477,
0.7997243998352758,
0.8193045627527525,
0.837982948010477,
0.8557275571258738,
0.872510975090099,
0.8883100233288603,
0.9031053836888757,
0.916881205912763,
0.9296247096714908,
0.9413257907047163,
0.9519766390791251,
0.9615713760938424,
0.9701057149980657,
0.9775766494767131,
0.9839821728250499,
0.9893210298787974,
0.9935925030871706,
0.9967962335998185,
0.9989320778666062,
1
},
            new double[] { //127, radius 64
6.677566405168827E-05,
0.0008013079686202755,
0.0016026153736991727,
0.0026710226172779366,
0.0040065240639676645,
0.005609108443139846,
0.007478756031741985,
0.009615435274442667,
0.012019098841976704,
0.014689679129083005,
0.017627083194123202,
0.020831187143361858,
0.024301829964003065,
0.028038806811435835,
0.03204186175776414,
0.03631068001060685,
0.040844879613366705,
0.045644002640703035,
0.05070750590581227,
0.056034751199335917,
0.061624995083281335,
0.06747737826726026,
0.07359091459861761,
0.07996447970262996,
0.08659679931388067,
0.09348643734514243,
0.10063178374558267,
0.10803104220581153,
0.11568221777315865,
0.123583104446533,
0.13173127282621086,
0.14012405789982627,
0.14875854705160582,
0.15763156838738868,
0.16673967947308294,
0.17607915658880546,
0.18564598460489654,
0.19543584758915614,
0.20544412025687186,
0.21566586037635593,
0.22609580224264464,
0.23672835133060555,
0.24755758023582644,
0.25857722600722527,
0.2697806889692369,
0.2811610331236465,
0.29271098821162234,
0.3044229535052605,
0.3162890033850325,
0.3283008947450094,
0.34045007625174617,
0.35272769946541616,
0.36512463181339105,
0.3776314713872232,
0.3902385635141746,
0.4029360190343779,
0.4157137341947354,
0.42856141205112275,
0.44146858525173266,
0.45442464005681693,
0.46741884143403023,
0.4804403590543639,
0.4934782940015842,
0.5065217059984145,
0.5195596409456349,
0.5325811585659685,
0.5455753599431817,
0.5585314147482661,
0.5714385879488759,
0.5842862658052633,
0.5970639809656207,
0.6097614364858241,
0.6223685286127756,
0.6348753681866077,
0.6472723005345826,
0.6595499237482526,
0.6716991052549895,
0.6837109966149664,
0.6955770464947384,
0.7072890117883766,
0.7188389668763524,
0.730219311030762,
0.7414227739927737,
0.7524424197641725,
0.7632716486693933,
0.7739041977573543,
0.7843341396236431,
0.7945558797431271,
0.8045641524108429,
0.8143540153951025,
0.8239208434111936,
0.8332603205269161,
0.8423684316126104,
0.8512414529483934,
0.8598759421001729,
0.8682687271737884,
0.8764168955534662,
0.8843177822268407,
0.8919689577941877,
0.8993682162544165,
0.9065135626548569,
0.9134032006861187,
0.9200355202973695,
0.9264090854013818,
0.9325226217327393,
0.9383750049167182,
0.9439652488006637,
0.9492924940941874,
0.9543559973592967,
0.959155120386633,
0.9636893199893929,
0.9679581382422356,
0.971961193188564,
0.9756981700359968,
0.9791688128566379,
0.9823729168058767,
0.9853103208709169,
0.9879809011580233,
0.9903845647255572,
0.992521243968258,
0.9943908915568601,
0.9959934759360323,
0.9973289773827221,
0.9983973846263008,
0.9991986920313797,
0.9997328973437932,
1
}
            };

        static HashSet<Vector2> Radius1 = new HashSet<Vector2> { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY };
        static HashSet<Vector2> Radius2 = new HashSet<Vector2> { new(2, 0), new(1, 1), new(0, 2), new(-1, 1), new(-2, 0), new(-1, -1), new(0, -2), new(1, -1) };
        static List<Vector2> Neighbours = new()
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


        public Lattice(int maxPoints, int seed)
        {
            MaxPoints = maxPoints;
            Random r = new Random(seed);
            Random = new Random(r.Next() ^ r.Next()); //delineates seeding while maintaining reproducability
            NumPoints = 1;
            Steps = 0;
            FileName = seed.ToString() + "-" + MaxPoints.ToString() + "-outputSL.txt";
            BoundingRadius = 1; //points are balls radius 1
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
        float Distance(Vector2 point1, Vector2 point2) //distance 
        {
            return Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y);
        }
        Vector2 MovePoint(float radius) //needs redo as algorithm doesnt work as convergence shit, replace with discrete poisson/greens function solver
        {
            int Xval = Random.Next(0, (int)radius + 1);
            Vector2 move = new Vector2(Xval, radius - Xval);
            return Vector2.Transform(move, Matrix3x2.CreateRotation((float)Math.PI * Random.Next(4) / 2));
        }

        void NewPoint()
        {
            Steps = 0;
            CurrentPoint = 2*(BoundingRadius + 15) * Vector2.Transform(Vector2.UnitX, Matrix3x2.CreateRotation(2*MathF.PI*(float)Random.NextDouble())); //change to uniform on l2 circle far out, floor position, then add the gaussian blur
            CurrentPoint = CurrentPoint.Round()+GaussianBlur(); //discretise and reduce discretisation error via applying a gaussian blur
        }
        void JumpSquare(int scale) //takes the distribution precalcd on the square and jumps to it
        {
            double[] distribution = JumpDistributions[scale];
            double comparision = Random.NextDouble();
            Vector2 move = (1 << scale+ 2) * Vector2.UnitX;
            for (int index = 0; index < distribution.Length; index++)
            {
                if (comparision <= distribution[index])
                {
                    move.Y = index - (distribution.Length - 1) / 2;
                    break;
                }
            }
            CurrentPoint += Vector2.Transform(move, Matrix3x2.CreateRotation((float)Math.PI * Random.Next(4) / 2)); 
        }
        public Vector2 GaussianBlur() //gaussian blur for reducing error from point generation with 5x5 gaussian blur kernel
        {
            int offSetCase = Random.Next(64);
            offSetCase -= 9;
            if (offSetCase < 0) return Vector2.Zero;
            Vector2 gaussianOffset = new();
            foreach (KeyValuePair<int,Vector2> pair in GaussianBlurDistribution)
            {
                offSetCase -= pair.Key;
                if (offSetCase < 0)
                {
                    gaussianOffset = pair.Value;
                    break;
                }
            }
            return Vector2.Transform(gaussianOffset,Matrix3x2.CreateRotation(MathF.PI*Random.Next(4)/2)); 
        }
        public bool InsideJump() //handles optimised jumps inside bounding region,loosely based off of https://doi.org/10.48550/arXiv.1407.2586
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

                //jump maxiumum safe radius on a square
                JumpSquare(scale);
                return true;
            }

            //manual search required within local radius
            Vector2 region = Vector2.Divide(CurrentPoint, 4).Floor();

            HashSet<Vector2> neighbourRegion = new();
            HashSet<Vector2> localRegion = new();
            foreach (Vector2 neighbour in Neighbours)
            {
                if (Regions.TryGetValue(neighbour + region, out neighbourRegion))
                {
                    localRegion.UnionWith(neighbourRegion);
                }
            }
            HashSet<Vector2> radius;
            radius = Radius1.Offset(CurrentPoint);
            radius.IntersectWith(localRegion);
            if (radius.Count > 0) return false; //neighbour in cluster, add to cluster

            radius = Radius2.Offset(CurrentPoint); //check radius 2 points
            radius.IntersectWith(localRegion);
            if (radius.Count > 0)
            {
                CurrentPoint += Vector2.Transform(Vector2.UnitX, Matrix3x2.CreateRotation(MathF.PI*Random.Next(4)/2)); //jump 1 step
                return true;
            }
            CurrentPoint += Vector2.Transform(Random.Next(3) == 0 ? new Vector2(2,0) : Vector2.One , Matrix3x2.CreateRotation(MathF.PI * Random.Next(4) / 2)); //jump 2 steps
            
            return true;
        }
        public bool StepPoint()
        {
            if (Steps++ > 10000) NewPoint();//timeout to prevent too long looping on 1 point

            float distanceSquared = Vector2.DistanceSquared(CurrentPoint, Vector2.Zero);
            //outside bounding region of cluster
            while (distanceSquared > (BoundingRadius + 1)*(BoundingRadius + 1))
            {
                if (distanceSquared > 16 * (BoundingRadius + 15)*(BoundingRadius+15)) //beyond killing radius, make a new point
                {
                    NewPoint();
                    return true;
                }
                if (Steps++ > 10000) //timeout to prevent too long looping on 1 point
                {
                    NewPoint();
                    return true;
                }

                InsideJump();
                distanceSquared = Vector2.DistanceSquared(CurrentPoint, Vector2.Zero);
            };
            //within bounding region of cluster
            return InsideJump();//if return false then add point to cluster(as touching a point)
        }
        public void AddPoint() //add the point to the cluster
        {
            //increment values
            NumPoints++;
            LastAdded = CurrentPoint;
            float distance = Vector2.Distance(LastAdded, Vector2.Zero);
            BoundingRadius = Math.Max(BoundingRadius, distance + 1); //+1 for radius of ball

            //add regions for the point:
            Vector2 scaleRegion = Vector2.Divide(CurrentPoint, 1 << 2).Floor();
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
                    DLAoutput.WriteLine("{0}:{1}", LastAdded.X, LastAdded.Y); //removed writing bounding radius to file for memory
                }
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
    }
}
