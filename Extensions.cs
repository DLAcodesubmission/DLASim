using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class Extensions //adding convinient functions for code
    {
        public static Vector2 Floor(this Vector2 v)
        {
            return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
        }
        public static Vector2 Sign(this Vector2 v)
        {
            return new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
        }
        public static Complex ToComplex(this Vector2 v)
        {
            return new Complex(v.X, v.Y);
        }
        public static Vector2 ToVector2(this Complex complex)
        {
            return new Vector2((float)complex.Real,(float)complex.Imaginary);
        }
        public static Vector2 Round(this Vector2 v)
        {
            return new(MathF.Round(v.X),MathF.Round(v.Y));
        }
      
        public static HashSet<Vector2> Offset(this HashSet<Vector2> set, Vector2 v)
        {
            HashSet<Vector2> offset = new HashSet<Vector2>();
            foreach(Vector2 vector in set) offset.Add(vector+v);
            return offset;
        }

    }
}
