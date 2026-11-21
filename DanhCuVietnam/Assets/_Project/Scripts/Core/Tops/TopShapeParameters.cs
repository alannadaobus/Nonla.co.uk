using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DanhCu.Core.Tops
{
    /// <summary>
    /// Every adjustable dimension of a spinning-top model, in SI units (metres).
    /// Engine-free so the same numbers drive the Unity mesh, the colliders, the mass
    /// properties, unit tests and the offline exporter.
    ///
    /// The default values are the "Medium" prototype (see <see cref="TopShapePresets"/>).
    /// They are design assumptions, not measurements of historical tops: see Docs/TOP_MODEL.md §2.
    ///
    /// Coordinate frame: +Y is the spin axis, origin at the lowest point of the metal tip
    /// (the physical pivot / ground contact). The model is rotationally symmetric about +Y.
    /// </summary>
    public sealed class TopShapeParameters
    {
        /// <summary>Bump when the mesh/profile algorithm changes so baked meshes are regenerated.</summary>
        public const int GeneratorVersion = 1;

        // ---- Overall ----

        /// <summary>From the tip point to the top of the crown.</summary>
        public double TotalHeight { get; set; } = 0.075;

        /// <summary>Widest diameter of the wooden body (the belly).</summary>
        public double BodyDiameter { get; set; } = 0.055;

        /// <summary>Height of the belly above the wood underside, as a fraction of the wood height.</summary>
        public double BellyHeightRatio { get; set; } = 0.50;

        /// <summary>Diameter of the wood where the nail enters it (bottom of the rope cone).</summary>
        public double CollarDiameter { get; set; } = 0.010;

        /// <summary>Shape of the lower (rope) cone: 1 = straight cone, larger = fuller, rounder flank.</summary>
        public double LowerCurveExponent { get; set; } = 1.6;

        /// <summary>Superellipse exponent of the crown: 2 = elliptical dome, larger = flatter, squarer shoulder.</summary>
        public double CrownFlatness { get; set; } = 2.6;

        // ---- Metal tip (chân đinh) ----

        /// <summary>Length of metal visible below the wood.</summary>
        public double TipExposedLength { get; set; } = 0.012;

        /// <summary>Diameter of the metal shank.</summary>
        public double TipShankDiameter { get; set; } = 0.005;

        /// <summary>Length of the tapered (conical) part of the tip, measured from the virtual cone apex.</summary>
        public double TipConeLength { get; set; } = 0.008;

        /// <summary>Radius of the rounded point (the contact patch).</summary>
        public double TipPointRadius { get; set; } = 0.0008;

        /// <summary>How deep the shank continues inside the wood (affects mass and centre of mass, not visible).</summary>
        public double TipEmbedDepth { get; set; } = 0.015;

        // ---- Rope winding area ----

        /// <summary>Start of the rope zone along the lower cone, 0 = collar, 1 = belly.</summary>
        public double RopeZoneStart { get; set; } = 0.10;

        /// <summary>End of the rope zone along the lower cone, 0 = collar, 1 = belly.</summary>
        public double RopeZoneEnd { get; set; } = 0.90;

        /// <summary>Shallow turned grooves that guide the rope. 0 = smooth cone.</summary>
        public int RopeGrooveCount { get; set; } = 4;

        public double RopeGrooveDepth { get; set; } = 0.0004;

        public double RopeGrooveWidth { get; set; } = 0.0016;

        // ---- Mesh detail (does not change physics) ----

        public int RadialSegments { get; set; } = 40;

        /// <summary>Samples per curved profile section.</summary>
        public int ProfileSegments { get; set; } = 16;

        /// <summary>Radial segments of the convex collision hull (keep the hull under 255 faces).</summary>
        public int CollisionRadialSegments { get; set; } = 12;

        // ---- Derived ----

        public double BodyRadius => BodyDiameter * 0.5;

        public double CollarRadius => CollarDiameter * 0.5;

        public double ShankRadius => TipShankDiameter * 0.5;

        /// <summary>Height of the wood underside above the pivot.</summary>
        public double WoodBottomHeight => TipExposedLength;

        public double WoodHeight => TotalHeight - TipExposedLength;

        public double BellyHeight => WoodBottomHeight + (BellyHeightRatio * WoodHeight);

        public TopShapeParameters Clone() => (TopShapeParameters)MemberwiseClone();

        /// <summary>Human-readable problems; empty when the shape can be built.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            void Check(bool ok, string message)
            {
                if (!ok)
                {
                    errors.Add(message);
                }
            }

            Check(TotalHeight >= 0.02 && TotalHeight <= 0.2, "TotalHeight must be 20..200 mm.");
            Check(BodyDiameter >= 0.015 && BodyDiameter <= 0.15, "BodyDiameter must be 15..150 mm.");
            Check(BellyHeightRatio >= 0.2 && BellyHeightRatio <= 0.85, "BellyHeightRatio must be 0.2..0.85.");
            Check(LowerCurveExponent >= 1.0 && LowerCurveExponent <= 3.0, "LowerCurveExponent must be 1..3.");
            Check(CrownFlatness >= 1.5 && CrownFlatness <= 5.0, "CrownFlatness must be 1.5..5.");
            Check(TipShankDiameter > 0 && CollarDiameter >= TipShankDiameter * 1.2, "CollarDiameter must be at least 1.2 x TipShankDiameter.");
            Check(CollarDiameter <= BodyDiameter * 0.6, "CollarDiameter must be at most 0.6 x BodyDiameter.");
            Check(TipExposedLength > 0 && TipExposedLength <= TotalHeight * 0.4, "TipExposedLength must be > 0 and at most 40% of TotalHeight.");
            Check(TipConeLength > 0 && TipConeLength <= TipExposedLength, "TipConeLength must be > 0 and at most TipExposedLength.");
            Check(TipPointRadius > 0 && TipPointRadius < ShankRadius * 0.8, "TipPointRadius must be > 0 and below 80% of the shank radius.");
            Check(TipEmbedDepth >= 0 && TipEmbedDepth <= BellyHeight - WoodBottomHeight, "TipEmbedDepth must stay inside the lower cone (0..belly height).");
            Check(RopeZoneStart >= 0 && RopeZoneStart < RopeZoneEnd && RopeZoneEnd <= 1, "Rope zone must satisfy 0 <= start < end <= 1.");
            Check(RopeGrooveCount >= 0 && RopeGrooveCount <= 12, "RopeGrooveCount must be 0..12.");
            Check(RopeGrooveDepth >= 0 && RopeGrooveDepth <= CollarRadius * 0.5, "RopeGrooveDepth must be at most half the collar radius.");
            Check(RopeGrooveWidth > 0, "RopeGrooveWidth must be positive.");
            Check(
                RopeGrooveCount * RopeGrooveWidth <= (RopeZoneEnd - RopeZoneStart) * (BellyHeight - WoodBottomHeight),
                "Rope grooves do not fit in the rope zone.");
            Check(RadialSegments >= 8 && RadialSegments <= 128, "RadialSegments must be 8..128.");
            Check(ProfileSegments >= 4 && ProfileSegments <= 128, "ProfileSegments must be 4..128.");
            Check(CollisionRadialSegments >= 6 && CollisionRadialSegments <= 24, "CollisionRadialSegments must be 6..24.");

            if (TipPointRadius > 0 && TipConeLength > 0 && ShankRadius > 0)
            {
                // The rounded point must end below the end of the taper.
                double alpha = System.Math.Atan(ShankRadius / TipConeLength);
                double minCone = TipPointRadius * System.Math.Cos(alpha) * System.Math.Cos(alpha) / System.Math.Sin(alpha);
                Check(TipConeLength > minCone, "TipPointRadius is too large for this TipConeLength.");
            }

            return errors;
        }

        /// <summary>Stable text that changes whenever anything affecting the generated mesh changes.</summary>
        public string GetSignature()
        {
            var sb = new StringBuilder();
            sb.Append('v').Append(GeneratorVersion);
            foreach (double value in new[]
            {
                TotalHeight, BodyDiameter, BellyHeightRatio, CollarDiameter, LowerCurveExponent, CrownFlatness,
                TipExposedLength, TipShankDiameter, TipConeLength, TipPointRadius, TipEmbedDepth,
                RopeZoneStart, RopeZoneEnd, RopeGrooveDepth, RopeGrooveWidth,
            })
            {
                sb.Append('|').Append(value.ToString("R", CultureInfo.InvariantCulture));
            }

            sb.Append('|').Append(RopeGrooveCount).Append('|').Append(RadialSegments)
              .Append('|').Append(ProfileSegments).Append('|').Append(CollisionRadialSegments);
            return sb.ToString();
        }
    }
}
