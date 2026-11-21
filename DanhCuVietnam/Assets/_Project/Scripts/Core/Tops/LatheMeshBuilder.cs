using System;
using System.Collections.Generic;
using System.Numerics;

namespace DanhCu.Core.Tops
{
    /// <summary>A point of a 2D profile: radius from the Y axis and height.</summary>
    public readonly struct ProfilePoint
    {
        public ProfilePoint(double radius, double height)
        {
            Radius = radius;
            Height = height;
        }

        public double Radius { get; }

        public double Height { get; }
    }

    /// <summary>
    /// A run of profile points lathed into one submesh. Consecutive strips share their boundary
    /// point; <see cref="SmoothWithPrevious"/> makes the shading continuous across it, otherwise
    /// the boundary is a hard edge (crease).
    /// </summary>
    public sealed class ProfileStrip
    {
        public ProfileStrip(int submesh, bool smoothWithPrevious)
        {
            Submesh = submesh;
            SmoothWithPrevious = smoothWithPrevious;
        }

        public int Submesh { get; }

        public bool SmoothWithPrevious { get; }

        public List<ProfilePoint> Points { get; } = new List<ProfilePoint>();
    }

    /// <summary>Revolves profile strips around +Y into a mesh with smooth normals, cylindrical UVs and clean poles.</summary>
    public static class LatheMeshBuilder
    {
        private const double PoleEpsilon = 1e-9;

        public static MeshData Build(IReadOnlyList<ProfileStrip> strips, int radialSegments, int submeshCount)
        {
            if (radialSegments < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(radialSegments));
            }

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++)
            {
                triangles[i] = new List<int>();
            }

            double totalLength = 0;
            foreach (ProfileStrip strip in strips)
            {
                for (int k = 1; k < strip.Points.Count; k++)
                {
                    totalLength += Distance(strip.Points[k - 1], strip.Points[k]);
                }
            }

            totalLength = Math.Max(totalLength, PoleEpsilon);
            double arc = 0;
            int columns = radialSegments + 1;

            for (int s = 0; s < strips.Count; s++)
            {
                ProfileStrip strip = strips[s];
                List<ProfilePoint> pts = strip.Points;
                if (pts.Count < 2)
                {
                    continue;
                }

                if (strip.Submesh < 0 || strip.Submesh >= submeshCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(strips), $"Strip {s} uses submesh {strip.Submesh}.");
                }

                int baseVertex = positions.Count;
                for (int k = 0; k < pts.Count; k++)
                {
                    if (k > 0)
                    {
                        arc += Distance(pts[k - 1], pts[k]);
                    }

                    Vector2 n2 = ProfileNormal(strips, s, k);
                    float v = (float)(arc / totalLength);
                    for (int j = 0; j < columns; j++)
                    {
                        double theta = 2.0 * Math.PI * j / radialSegments;
                        float cos = (float)Math.Cos(theta);
                        float sin = (float)Math.Sin(theta);
                        float r = (float)pts[k].Radius;
                        positions.Add(new Vector3(r * cos, (float)pts[k].Height, r * sin));
                        normals.Add(new Vector3(n2.X * cos, n2.Y, n2.X * sin));
                        uvs.Add(new Vector2((float)j / radialSegments, v));
                    }
                }

                List<int> tris = triangles[strip.Submesh];
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    bool lowerPole = pts[k].Radius < PoleEpsilon;
                    bool upperPole = pts[k + 1].Radius < PoleEpsilon;
                    if (Distance(pts[k], pts[k + 1]) < PoleEpsilon || (lowerPole && upperPole))
                    {
                        continue;
                    }

                    for (int j = 0; j < radialSegments; j++)
                    {
                        int a = baseVertex + (k * columns) + j; // lower ring, column j
                        int b = a + 1;                         // lower ring, column j+1
                        int c = a + columns;                   // upper ring, column j
                        int d = c + 1;                         // upper ring, column j+1

                        // Clockwise when seen from outside (Unity front faces).
                        if (!upperPole)
                        {
                            tris.Add(a);
                            tris.Add(c);
                            tris.Add(d);
                        }

                        if (!lowerPole)
                        {
                            tris.Add(a);
                            tris.Add(d);
                            tris.Add(b);
                        }
                    }
                }
            }

            var submeshes = new int[submeshCount][];
            for (int i = 0; i < submeshCount; i++)
            {
                submeshes[i] = triangles[i].ToArray();
            }

            return new MeshData(positions.ToArray(), normals.ToArray(), uvs.ToArray(), submeshes);
        }

        /// <summary>Outward 2D normal (radial, vertical) at point k of strip s.</summary>
        private static Vector2 ProfileNormal(IReadOnlyList<ProfileStrip> strips, int s, int k)
        {
            List<ProfilePoint> pts = strips[s].Points;
            ProfilePoint current = pts[k];
            ProfilePoint prev = current;
            ProfilePoint next = current;

            if (k > 0)
            {
                prev = pts[k - 1];
            }
            else if (strips[s].SmoothWithPrevious && s > 0 && strips[s - 1].Points.Count >= 2)
            {
                List<ProfilePoint> before = strips[s - 1].Points;
                prev = before[before.Count - 2];
            }

            if (k < pts.Count - 1)
            {
                next = pts[k + 1];
            }
            else if (s < strips.Count - 1 && strips[s + 1].SmoothWithPrevious && strips[s + 1].Points.Count >= 2)
            {
                next = strips[s + 1].Points[1];
            }

            double tr = next.Radius - prev.Radius;
            double ty = next.Height - prev.Height;
            double length = Math.Sqrt((tr * tr) + (ty * ty));
            if (length < PoleEpsilon)
            {
                return new Vector2(1, 0);
            }

            // Rotate the tangent (dr, dy) by -90°: going up a convex outline gives an outward normal.
            return new Vector2((float)(ty / length), (float)(-tr / length));
        }

        private static double Distance(ProfilePoint a, ProfilePoint b)
        {
            double dr = b.Radius - a.Radius;
            double dy = b.Height - a.Height;
            return Math.Sqrt((dr * dr) + (dy * dy));
        }
    }
}
