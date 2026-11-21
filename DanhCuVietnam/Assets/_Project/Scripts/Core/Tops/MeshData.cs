using System.Numerics;

namespace DanhCu.Core.Tops
{
    /// <summary>
    /// Engine-free triangle mesh. Uses Unity's conventions so it can be copied straight into
    /// a UnityEngine.Mesh: left-handed, +Y up, clockwise front faces, UV origin bottom-left.
    /// </summary>
    public sealed class MeshData
    {
        public MeshData(Vector3[] positions, Vector3[] normals, Vector2[] uvs, int[][] submeshTriangles)
        {
            Positions = positions;
            Normals = normals;
            Uvs = uvs;
            SubmeshTriangles = submeshTriangles;
        }

        public Vector3[] Positions { get; }

        public Vector3[] Normals { get; }

        public Vector2[] Uvs { get; }

        /// <summary>One index array per submesh (material slot).</summary>
        public int[][] SubmeshTriangles { get; }

        public int VertexCount => Positions.Length;

        public int TriangleCount
        {
            get
            {
                int count = 0;
                foreach (int[] submesh in SubmeshTriangles)
                {
                    count += submesh.Length / 3;
                }

                return count;
            }
        }
    }
}
