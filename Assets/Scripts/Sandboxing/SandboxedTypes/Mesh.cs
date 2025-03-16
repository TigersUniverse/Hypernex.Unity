using System.Collections.Generic;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Unity.Collections;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Mesh
    {
        internal UnityEngine.Mesh r;

        public Mesh() => r = new UnityEngine.Mesh();
        internal Mesh(UnityEngine.Mesh r)
        {
            this.r = r;
        }

        public bool IsReadable => r.isReadable; // this is not the same as sandbox readonly, its if the mesh is in CPU RAM or GPU VRAM.

        public void Clear()
        {
            r.Clear();
        }

        public float3[] GetVertices()
        {
            List<Vector3> verts = new List<Vector3>();
            r.GetVertices(verts);
            float3[] vs = new float3[verts.Count];
            for (int i = 0; i < vs.Length; i++)
                vs[i] = NetworkConversionTools.Vector3Tofloat3(verts[i]);
            return vs;
        }

        public int[] GetIndices(int submesh)
        {
            return r.GetIndices(submesh);
        }

        public void SetVertices(float3[] verts)
        {
            NativeArray<Vector3> vectors = new NativeArray<Vector3>(verts.Length, Allocator.Temp);
            for (int i = 0; i < verts.Length; i++)
                vectors[i] = NetworkConversionTools.float3ToVector3(verts[i]);
            r.SetVertices(vectors);
            vectors.Dispose();
        }

        public void SetNormals(float3[] verts)
        {
            NativeArray<Vector3> vectors = new NativeArray<Vector3>(verts.Length, Allocator.Temp);
            for (int i = 0; i < verts.Length; i++)
                vectors[i] = NetworkConversionTools.float3ToVector3(verts[i]);
            r.SetNormals(vectors);
            vectors.Dispose();
        }

        public void SetTangents(float4[] verts)
        {
            NativeArray<Vector4> vectors = new NativeArray<Vector4>(verts.Length, Allocator.Temp);
            for (int i = 0; i < verts.Length; i++)
                vectors[i] = NetworkConversionTools.float4ToVector4(verts[i]);
            r.SetTangents(vectors);
            vectors.Dispose();
        }

        public void SetUVs(float2[] verts)
        {
            NativeArray<Vector2> vectors = new NativeArray<Vector2>(verts.Length, Allocator.Temp);
            for (int i = 0; i < verts.Length; i++)
                vectors[i] = NetworkConversionTools.float2ToVector2(verts[i]);
            r.SetUVs(0, vectors);
            vectors.Dispose();
        }

        public void SetIndices(int[] verts, int submesh)
        {
            NativeArray<int> vectors = new NativeArray<int>(verts.Length, Allocator.Temp);
            for (int i = 0; i < verts.Length; i++)
                vectors[i] = verts[i];
            r.SetIndices(vectors, MeshTopology.Triangles, submesh);
            vectors.Dispose();
        }

        public int SubMeshCount
        {
            get => r.subMeshCount;
            set => r.subMeshCount = value;
        }

        public void SetSubMesh(int subIndex, int indexStart, int indexCount)
        {
            r.SetSubMesh(subIndex, new UnityEngine.Rendering.SubMeshDescriptor(indexStart, indexCount));
        }

        public void RecalculateNormals()
        {
            r.RecalculateNormals();
        }
    }
}