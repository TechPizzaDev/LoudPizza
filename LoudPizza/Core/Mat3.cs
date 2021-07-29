
namespace LoudPizza
{
    public struct Mat3
    {
        public Vec3 M0;
        public Vec3 M1;
        public Vec3 M2;

        public Vec3 mul(Vec3 a)
        {
            Vec3 r;

            r.X = M0.X * a.X + M0.Y * a.Y + M0.Z * a.Z;
            r.Y = M1.X * a.X + M1.Y * a.Y + M1.Z * a.Z;
            r.Z = M2.X * a.X + M2.Y * a.Y + M2.Z * a.Z;

            return r;
        }

        public void lookatRH(Vec3 at, Vec3 up)
        {
            Vec3 z = at;
            z.normalize();
            Vec3 x = up.cross(z);
            x.normalize();
            Vec3 y = z.cross(x);
            M0 = x;
            M1 = y;
            M2 = z;
        }

        public void lookatLH(Vec3 at, Vec3 up)
        {
            Vec3 z = at;
            z.normalize();
            Vec3 x = up.cross(z);
            x.normalize();
            Vec3 y = z.cross(x);
            x.neg();  // flip x
            M0 = x;
            M1 = y;
            M2 = z;
        }
    }
}
