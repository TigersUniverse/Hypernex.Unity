
using System.Collections.Generic;
using UnityEngine;

// Require a mesh filter component
// This script, unfortunately, does not support skinned meshes
[RequireComponent(typeof(MeshFilter))]
public class OutlineNormalsCalculator : MonoBehaviour
{
    // Store these outline normals in the specified UV/Texcoord channel
    // This corresponds to the TEXCOORD_ semantics in HLSL
    [SerializeField] private int storeInTexcoordChannel = 1;
    // The maximum distance apart two vertices must be to be merged
    [SerializeField] private float cospatialVertexDistance = 0.01f;

    // This class holds the accumulated normal for merged, or cospatial, vertices
    private class CospatialVertex
    {
        public Vector3 position;
        public Vector3 accumulatedNormal;
    }

    // We'll run the algorithm in the start function
    // It would be better to run this in the editor at compile time, but that's another video
    private void Start()
    {
        // Get the mesh
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        // Copy the vertices and triangle arrays from the mesh
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        // Create a new outline normal array
        Vector3[] outlineNormals = new Vector3[vertices.Length];

        // Create a data structure to find and compile cospacial vertices, or vertices which
        // are so close together we'll consider them the same vertex
        List<CospatialVertex> cospatialVerticesData = new List<CospatialVertex>();
        // This array maps vertex index -> cospatialVerticesData index
        int[] cospacialVertexIndices = new int[vertices.Length];
        FindCospatialVertices(vertices, cospacialVertexIndices, cospatialVerticesData);

        // Unity stores triangles as three vertex indices, so for every three entries there is one triangle
        int numTriangles = triangles.Length / 3;
        // For each triangle
        for (int t = 0; t < numTriangles; t++)
        {
            // Get the three vertex indices making up this triangle
            int vertexStart = t * 3;
            int v1Index = triangles[vertexStart];
            int v2Index = triangles[vertexStart + 1];
            int v3Index = triangles[vertexStart + 2];
            // Get this triangle's normal vector and the weight for each vertex
            ComputeNormalAndWeights(vertices[v1Index], vertices[v2Index], vertices[v3Index], out Vector3 normal, out Vector3 weights);
            // Add the weighted normal to each cospatial vertex data instance
            AddWeightedNormal(normal * weights.x, v1Index, cospacialVertexIndices, cospatialVerticesData);
            AddWeightedNormal(normal * weights.y, v2Index, cospacialVertexIndices, cospatialVerticesData);
            AddWeightedNormal(normal * weights.z, v3Index, cospacialVertexIndices, cospatialVerticesData);
        }

        // For each vertex
        for (int v = 0; v < outlineNormals.Length; v++)
        {
            // Find the cospacial registry index for this vertex
            int cvIndex = cospacialVertexIndices[v];
            // Get the cospatial data object
            var cospatial = cospatialVerticesData[cvIndex];
            // Normalize the accumulated normal
            // This averages it
            outlineNormals[v] = cospatial.accumulatedNormal.normalized;
        }

        // Store the outline normals in the mesh's UV channel
        mesh.SetUVs(storeInTexcoordChannel, outlineNormals);
    }

    private void FindCospatialVertices(Vector3[] vertices, int[] indices, List<CospatialVertex> registry)
    {
        // For each vertex
        for (int v = 0; v < vertices.Length; v++)
        {
            if (SearchForPreviouslyRegisteredCV(vertices[v], registry, out int index))
            {
                // If this vertex is cospatial with another, then register the data index
                indices[v] = index;
            }
            else
            {
                // If this vertex is unique, create a new cospacial vertex data object
                var cospatialEntry = new CospatialVertex()
                {
                    position = vertices[v],
                    accumulatedNormal = Vector3.zero,
                };
                // Set the cospatial index to this new object's index in the list
                indices[v] = registry.Count;
                registry.Add(cospatialEntry);
            }
        }
    }

    private bool SearchForPreviouslyRegisteredCV(Vector3 position, List<CospatialVertex> registry, out int index)
    {
        // For each registry entry
        for (int i = 0; i < registry.Count; i++)
        {
            // If the vertex is close enough, consider it cospatial
            if (Vector3.Distance(registry[i].position, position) <= cospatialVertexDistance)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    private void ComputeNormalAndWeights(Vector3 a, Vector3 b, Vector3 c, out Vector3 normal, out Vector3 weights)
    {
        // The normal of a triangle is the normal of a plane containing the triangle
        // We can calculate this by constructing two vectors on the plane and taking their cross product
        // Unity's triangles are wound clockwise, so taking the cross product this way will produce a normal
        // pointing the right way
        normal = Vector3.Cross(b - a, c - a).normalized;
        // We want to weight each normal by the angle between the two triangle lines containing this point
        // This makes it so vertices on faces made from very many triangles are not over counted
        weights = new Vector3(Vector3.Angle(b - a, c - a), Vector3.Angle(c - b, a - b), Vector3.Angle(a - c, b - c));
    }

    private void AddWeightedNormal(Vector3 weightedNormal, int vertexIndex, int[] cvIndices, List<CospatialVertex> cvRegistry)
    {
        // Find the cospatial vertex data index
        int cvIndex = cvIndices[vertexIndex];
        // Add the weighted normal
        cvRegistry[cvIndex].accumulatedNormal += weightedNormal;
    }
}