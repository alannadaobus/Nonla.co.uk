using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using DanhCu.Core.Tops;

namespace DanhCu.Tools.TopModelExport
{
    /// <summary>
    /// Writes, per preset: body/tip/collision OBJ (right-handed: X mirrored, as DCC tools and
    /// Unity's importer expect), a JSON description (profile, key heights, mass properties)
    /// and a Markdown summary table.
    /// </summary>
    internal static class Program
    {
        private const double Steel = 7850;

        private static readonly (string Name, double Density)[] Woods =
        {
            ("go_mit", 600), ("go_oi", 900), ("go_sen", 1050),
        };

        private static int Main(string[] args)
        {
            string outDir = args.Length > 0 ? args[0] : "top_model_export";
            Directory.CreateDirectory(outDir);

            var presets = new (string Name, TopShapeParameters Shape)[]
            {
                ("small", TopShapePresets.Small()),
                ("medium", TopShapePresets.Medium()),
                ("large", TopShapePresets.Large()),
            };

            var summary = new StringBuilder();
            summary.AppendLine("| Size | Wood | Mass (g) | Metal (g) | CoM above pivot (mm) | I₃ (kg·m²) | I₁ at CoM (kg·m²) | I₁ at pivot (kg·m²) | ω_crit (rad/s) | ω_crit (RPM) |");
            summary.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

            foreach ((string name, TopShapeParameters shape) in presets)
            {
                var profile = new TopProfile(shape);
                MeshData body = TopMeshFactory.BuildBody(profile);
                MeshData tip = TopMeshFactory.BuildTip(profile);
                MeshData hull = TopMeshFactory.BuildCollisionHull(profile);

                WriteObj(Path.Combine(outDir, $"top_{name}_body.obj"), body, new[] { "wood", "rope_zone" });
                WriteObj(Path.Combine(outDir, $"top_{name}_tip.obj"), tip, new[] { "metal" });
                WriteObj(Path.Combine(outDir, $"top_{name}_collision.obj"), hull, new[] { "collision" });

                var massByWood = new Dictionary<string, object>();
                foreach ((string wood, double density) in Woods)
                {
                    TopMassProperties m = RevolvedMassIntegrator.Compute(profile, density, Steel);
                    double crit = m.CriticalAngularSpeed(9.81);
                    massByWood[wood] = new
                    {
                        density,
                        massKg = m.Mass,
                        metalMassKg = m.MetalMass,
                        centerOfMassHeightM = m.CenterOfMassHeight,
                        axialInertia = m.AxialInertia,
                        transverseInertiaAtCom = m.TransverseInertiaAtCenterOfMass,
                        transverseInertiaAtPivot = m.TransverseInertiaAtPivot,
                        criticalRadPerSec = crit,
                    };
                    summary.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "| {0} | {1} ({2:0} kg/m³) | {3:0.0} | {4:0.0} | {5:0.0} | {6:0.00e0} | {7:0.00e0} | {8:0.00e0} | {9:0} | {10:0} |",
                        name, wood, density, m.Mass * 1000, m.MetalMass * 1000, m.CenterOfMassHeight * 1000,
                        m.AxialInertia, m.TransverseInertiaAtCenterOfMass, m.TransverseInertiaAtPivot, crit, crit * 60 / (2 * Math.PI)));
                }

                var json = new
                {
                    name,
                    parameters = shape,
                    derived = new
                    {
                        coneHalfAngleDeg = profile.ConeHalfAngle * 180 / Math.PI,
                        tipTangentHeight = profile.TipTangentHeight,
                        coneEndHeight = profile.ConeEndHeight,
                        woodBottomHeight = shape.WoodBottomHeight,
                        bellyHeight = shape.BellyHeight,
                        ropeZoneStartHeight = profile.RopeZoneStartHeight,
                        ropeZoneEndHeight = profile.RopeZoneEndHeight,
                        metalTopHeight = profile.MetalTopHeight,
                    },
                    mesh = new { bodyVertices = body.VertexCount, bodyTriangles = body.TriangleCount, tipVertices = tip.VertexCount, tipTriangles = tip.TriangleCount, hullVertices = hull.VertexCount, hullTriangles = hull.TriangleCount },
                    mass = massByWood,
                    woodOutline = SampleOutline(profile, y => profile.WoodRadiusAt(y), shape.WoodBottomHeight, shape.TotalHeight),
                    metalOutline = SampleOutline(profile, y => profile.MetalRadiusAt(y), 0, profile.MetalTopHeight),
                    hullOutline = Outline(hull, shape.CollisionRadialSegments),
                };
                File.WriteAllText(Path.Combine(outDir, $"top_{name}.json"), JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"{name}: body {body.VertexCount} v / {body.TriangleCount} t, tip {tip.VertexCount} v / {tip.TriangleCount} t, hull {hull.VertexCount} v");
            }

            File.WriteAllText(Path.Combine(outDir, "summary.md"), summary.ToString());
            Console.Write(summary);
            return 0;
        }

        private static List<double[]> SampleOutline(TopProfile profile, Func<double, double> radius, double y0, double y1)
        {
            var points = new List<double[]>();
            const int n = 400;
            for (int i = 0; i <= n; i++)
            {
                double y = y0 + ((y1 - y0) * i / n);
                points.Add(new[] { radius(Math.Min(y1, Math.Max(y0, y)) ), y });
            }

            return points;
        }

        /// <summary>Profile of the θ = 0 column of a lathed mesh.</summary>
        private static List<double[]> Outline(MeshData mesh, int radialSegments)
        {
            var points = new List<double[]>();
            for (int i = 0; i < mesh.VertexCount; i += radialSegments + 1)
            {
                points.Add(new double[] { mesh.Positions[i].X, mesh.Positions[i].Y });
            }

            return points;
        }

        private static void WriteObj(string path, MeshData mesh, string[] groups)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Danh Cu Vietnam procedural top - metres, Y up, right-handed (X mirrored from Unity)");
            foreach (Vector3 v in mesh.Positions)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}", -v.X, v.Y, v.Z));
            }

            foreach (Vector2 uv in mesh.Uvs)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "vt {0:R} {1:R}", uv.X, uv.Y));
            }

            foreach (Vector3 n in mesh.Normals)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "vn {0:R} {1:R} {2:R}", -n.X, n.Y, n.Z));
            }

            for (int s = 0; s < mesh.SubmeshTriangles.Length; s++)
            {
                sb.AppendLine("g " + groups[s]);
                sb.AppendLine("usemtl " + groups[s]);
                int[] tris = mesh.SubmeshTriangles[s];
                for (int i = 0; i < tris.Length; i += 3)
                {
                    // Mirroring X flips handedness, which turns Unity's clockwise order into OBJ's counter-clockwise.
                    int a = tris[i] + 1, b = tris[i + 1] + 1, c = tris[i + 2] + 1;
                    sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
                }
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
