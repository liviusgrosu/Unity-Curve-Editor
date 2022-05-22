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

    public void UpdateRoad()
    {
        Path path = GetComponent<PathCreator>().path;
        Vector2[] points = path.CalculateEvenlySpacedPoints(Spacing);
        GetComponent<MeshFilter>().mesh = CreateRoadMesh(points);
    }

    private Mesh CreateRoadMesh(Vector2[] points)
    {
        // 2n
        Vector3[] vertices = new Vector3[points.Length * 2];
        // 2(n-1)
        int[] triangles = new int[2 * (points.Length - 1) * 3];
        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 forward = Vector2.zero;
            // Not the last point
            if (i < points.Length - 1)
            {
                forward += points[i + 1] - points[i];
            }
            // Not the first point
            if (i > 0)
            {
                forward += points[i] - points[i - 1];
            }

            // take average of first and second conditions. Not first or last point
            forward.Normalize();
            // Perpindicular vector
            Vector2 left = new Vector2(-forward.y, forward.x);

            // Add the two points
            vertices[vertexIndex] = points[i] + left * RoadWidth * 0.5f;
            vertices[vertexIndex + 1] = points[i] - left * RoadWidth * 0.5f;

            if (i < points.Length - 1)
            {
                // First triangle
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 2;
                triangles[triangleIndex + 2] = vertexIndex + 1;

                // Second triangle
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            vertexIndex += 2;
            triangleIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }
}
