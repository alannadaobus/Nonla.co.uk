using System;
using System.Collections.Generic;

namespace DanhCu.Core.Tops
{
    /// <summary>Builds the render and collision meshes of a top from its <see cref="TopProfile"/>.</summary>
    public static class TopMeshFactory
    {
        /// <summary>Material slot of the body mesh for plain wood.</summary>
        public const int WoodSubmesh = 0;

        /// <summary>Material slot of the body mesh for the rope winding area.</summary>
        public const int RopeZoneSubmesh = 1;

        /// <summary>Hidden shank length drawn inside the wood so no gap can show at the joint (m).</summary>
        private const double HiddenShankOverlap = 0.0005;

        /// <summary>Profile samples across one rope groove.</summary>
        private const int GrooveSamples = 5;

        /// <summary>Wooden body: underside ring, rope cone (with rope zone submesh), crown. Two submeshes.</summary>
        public static MeshData BuildBody(TopProfile profile)
        {
            TopShapeParameters p = profile.Parameters;
            var strips = new List<ProfileStrip>();

            var underside = new ProfileStrip(WoodSubmesh, false);
            underside.Points.Add(new ProfilePoint(p.ShankRadius, p.WoodBottomHeight));
            underside.Points.Add(new ProfilePoint(p.CollarRadius, p.WoodBottomHeight));
            strips.Add(underside);

            AddLowerStrip(strips, profile, WoodSubmesh, false, UniformSamples(0.0, p.RopeZoneStart, Samples(p, p.RopeZoneStart)));
            AddLowerStrip(strips, profile, RopeZoneSubmesh, true, RopeZoneSamples(profile));
            AddLowerStrip(strips, profile, WoodSubmesh, true, UniformSamples(p.RopeZoneEnd, 1.0, Samples(p, 1.0 - p.RopeZoneEnd)));

            var crown = new ProfileStrip(WoodSubmesh, true);
            for (int i = 0; i <= p.ProfileSegments; i++)
            {
                profile.CrownPoint(Math.PI * 0.5 * i / p.ProfileSegments, out double r, out double y);
                crown.Points.Add(new ProfilePoint(r, y));
            }

            strips.Add(crown);
            return LatheMeshBuilder.Build(Clean(strips), p.RadialSegments, 2);
        }

        /// <summary>Metal tip: rounded point, taper and shank (into the wood). One submesh.</summary>
        public static MeshData BuildTip(TopProfile profile)
        {
            TopShapeParameters p = profile.Parameters;
            var strips = new List<ProfileStrip>();

            var point = new ProfileStrip(0, false);
            double arcEnd = (Math.PI * 0.5) - profile.ConeHalfAngle;
            int arcSamples = Math.Max(4, p.ProfileSegments / 2);
            for (int i = 0; i <= arcSamples; i++)
            {
                double beta = arcEnd * i / arcSamples;
                point.Points.Add(new ProfilePoint(p.TipPointRadius * Math.Sin(beta), p.TipPointRadius * (1 - Math.Cos(beta))));
            }

            point.Points.Add(new ProfilePoint(p.ShankRadius, profile.ConeEndHeight));
            strips.Add(point);

            var shank = new ProfileStrip(0, false);
            shank.Points.Add(new ProfilePoint(p.ShankRadius, profile.ConeEndHeight));
            shank.Points.Add(new ProfilePoint(p.ShankRadius, p.WoodBottomHeight + Math.Min(p.TipEmbedDepth, HiddenShankOverlap)));
            strips.Add(shank);

            return LatheMeshBuilder.Build(Clean(strips), Math.Max(12, p.RadialSegments / 2), 1);
        }

        /// <summary>
        /// Low-poly outline for a convex MeshCollider. It starts at the tangent ring of the
        /// rounded point so it never reaches below the tip sphere collider: ground contact is
        /// always the sphere, exactly on the axis.
        /// </summary>
        public static MeshData BuildCollisionHull(TopProfile profile)
        {
            TopShapeParameters p = profile.Parameters;
            var hull = new ProfileStrip(0, false);
            hull.Points.Add(new ProfilePoint(profile.TipTangentRadius, profile.TipTangentHeight));
            hull.Points.Add(new ProfilePoint(p.ShankRadius, profile.ConeEndHeight));
            hull.Points.Add(new ProfilePoint(p.ShankRadius, p.WoodBottomHeight));
            hull.Points.Add(new ProfilePoint(p.CollarRadius, p.WoodBottomHeight));
            for (int i = 1; i <= 4; i++)
            {
                hull.Points.Add(new ProfilePoint(profile.LowerConeRadius(i / 4.0, false), p.WoodBottomHeight + ((p.BellyHeight - p.WoodBottomHeight) * i / 4.0)));
            }

            for (int i = 1; i <= 4; i++)
            {
                profile.CrownPoint(Math.PI * 0.5 * i / 4.0, out double r, out double y);
                hull.Points.Add(new ProfilePoint(r, y));
            }

            return LatheMeshBuilder.Build(new[] { hull }, p.CollisionRadialSegments, 1);
        }

        private static int Samples(TopShapeParameters p, double fraction)
        {
            return Math.Max(1, (int)Math.Ceiling(p.ProfileSegments * fraction));
        }

        private static List<double> UniformSamples(double t0, double t1, int samples)
        {
            var ts = new List<double>(samples + 1);
            for (int i = 0; i <= samples; i++)
            {
                ts.Add(t0 + ((t1 - t0) * i / samples));
            }

            return ts;
        }

        /// <summary>
        /// Coarse samples along the rope zone plus a dense cluster across each groove only,
        /// so grooves stay round without paying for dense rings over the whole cone.
        /// </summary>
        private static List<double> RopeZoneSamples(TopProfile profile)
        {
            TopShapeParameters p = profile.Parameters;
            List<double> ts = UniformSamples(p.RopeZoneStart, p.RopeZoneEnd, Samples(p, p.RopeZoneEnd - p.RopeZoneStart));
            double lowerLength = p.BellyHeight - p.WoodBottomHeight;
            if (p.RopeGrooveCount > 0 && p.RopeGrooveDepth > 0)
            {
                double zone = p.RopeZoneEnd - p.RopeZoneStart;
                double halfWidth = p.RopeGrooveWidth * 0.5 / lowerLength;
                for (int k = 0; k < p.RopeGrooveCount; k++)
                {
                    double centre = p.RopeZoneStart + (zone * (k + 0.5) / p.RopeGrooveCount);
                    for (int i = 0; i <= GrooveSamples; i++)
                    {
                        ts.Add(centre - halfWidth + (2 * halfWidth * i / GrooveSamples));
                    }
                }
            }

            ts.Sort();
            var result = new List<double>(ts.Count);
            foreach (double t in ts)
            {
                if (t >= p.RopeZoneStart && t <= p.RopeZoneEnd && (result.Count == 0 || t - result[result.Count - 1] > 1e-6))
                {
                    result.Add(t);
                }
            }

            if (result[result.Count - 1] < p.RopeZoneEnd)
            {
                result.Add(p.RopeZoneEnd);
            }

            return result;
        }

        private static void AddLowerStrip(List<ProfileStrip> strips, TopProfile profile, int submesh, bool smooth, List<double> ts)
        {
            TopShapeParameters p = profile.Parameters;
            var strip = new ProfileStrip(submesh, smooth);
            foreach (double t in ts)
            {
                double y = p.WoodBottomHeight + (t * (p.BellyHeight - p.WoodBottomHeight));
                strip.Points.Add(new ProfilePoint(profile.LowerConeRadius(t, true), y));
            }

            strips.Add(strip);
        }

        /// <summary>
        /// Drops zero-length strips (e.g. a rope zone starting exactly at the collar). A dropped
        /// strip's hard edge is carried to the next strip so creases are never smoothed away.
        /// </summary>
        private static List<ProfileStrip> Clean(List<ProfileStrip> strips)
        {
            var result = new List<ProfileStrip>();
            bool carrySmooth = true;
            foreach (ProfileStrip strip in strips)
            {
                ProfilePoint first = strip.Points[0];
                ProfilePoint last = strip.Points[strip.Points.Count - 1];
                bool degenerate = Math.Abs(first.Radius - last.Radius) < 1e-12 && Math.Abs(first.Height - last.Height) < 1e-12;
                if (degenerate)
                {
                    carrySmooth &= strip.SmoothWithPrevious;
                    continue;
                }

                bool smooth = result.Count > 0 && strip.SmoothWithPrevious && carrySmooth;
                carrySmooth = true;
                if (smooth == strip.SmoothWithPrevious)
                {
                    result.Add(strip);
                    continue;
                }

                var copy = new ProfileStrip(strip.Submesh, smooth);
                copy.Points.AddRange(strip.Points);
                result.Add(copy);
            }

            return result;
        }
    }
}
