using System;
using System.Collections.Generic;
using System.Numerics;
using DanhCu.Core.Tops;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core.Tops
{
    public sealed class TopShapeTests
    {
        private const double Guava = 900;
        private const double Steel = 7850;

        public static IEnumerable<TestCaseData> Presets()
        {
            yield return new TestCaseData(TopShapePresets.Small()).SetName("Small");
            yield return new TestCaseData(TopShapePresets.Medium()).SetName("Medium");
            yield return new TestCaseData(TopShapePresets.Large()).SetName("Large");
        }

        [TestCaseSource(nameof(Presets))]
        public void Preset_IsValid(TopShapeParameters p)
        {
            Assert.That(p.Validate(), Is.Empty);
        }

        [TestCaseSource(nameof(Presets))]
        public void Profile_IsContinuous_AndClosesAtTheAxis(TopShapeParameters p)
        {
            var profile = new TopProfile(p);

            // Rounded point meets the cone without a step.
            double below = profile.MetalRadiusAt(profile.TipTangentHeight - 1e-9);
            double above = profile.MetalRadiusAt(profile.TipTangentHeight + 1e-9);
            Assert.That(above, Is.EqualTo(below).Within(1e-7));
            Assert.That(profile.MetalRadiusAt(profile.ConeEndHeight + 1e-9), Is.EqualTo(p.ShankRadius).Within(1e-7));

            // Lower cone meets crown at the belly; crown closes at the axis.
            Assert.That(profile.WoodRadiusAt(p.BellyHeight + 1e-9), Is.EqualTo(p.BodyRadius).Within(1e-5));
            Assert.That(profile.WoodRadiusAt(p.TotalHeight), Is.EqualTo(0).Within(1e-9));
            Assert.That(profile.WoodRadiusAt(p.WoodBottomHeight + 1e-12), Is.EqualTo(p.CollarRadius).Within(1e-6));
        }

        [TestCaseSource(nameof(Presets))]
        public void Profile_BellyIsTheWidestPoint(TopShapeParameters p)
        {
            var profile = new TopProfile(p);
            for (double y = 0; y <= p.TotalHeight; y += p.TotalHeight / 500)
            {
                Assert.That(profile.WoodRadiusAt(y), Is.LessThanOrEqualTo(p.BodyRadius + 1e-12));
                Assert.That(profile.MetalRadiusAt(y), Is.LessThanOrEqualTo(p.ShankRadius + 1e-12));
            }
        }

        [TestCaseSource(nameof(Presets))]
        public void TipMesh_LowestPointIsOnTheAxisAtThePivot(TopShapeParameters p)
        {
            MeshData tip = TopMeshFactory.BuildTip(new TopProfile(p));

            float minY = float.MaxValue;
            Vector3 lowest = default;
            foreach (Vector3 v in tip.Positions)
            {
                if (v.Y < minY)
                {
                    minY = v.Y;
                    lowest = v;
                }
            }

            Assert.That(minY, Is.EqualTo(0f).Within(1e-7f));
            Assert.That(Math.Sqrt((lowest.X * lowest.X) + (lowest.Z * lowest.Z)), Is.LessThan(1e-7));
        }

        [TestCaseSource(nameof(Presets))]
        public void Meshes_AreAxisymmetric_AndWithinTheEnvelope(TopShapeParameters p)
        {
            var profile = new TopProfile(p);
            foreach (MeshData mesh in new[] { TopMeshFactory.BuildBody(profile), TopMeshFactory.BuildTip(profile), TopMeshFactory.BuildCollisionHull(profile) })
            {
                Vector3 sum = Vector3.Zero;
                int counted = 0;
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    Vector3 v = mesh.Positions[i];
                    Assert.That(v.Y, Is.InRange(-1e-6f, (float)p.TotalHeight + 1e-6f));
                    Assert.That(Math.Sqrt((v.X * v.X) + (v.Z * v.Z)), Is.LessThanOrEqualTo(p.BodyRadius + 1e-6));

                    // Skip the duplicated UV seam column (u = 1), which would bias the centroid.
                    if (mesh.Uvs[i].X < 0.999f)
                    {
                        sum += v;
                        counted++;
                    }
                }

                // Every ring is a full circle, so the vertex centroid lies on the Y axis.
                Vector3 centroid = sum / counted;
                Assert.That(Math.Abs(centroid.X), Is.LessThan(1e-4));
                Assert.That(Math.Abs(centroid.Z), Is.LessThan(1e-4));
            }
        }

        [TestCaseSource(nameof(Presets))]
        public void Meshes_HaveConsistentOutwardWinding_AndNoDegenerateTriangles(TopShapeParameters p)
        {
            var profile = new TopProfile(p);
            foreach (MeshData mesh in new[] { TopMeshFactory.BuildBody(profile), TopMeshFactory.BuildTip(profile) })
            {
                foreach (int[] tris in mesh.SubmeshTriangles)
                {
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 a = mesh.Positions[tris[i]];
                        Vector3 b = mesh.Positions[tris[i + 1]];
                        Vector3 c = mesh.Positions[tris[i + 2]];
                        Vector3 face = Vector3.Cross(b - a, c - a); // Unity convention: front-face normal
                        Assert.That(face.Length(), Is.GreaterThan(0f), "degenerate triangle");

                        Vector3 vertexNormal = mesh.Normals[tris[i]] + mesh.Normals[tris[i + 1]] + mesh.Normals[tris[i + 2]];
                        Assert.That(Vector3.Dot(face, vertexNormal), Is.GreaterThan(0f), "winding disagrees with normals");
                    }
                }
            }
        }

        [TestCaseSource(nameof(Presets))]
        public void Body_HasWoodAndRopeZoneSubmeshes(TopShapeParameters p)
        {
            MeshData body = TopMeshFactory.BuildBody(new TopProfile(p));

            Assert.That(body.SubmeshTriangles.Length, Is.EqualTo(2));
            Assert.That(body.SubmeshTriangles[TopMeshFactory.WoodSubmesh], Is.Not.Empty);
            Assert.That(body.SubmeshTriangles[TopMeshFactory.RopeZoneSubmesh], Is.Not.Empty);
            Assert.That(body.VertexCount, Is.LessThan(10000), "keep the top cheap to render");
        }

        [TestCaseSource(nameof(Presets))]
        public void CollisionHull_StaysAboveTheTipSphere_AndIsSmall(TopShapeParameters p)
        {
            var profile = new TopProfile(p);
            MeshData hull = TopMeshFactory.BuildCollisionHull(profile);

            foreach (Vector3 v in hull.Positions)
            {
                Assert.That(v.Y, Is.GreaterThanOrEqualTo((float)profile.TipTangentHeight - 1e-7f));
            }

            Assert.That(hull.VertexCount, Is.LessThan(255));
        }

        [Test]
        public void RopeZoneAtCollar_KeepsTheUndersideCrease()
        {
            TopShapeParameters p = TopShapePresets.Medium();
            p.RopeZoneStart = 0;
            MeshData body = TopMeshFactory.BuildBody(new TopProfile(p));

            // First vertex ring after the underside belongs to the rope strip and must face outward,
            // not be averaged with the downward-facing underside.
            int ring = p.RadialSegments + 1;
            Vector3 firstRopeNormal = body.Normals[2 * ring];
            Assert.That(firstRopeNormal.X, Is.GreaterThan(0.3f));
        }

        [TestCaseSource(nameof(Presets))]
        public void MassProperties_ArePlausible(TopShapeParameters p)
        {
            TopMassProperties m = RevolvedMassIntegrator.Compute(new TopProfile(p), Guava, Steel);

            Assert.That(m.Mass, Is.InRange(0.02, 0.5));
            Assert.That(m.CenterOfMassHeight, Is.InRange(p.WoodBottomHeight, p.TotalHeight));
            Assert.That(m.TransverseInertiaAtCenterOfMass, Is.GreaterThan(0));
            Assert.That(m.AxialInertia, Is.GreaterThan(0));
            Assert.That(m.CriticalAngularSpeed(9.81), Is.InRange(50, 400));
        }

        [Test]
        public void HeavierTip_LowersTheCentreOfMass()
        {
            TopShapeParameters p = TopShapePresets.Medium();
            double withSteel = RevolvedMassIntegrator.Compute(new TopProfile(p), Guava, Steel).CenterOfMassHeight;
            double withoutMetal = RevolvedMassIntegrator.Compute(new TopProfile(p), Guava, Guava).CenterOfMassHeight;

            Assert.That(withSteel, Is.LessThan(withoutMetal));
        }

        [Test]
        public void SizesAreOrdered()
        {
            double small = RevolvedMassIntegrator.Compute(new TopProfile(TopShapePresets.Small()), Guava, Steel).Mass;
            double medium = RevolvedMassIntegrator.Compute(new TopProfile(TopShapePresets.Medium()), Guava, Steel).Mass;
            double large = RevolvedMassIntegrator.Compute(new TopProfile(TopShapePresets.Large()), Guava, Steel).Mass;

            Assert.That(small, Is.LessThan(medium));
            Assert.That(medium, Is.LessThan(large));
        }

        [Test]
        public void MeshVolume_MatchesIntegratedVolume()
        {
            TopShapeParameters p = TopShapePresets.Medium();
            p.RadialSegments = 128;
            var profile = new TopProfile(p);
            MeshData body = TopMeshFactory.BuildBody(profile);

            // Body mesh is closed except the shank hole; add the hole disc back via the integrator's
            // wood volume + the shank cylinder that the wood mesh wraps around.
            double meshVolume = SignedVolume(body);
            double woodVolume = RevolvedMassIntegrator.Integrate(y => (0.0, profile.WoodRadiusAt(y)), profile.Breakpoints, 1, 0).WoodVolume;

            Assert.That(meshVolume, Is.EqualTo(woodVolume).Within(woodVolume * 0.01));
        }

        [Test]
        public void InvalidShape_IsRejectedWithReason()
        {
            TopShapeParameters p = TopShapePresets.Medium();
            p.CollarDiameter = p.TipShankDiameter;

            Assert.That(p.Validate(), Has.Some.Contains("CollarDiameter"));
            Assert.Throws<ArgumentException>(() => _ = new TopProfile(p));
        }

        [Test]
        public void Signature_ChangesWithAnyDimension()
        {
            TopShapeParameters a = TopShapePresets.Medium();
            TopShapeParameters b = a.Clone();
            b.TipEmbedDepth += 0.001;

            Assert.That(a.GetSignature(), Is.EqualTo(TopShapePresets.Medium().GetSignature()));
            Assert.That(b.GetSignature(), Is.Not.EqualTo(a.GetSignature()));
        }

        /// <summary>Divergence theorem with the hole under the body treated as a disc at the underside height.</summary>
        private static double SignedVolume(MeshData mesh)
        {
            double volume = 0;
            foreach (int[] tris in mesh.SubmeshTriangles)
            {
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 a = mesh.Positions[tris[i]];
                    Vector3 b = mesh.Positions[tris[i + 1]];
                    Vector3 c = mesh.Positions[tris[i + 2]];

                    // Clockwise (Unity) winding: outward normal = (b-a)x(c-a).
                    volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
                }
            }

            return volume;
        }
    }
}
