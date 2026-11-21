using System;
using DanhCu.Core.Tops;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core.Tops
{
    /// <summary>Checks the quadrature against closed-form solids.</summary>
    public sealed class RevolvedMassIntegratorTests
    {
        private const double Rho = 900.0;

        [Test]
        public void SolidCylinder_MatchesClosedForm()
        {
            const double r = 0.03;
            const double h = 0.08;
            TopMassProperties p = RevolvedMassIntegrator.Integrate(y => (0.0, r), new[] { 0.0, h }, Rho, 7850);

            double m = Rho * Math.PI * r * r * h;
            Assert.That(p.Mass, Is.EqualTo(m).Within(1e-9));
            Assert.That(p.CenterOfMassHeight, Is.EqualTo(h / 2).Within(1e-9));
            Assert.That(p.AxialInertia, Is.EqualTo(0.5 * m * r * r).Within(1e-12));
            Assert.That(p.TransverseInertiaAtPivot, Is.EqualTo(m * ((r * r / 4) + (h * h / 3))).Within(1e-12));
            Assert.That(p.TransverseInertiaAtCenterOfMass, Is.EqualTo(m * ((r * r / 4) + (h * h / 12))).Within(1e-12));
        }

        [Test]
        public void SolidCone_ApexDown_MatchesClosedForm()
        {
            const double r = 0.035;
            const double h = 0.07;
            TopMassProperties p = RevolvedMassIntegrator.Integrate(y => (0.0, r * y / h), new[] { 0.0, h }, Rho, 7850);

            double m = Rho * Math.PI * r * r * h / 3;
            Assert.That(p.Mass, Is.EqualTo(m).Within(m * 1e-9));
            Assert.That(p.CenterOfMassHeight, Is.EqualTo(0.75 * h).Within(1e-9));
            Assert.That(p.AxialInertia, Is.EqualTo(0.3 * m * r * r).Within(1e-12));
            Assert.That(p.TransverseInertiaAtPivot, Is.EqualTo(m * ((0.15 * r * r) + (0.6 * h * h))).Within(1e-11));
        }

        [Test]
        public void MetalCoreInWood_SplitsMassCorrectly()
        {
            const double rWood = 0.02;
            const double rMetal = 0.003;
            const double h = 0.05;
            TopMassProperties p = RevolvedMassIntegrator.Integrate(y => (rMetal, rWood), new[] { 0.0, h }, Rho, 7850);

            Assert.That(p.MetalMass, Is.EqualTo(7850 * Math.PI * rMetal * rMetal * h).Within(1e-9));
            Assert.That(p.WoodMass, Is.EqualTo(Rho * Math.PI * ((rWood * rWood) - (rMetal * rMetal)) * h).Within(1e-9));
        }

        [Test]
        public void StepChangeAtBreakpoint_IsIntegratedExactly()
        {
            // Two stacked cylinders of different radii; the step is at a breakpoint.
            TopMassProperties p = RevolvedMassIntegrator.Integrate(
                y => (0.0, y < 0.01 ? 0.005 : 0.02), new[] { 0.0, 0.01, 0.03 }, Rho, 0, intervalsPerPiece: 4);

            double expected = Rho * Math.PI * ((0.005 * 0.005 * 0.01) + (0.02 * 0.02 * 0.02));
            Assert.That(p.Mass, Is.EqualTo(expected).Within(1e-12));
        }
    }
}
