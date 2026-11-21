using System;

namespace DanhCu.Core.Tops
{
    /// <summary>
    /// Mass properties of a two-material solid of revolution about +Y.
    /// Inertia values are about the stated axes; the body is axisymmetric so I₁ = I₂.
    /// </summary>
    public readonly struct TopMassProperties
    {
        public TopMassProperties(double woodMass, double metalMass, double centerOfMassHeight, double axialInertia, double transverseInertiaAtPivot, double woodVolume, double metalVolume)
        {
            WoodMass = woodMass;
            MetalMass = metalMass;
            CenterOfMassHeight = centerOfMassHeight;
            AxialInertia = axialInertia;
            TransverseInertiaAtPivot = transverseInertiaAtPivot;
            WoodVolume = woodVolume;
            MetalVolume = metalVolume;
        }

        public double WoodMass { get; }

        public double MetalMass { get; }

        public double Mass => WoodMass + MetalMass;

        /// <summary>l: centre of mass height above the pivot (tip point), on the axis.</summary>
        public double CenterOfMassHeight { get; }

        /// <summary>I₃ about the spin axis (kg·m²).</summary>
        public double AxialInertia { get; }

        /// <summary>I₁ about a horizontal axis through the pivot (kg·m²).</summary>
        public double TransverseInertiaAtPivot { get; }

        /// <summary>I₁ about a horizontal axis through the centre of mass (kg·m²) — what a rigid body needs.</summary>
        public double TransverseInertiaAtCenterOfMass => TransverseInertiaAtPivot - (Mass * CenterOfMassHeight * CenterOfMassHeight);

        public double WoodVolume { get; }

        public double MetalVolume { get; }

        /// <summary>ω_crit = 2·√(I₁·m·g·l) / I₃ (PHYSICS_SPEC §4.2), rad/s.</summary>
        public double CriticalAngularSpeed(double gravity)
        {
            return 2.0 * Math.Sqrt(TransverseInertiaAtPivot * Mass * Math.Abs(gravity) * CenterOfMassHeight) / AxialInertia;
        }
    }

    /// <summary>Integrates mass properties of layered solids of revolution with composite Gauss–Legendre quadrature.</summary>
    public static class RevolvedMassIntegrator
    {
        private static readonly double[] GaussNodes = { -Math.Sqrt(3.0 / 5.0), 0.0, Math.Sqrt(3.0 / 5.0) };
        private static readonly double[] GaussWeights = { 5.0 / 9.0, 8.0 / 9.0, 5.0 / 9.0 };

        /// <summary>
        /// <paramref name="radii"/> returns, for a height y, the outer radius of the metal core and
        /// the outer radius of the wood around it. Metal fills r &lt; metal; wood fills metal &lt; r &lt; wood.
        /// Integration is split at <paramref name="breakpoints"/> (sorted or not) so kinks are exact;
        /// quadrature nodes never land on a breakpoint, so step changes are handled correctly.
        /// </summary>
        public static TopMassProperties Integrate(
            Func<double, (double metal, double wood)> radii,
            double[] breakpoints,
            double woodDensity,
            double metalDensity,
            int intervalsPerPiece = 64)
        {
            var points = (double[])breakpoints.Clone();
            Array.Sort(points);

            double woodVolume = 0, metalVolume = 0, mass = 0, moment = 0, axial = 0, transverse = 0;
            for (int p = 0; p < points.Length - 1; p++)
            {
                double a = points[p];
                double b = points[p + 1];
                if (b - a <= 0)
                {
                    continue;
                }

                double h = (b - a) / intervalsPerPiece;
                for (int i = 0; i < intervalsPerPiece; i++)
                {
                    double mid = a + ((i + 0.5) * h);
                    for (int g = 0; g < GaussNodes.Length; g++)
                    {
                        double y = mid + (GaussNodes[g] * h * 0.5);
                        double w = GaussWeights[g] * h * 0.5;
                        (double rm, double rw) = radii(y);
                        rm = Math.Max(0, rm);
                        rw = Math.Max(rm, rw);

                        double rm2 = rm * rm;
                        double rw2 = rw * rw;
                        double areaMetal = Math.PI * rm2;
                        double areaWood = Math.PI * (rw2 - rm2);
                        double dm = (metalDensity * areaMetal) + (woodDensity * areaWood);

                        // Thin disc/annulus: dI₃ = ρ·π/2·(r_o⁴ − r_i⁴) dy; dI_x(own) = dI₃/2.
                        double dI3 = (metalDensity * Math.PI * 0.5 * rm2 * rm2) + (woodDensity * Math.PI * 0.5 * ((rw2 * rw2) - (rm2 * rm2)));

                        metalVolume += areaMetal * w;
                        woodVolume += areaWood * w;
                        mass += dm * w;
                        moment += dm * y * w;
                        axial += dI3 * w;
                        transverse += ((dI3 * 0.5) + (dm * y * y)) * w;
                    }
                }
            }

            double com = mass > 0 ? moment / mass : 0;
            return new TopMassProperties(woodVolume * woodDensity, metalVolume * metalDensity, com, axial, transverse, woodVolume, metalVolume);
        }

        /// <summary>Mass properties of a top made of <paramref name="woodDensity"/> wood with a metal tip.</summary>
        public static TopMassProperties Compute(TopProfile profile, double woodDensity, double metalDensity)
        {
            return Integrate(y => (profile.MetalRadiusAt(y), profile.WoodRadiusAt(y)), profile.Breakpoints, woodDensity, metalDensity);
        }
    }
}
