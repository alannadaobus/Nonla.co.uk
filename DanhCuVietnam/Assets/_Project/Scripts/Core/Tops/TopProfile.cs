using System;
using System.Collections.Generic;

namespace DanhCu.Core.Tops
{
    /// <summary>
    /// Analytic outline (radius as a function of height) of the metal tip and wooden body.
    /// All mesh, collider and mass computations sample this one description.
    ///
    /// Metal tip, from the pivot up: spherical point of radius ρ, tangent cone of half-angle α,
    /// cylindrical shank up to the wood and <see cref="TopShapeParameters.TipEmbedDepth"/> into it.
    /// Wood: flat underside ring, lower "rope" cone r = r_c + (R − r_c)(1 − (1 − t)^p) with
    /// optional grooves, then a superellipse crown r = R·cos(φ)^(2/n), y = y_b + (H − y_b)·sin(φ)^(2/n).
    /// </summary>
    public sealed class TopProfile
    {
        private const double HalfPi = Math.PI * 0.5;

        public TopProfile(TopShapeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            List<string> errors = parameters.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException("Invalid top shape: " + string.Join(" ", errors), nameof(parameters));
            }

            Parameters = parameters.Clone();
            ConeHalfAngle = Math.Atan(Parameters.ShankRadius / Parameters.TipConeLength);
            double rho = Parameters.TipPointRadius;
            double sin = Math.Sin(ConeHalfAngle);
            ConeApexHeight = rho - (rho / sin);
            TipTangentHeight = rho * (1.0 - sin);
            TipTangentRadius = rho * Math.Cos(ConeHalfAngle);
            ConeEndHeight = ConeApexHeight + Parameters.TipConeLength;

            double lowerLength = Parameters.BellyHeight - Parameters.WoodBottomHeight;
            RopeZoneStartHeight = Parameters.WoodBottomHeight + (Parameters.RopeZoneStart * lowerLength);
            RopeZoneEndHeight = Parameters.WoodBottomHeight + (Parameters.RopeZoneEnd * lowerLength);
        }

        public TopShapeParameters Parameters { get; }

        /// <summary>Half-angle α of the tapered tip, radians.</summary>
        public double ConeHalfAngle { get; }

        /// <summary>Height of the virtual (unrounded) cone apex; slightly below the pivot.</summary>
        public double ConeApexHeight { get; }

        /// <summary>Where the spherical point meets the cone.</summary>
        public double TipTangentHeight { get; }

        public double TipTangentRadius { get; }

        /// <summary>Where the taper reaches the shank radius.</summary>
        public double ConeEndHeight { get; }

        public double RopeZoneStartHeight { get; }

        public double RopeZoneEndHeight { get; }

        public double MetalTopHeight => Parameters.WoodBottomHeight + Parameters.TipEmbedDepth;

        /// <summary>Heights where the outline is not smooth; integration is split there.</summary>
        public double[] Breakpoints => new[]
        {
            0.0, TipTangentHeight, ConeEndHeight, Parameters.WoodBottomHeight, MetalTopHeight,
            RopeZoneStartHeight, RopeZoneEndHeight, Parameters.BellyHeight, Parameters.TotalHeight,
        };

        /// <summary>Radius of solid metal at height y (0 where there is none).</summary>
        public double MetalRadiusAt(double y)
        {
            double rho = Parameters.TipPointRadius;
            if (y < 0 || y > MetalTopHeight)
            {
                return 0;
            }

            if (y <= TipTangentHeight)
            {
                double dy = rho - y;
                return Math.Sqrt(Math.Max(0, (rho * rho) - (dy * dy)));
            }

            if (y <= ConeEndHeight)
            {
                return (y - ConeApexHeight) * Math.Tan(ConeHalfAngle);
            }

            return Parameters.ShankRadius;
        }

        /// <summary>Outer radius of the wood at height y including rope grooves (0 below the underside).</summary>
        public double WoodRadiusAt(double y) => WoodRadius(y, true);

        /// <summary>Outer radius ignoring grooves (used for the collision hull).</summary>
        public double WoodEnvelopeRadiusAt(double y) => WoodRadius(y, false);

        /// <summary>Wood radius on the lower cone at normalised position t (0 = collar, 1 = belly).</summary>
        public double LowerConeRadius(double t, bool withGrooves)
        {
            TopShapeParameters p = Parameters;
            double r = p.CollarRadius + ((p.BodyRadius - p.CollarRadius) * (1.0 - Math.Pow(1.0 - t, p.LowerCurveExponent)));
            if (withGrooves)
            {
                double y = p.WoodBottomHeight + (t * (p.BellyHeight - p.WoodBottomHeight));
                r -= GrooveDepthAt(y);
            }

            return r;
        }

        /// <summary>Crown point at parameter φ ∈ [0, π/2] (0 = belly, π/2 = apex).</summary>
        public void CrownPoint(double phi, out double r, out double y)
        {
            TopShapeParameters p = Parameters;
            double e = 2.0 / p.CrownFlatness;
            double c = Math.Max(0, Math.Cos(phi));
            double s = Math.Max(0, Math.Sin(phi));
            r = p.BodyRadius * Math.Pow(c, e);
            y = p.BellyHeight + ((p.TotalHeight - p.BellyHeight) * Math.Pow(s, e));
            if (phi >= HalfPi)
            {
                r = 0;
                y = p.TotalHeight;
            }
        }

        /// <summary>Groove depth at height y (0 outside grooves). Grooves are smooth cos² dips.</summary>
        public double GrooveDepthAt(double y)
        {
            TopShapeParameters p = Parameters;
            if (p.RopeGrooveCount == 0 || p.RopeGrooveDepth <= 0 || y < RopeZoneStartHeight || y > RopeZoneEndHeight)
            {
                return 0;
            }

            double spacing = (RopeZoneEndHeight - RopeZoneStartHeight) / p.RopeGrooveCount;
            double half = p.RopeGrooveWidth * 0.5;
            for (int k = 0; k < p.RopeGrooveCount; k++)
            {
                double centre = RopeZoneStartHeight + ((k + 0.5) * spacing);
                double d = y - centre;
                if (Math.Abs(d) < half)
                {
                    double c = Math.Cos(Math.PI * d / p.RopeGrooveWidth);
                    return p.RopeGrooveDepth * c * c;
                }
            }

            return 0;
        }

        private double WoodRadius(double y, bool withGrooves)
        {
            TopShapeParameters p = Parameters;
            if (y < p.WoodBottomHeight || y > p.TotalHeight)
            {
                return 0;
            }

            if (y <= p.BellyHeight)
            {
                double t = (y - p.WoodBottomHeight) / (p.BellyHeight - p.WoodBottomHeight);
                return LowerConeRadius(t, withGrooves);
            }

            // Invert the crown superellipse: sin φ = s^(n/2), r = R · cos φ^(2/n).
            double s = (y - p.BellyHeight) / (p.TotalHeight - p.BellyHeight);
            double sinPhi = Math.Pow(Math.Min(1, s), p.CrownFlatness * 0.5);
            double cosPhi = Math.Sqrt(Math.Max(0, 1 - (sinPhi * sinPhi)));
            return p.BodyRadius * Math.Pow(cosPhi, 2.0 / p.CrownFlatness);
        }
    }
}
