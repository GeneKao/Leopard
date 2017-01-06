using System;
using Rhino.Geometry;

namespace Utilities
{
    static public class Utils
    {
        static private Random random = new Random();

        // ===============================================================================================================================================

        static public double Distance(Point3d A, Point3d B)
        {
            return Math.Sqrt(Math.Pow(A.X - B.X, 2.0) + Math.Pow(A.Y - B.Y, 2.0) + Math.Pow(A.Z - B.Z, 2.0));
        }


        static public double DistanceXY(Point3d A, Point3d B)
        {
            return Math.Sqrt(Math.Pow(A.X - B.X, 2.0) + Math.Pow(A.Y - B.Y, 2.0));
        }

        static public double DistanceSquared(Point3d A, Point3d B)
        {
            return Math.Pow(A.X - B.X, 2.0) + Math.Pow(A.Y - B.Y, 2.0) + Math.Pow(A.Z - B.Z, 2.0);
        }

        static public double DistanceSquaredXY(Point3d A, Point3d B)
        {
            return Math.Pow(A.X - B.X, 2.0) + Math.Pow(A.Y - B.Y, 2.0);
        }

        static public double DistancePointLine(Point3d M, Point3d A, Point3d B)
        {
            return Vector3d.CrossProduct(M - A, M - B).Length / (A - B).Length;
        }

        static public Point3d ClosestPointOnLine(Point3d M, Point3d A, Point3d B)
        {
            double distance = DistancePointLine(M, A, B);

            Vector3d AB = B - A;
            if ((M - A) * AB < 0) AB *= -1;

            return A + AB / AB.Length * Math.Sqrt(DistanceSquared(M, A) - distance * distance);
        }

        static public Point3d ClosestPointOnLine(Point3d M, Point3d A, Point3d B, ref double distance)
        {
            distance = DistancePointLine(M, A, B);
            Vector3d AB = B - A;
            if ((M - A) * AB < 0) AB *= -1;

            return A + AB / AB.Length * Math.Sqrt(DistanceSquared(M, A) - distance * distance);
        }


        static public void ResetRandom(int seed) { random = seed < 0 ? new Random() : new Random(seed); }
        static public int GetRandomInteger() { return random.Next(); }
        static public int GetRandomInteger(int minValue, int maxValue) { return random.Next(minValue, maxValue); }
        static public int GetRandomInteger(int maxValue) { return random.Next(maxValue); }
        static public double GetRandomDouble() { return random.NextDouble(); }
        static public double GetRandomDouble(double minValue, double maxValue) { return minValue + random.NextDouble() * (maxValue - minValue); }
        static public double GetRandomDouble(double maxValue) { return random.NextDouble() * maxValue; }


        static public Point3d GetRandomPoint(double minX = 0.0, double maxX = 1.0, double minY = 0.0, double maxY = 1.0, double minZ = 0.0, double maxZ = 1.0)
        {
            double x = minX + (maxX - minX) * random.NextDouble();
            double y = minY + (maxY - minY) * random.NextDouble();
            double z = minZ + (maxZ - minZ) * random.NextDouble();

            return new Point3d(x, y, z);
        }


        static public Vector3d GetRandomUnitVector()
        {
            double phi = 2.0 * Math.PI * random.NextDouble();
            double theta = Math.Acos(2.0 * random.NextDouble() - 1.0);

            double x = Math.Sin(theta) * Math.Cos(phi);
            double y = Math.Sin(theta) * Math.Sin(phi);
            double z = Math.Cos(theta);

            return new Vector3d(x, y, z);
        }


        static public Vector3d GetRandomUnitVectorXY()
        {
            double angle = 2.0 * Math.PI * random.NextDouble();

            double x = Math.Cos(angle);
            double y = Math.Sin(angle);

            return new Vector3d(x, y, 0.0);
        }


        static public Vector3d GetRandomVector(double minLength = 0.0, double maxLength = 1.0)
        {
            return GetRandomUnitVector() * (minLength + random.NextDouble() * (maxLength - minLength));
        }


        static public Vector3d GetRandomVectorXY(double minLength = 0.0, double maxLength = 1.0)
        {
            return GetRandomUnitVectorXY() * (minLength + random.NextDouble() * (maxLength - minLength));
        }


        static public Vector3d ComputePlaneNormal(Point3d A, Point3d B, Point3d C)
        {
            Vector3d normal = Vector3d.CrossProduct(B - A, C - A);
            normal.Unitize();
            return normal;
        }


        private const double degreeToRadianFactor = Math.PI / 180.0;
        private const double radianToDegreeFactor = 180.0 / Math.PI;

        static public double ToRadian(double degree)
        {
            return degree * degreeToRadianFactor;
        }


        static public double ToDegree(double radian)
        {
            return radian * radianToDegreeFactor;
        }


        static public double AngleBetweenTwoTriangles(Point3d A, Point3d B, Point3d M, Point3d N)
        {
            return AngleBetweenTwoVectors(
                M - ClosestPointOnLine(M, A, B),
                N - ClosestPointOnLine(N, A, B)
                );
        }


        /// <summary>Compute the angle between two unit vectors. The result will always lie in the interval between 0 and PI</summary>
        /// <param name="a">The first vector</param>
        /// <param name="b">The second vector</param>
        /// <returns>The angle between two vectors</returns>
        static public double AngleBetweenTwoUnitVectors(Vector3d a, Vector3d b)
        {
            return Math.Acos(a.X * b.X + a.Y * b.Y + a.Z * b.Z);
        }


        /// <summary>Compute the angle between two unit vectors, which can be smaller than 0 or larger than PI, based on a reference Z vector that indicates the positive rotation direction</summary>
        /// <param name="a">The first vector</param>
        /// <param name="b">The second vector</param>
        /// <param name="z">The vector that indicates the positive rotation direction</param>
        /// <param name="positive">
        ///     <para>True: the result angle will be in the interval between 0 and 2 * PI</para>
        ///     <para>False: the result angle will be in the interval between -PI and PI</para>
        /// </param>
        /// <returns>The angle between two vectors</returns>
        static public double AngleBetweenTwoUnitVectors(Vector3d a, Vector3d b, Vector3d z, bool positive = true)
        {
            double angle = Math.Acos(a.X * b.X + a.Y * b.Y + a.Z * b.Z);

            if (z * Vector3d.CrossProduct(a, b) < 0.0)
            {
                if (positive) angle = 2.0 * Math.PI - angle;
                else angle = -angle;
            }
            return angle;
        }


        /// <summary>Compute the angle between two vectors. The result will always lie in the interval between 0 and PI</summary>
        /// <param name="a">The first vector</param>
        /// <param name="b">The second vector</param>
        /// <returns>The angle between two vectors</returns>
        static public double AngleBetweenTwoVectors(Vector3d a, Vector3d b)
        {
            double temp = (a.X * b.X + a.Y * b.Y + a.Z * b.Z) / (a.Length * b.Length);
            if (temp > 1.0) temp = 1.0;
            else if (temp < -1.0) temp = -1.0;
            return Math.Acos(temp);
        }


        /// <summary>Compute the angle between two vectors, which can be smaller than 0 or larger than PI, based on a reference Z vector that indicates the positive rotation direction</summary>
        /// <param name="a">The first vector</param>
        /// <param name="b">The second vector</param>
        /// <param name="z">The vector that indicates the positive rotation direction</param>
        /// <param name="positive">
        ///     <para>True: the result angle will be in the interval between 0 and 2 * PI</para>
        ///     <para>False: the result angle will be in the interval between -PI and PI</para>
        /// </param>
        /// <returns>The angle between two vectors</returns>
        static public double AngleBetweenTwoVectors(Vector3d a, Vector3d b, Vector3d z, bool positive = true)
        {
            double temp = (a.X * b.X + a.Y * b.Y + a.Z * b.Z) / (a.Length * b.Length);
            if (temp > 1.0) temp = 1.0;
            else if (temp < -1.0) temp = -1.0;

            double angle = Math.Acos(temp);

            if (z * Vector3d.CrossProduct(a, b) < 0.0)
            {
                if (positive) angle = 2.0 * Math.PI - angle;
                else angle = -angle;
            }

            return angle;
        }



        //////////////////////// my mesh function /////////////////////////////////
        public static void CleanMesh(Mesh m)
        {
            m.Vertices.CombineIdentical(true, true);
            m.Vertices.CullUnused();
            m.Weld(3.1415926535897931);
            m.UnifyNormals();
            m.FaceNormals.ComputeFaceNormals();
            m.Normals.ComputeNormals();
        }

    }
}