using DanhCu.Core.Tops;
using UnityEngine;
using UnityEngine.Rendering;

namespace DanhCu.Tops
{
    /// <summary>Copies engine-free <see cref="MeshData"/> into Unity meshes (same handedness and winding).</summary>
    public static class TopMeshUtility
    {
        public static Mesh CreateMesh(MeshData data, string name)
        {
            var mesh = new Mesh { name = name };
            Fill(mesh, data);
            return mesh;
        }

        /// <summary>Overwrites <paramref name="target"/> in place so asset references to it stay valid.</summary>
        public static void Fill(Mesh target, MeshData data)
        {
            int count = data.VertexCount;
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                System.Numerics.Vector3 p = data.Positions[i];
                System.Numerics.Vector3 n = data.Normals[i];
                System.Numerics.Vector2 uv = data.Uvs[i];
                positions[i] = new Vector3(p.X, p.Y, p.Z);
                normals[i] = new Vector3(n.X, n.Y, n.Z);
                uvs[i] = new Vector2(uv.X, uv.Y);
            }

            target.Clear();
            target.indexFormat = count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            target.vertices = positions;
            target.normals = normals;
            target.uv = uvs;
            target.subMeshCount = data.SubmeshTriangles.Length;
            for (int s = 0; s < data.SubmeshTriangles.Length; s++)
            {
                target.SetTriangles(data.SubmeshTriangles[s], s, false);
            }

            target.RecalculateBounds();
            target.RecalculateTangents();
        }

        /// <summary>Builds the three meshes of a top. Caller owns (and must destroy) them.</summary>
        public static void BuildAll(TopProfile profile, string baseName, out Mesh body, out Mesh tip, out Mesh collision)
        {
            body = CreateMesh(TopMeshFactory.BuildBody(profile), baseName + "_Body");
            tip = CreateMesh(TopMeshFactory.BuildTip(profile), baseName + "_Tip");
            collision = CreateMesh(TopMeshFactory.BuildCollisionHull(profile), baseName + "_Collision");
        }
    }
}
