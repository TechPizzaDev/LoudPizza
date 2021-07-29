using System;

namespace LoudPizza
{
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool isZero()
        {
            if (X == 0 && Y == 0 && Z == 0)
                return true;
            return false;
        }

        public void neg()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        public float mag()
        {
            return MathF.Sqrt(X * X + Y * Y + Z * Z);
        }

        public void normalize()
        {
            float m = mag();
            if (m == 0)
            {
                X = Y = Z = 0;
                return;
            }
            X /= m;
            Y /= m;
            Z /= m;
        }

        public float dot(Vec3 a)
        {
            return X * a.X + Y * a.Y + Z * a.Z;
        }

        public Vec3 sub(Vec3 a)
        {
            Vec3 r;
            r.X = X - a.X;
            r.Y = Y - a.Y;
            r.Z = Z - a.Z;
            return r;
        }

        public Vec3 cross(Vec3 a)
        {
            Vec3 r;

            r.X = Y * a.Z - a.Y * Z;
            r.Y = Z * a.X - a.Z * X;
            r.Z = X * a.Y - a.X * Y;

            return r;
        }

        public override string ToString()
        {
            return $"{X}  {Y}  {Z}";
        }
    }
}
