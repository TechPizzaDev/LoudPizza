
using System.Numerics;

namespace LoudPizza
{
    public struct Mat3
    {
        public Vector3 M0;
        public Vector3 M1;
        public Vector3 M2;

        public Vector3 mul(Vector3 a)
        {
            Vector3 r;

            r.X = M0.X * a.X + M0.Y * a.Y + M0.Z * a.Z;
            r.Y = M1.X * a.X + M1.Y * a.Y + M1.Z * a.Z;
            r.Z = M2.X * a.X + M2.Y * a.Y + M2.Z * a.Z;

            return r;
        }

        public void lookatRH(Vector3 at, Vector3 up)
        {
            Vector3 z = Vector3.Normalize(at);
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
            Vector3 y = Vector3.Cross(z, x);
            M0 = x;
            M1 = y;
            M2 = z;
        }

        public void lookatLH(Vector3 at, Vector3 up)
        {
            Vector3 z = Vector3.Normalize(at);
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
            Vector3 y = Vector3.Cross(z, x);
            x = Vector3.Negate(x); // flip x
            M0 = x;
            M1 = y;
            M2 = z;
        }
    }
}
