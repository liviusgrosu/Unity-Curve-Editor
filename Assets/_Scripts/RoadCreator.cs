using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadCreator : MonoBehaviour
{
    public float RoadWidth = 1f;
    [Range(0.01f, 1.5f)]
    public float Spacing = 1f;
    public bool AutoUpdate;
    public float tiling = 1f;

    public void UpdateRoad()
    {
        Path path = GetComponent<PathCreator>().path;
        Vector3[] points = path.CalculateEvenlySpacedPoints(Spacing);
        // TODO: grab the rotations here as well
        // Pass angles to createroadmesh
        GetComponent<MeshFilter>().mesh = CreateRoadMesh(points, path.IsClosed);

        int textureRepeat = Mathf.RoundToInt(tiling * points.Length * Spacing * 0.05f);
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);
    }

    private Mesh CreateRoadMesh(Vector3[] points, bool isClosed)
    {
        // 2n
        Vector3[] vertices = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[points.Length * 2];
        // 2(n-1)
        int numberOfTriangles = 2 * (points.Length - 1) + (isClosed ? 2 : 0);
        int[] triangles = new int[numberOfTriangles * 3];
        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 forward = Vector3.zero;
            // Not the last point
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }
            // Not the first point
            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }

            // take average of first and second conditions. Not first or last point
            forward.Normalize();
            // Perpindicular vector
            Vector3 left = new Vector3(-forward.z, 0f, forward.x);
            
            // --- TEMP ---
            // left = Quaternion.AngleAxis(angle, forward) * left;
            // ------------

            // Add the two points
            vertices[vertexIndex] = points[i] + left * RoadWidth * 0.5f;
            vertices[vertexIndex + 1] = points[i] - left * RoadWidth * 0.5f;

            // Add each UV coordinate based off how complete the path is
            float completionPercent = i / (float)(points.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertexIndex] = new Vector2(0, v);
            uvs[vertexIndex + 1] = new Vector2(1, v);

            if (i < points.Length - 1 || isClosed)
            {
                // First triangle
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = (vertexIndex + 2) % vertices.Length;
                triangles[triangleIndex + 2] = vertexIndex + 1;

                // Second triangle
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = (vertexIndex + 2) % vertices.Length;
                triangles[triangleIndex + 5] = (vertexIndex + 3) % vertices.Length;
            }

            vertexIndex += 2;
            triangleIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        return mesh;
    }
}
