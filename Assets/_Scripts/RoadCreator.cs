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
    private List<int> anchorPointEquivalents;
    [Header("Path Shape")]
    [Range(0.001f, 2f)]
    public float PathDepth = 0.001f;
    [Range(0.001f, 2f)]
    public float PathEdgeWidth = 0.001f;

    /// <summary>
    /// Updates the path with parameters & new data
    /// </summary>
    public void UpdateRoad()
    {
        anchorPointEquivalents = new List<int>();
        Path path = GetComponent<PathCreator>().path;

        // Generate new evenly spaced points
        Vector3[] points = path.CalculateEvenlySpacedPoints(Spacing);
        
        // Get the anchor point equivalents to the evenly spaced points
        GenerateAnchorPointsEquivalents(path.points.ToArray(), points);
        // Generate the mesh itself
        GetComponent<MeshFilter>().mesh = CreateRoadMesh(points, path.IsClosed, path.Angles);

        // Apply the texture to the mesh
        int textureRepeat = Mathf.RoundToInt(tiling * points.Length * Spacing * 0.05f);
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);
    }

    /// <summary>
    /// Generate a road/path mesh given a set of points
    /// </summary>
    /// <param name="points">The list of evenly spaced points </param>
    /// <param name="isClosed">Whether or not the path is closed </param>
    /// <param name="angles">Rotation values of each anchor point </param>
    private Mesh CreateRoadMesh(Vector3[] points, bool isClosed, List<float> angles)
    {
        // 2n amount of vertices for the mesh
        Vector3[] vertices = new Vector3[points.Length * 4];
        Vector2[] uvs = new Vector2[points.Length * 4];
        // 8(n-1) amount of triangles for the mesh
        int numberOfTriangles = 8 * (points.Length - 1) + (isClosed ? 4 : 0);
        // 3 points for each triangle
        int[] triangles = new int[numberOfTriangles * 3];

        int vertexIndex = 0;
        int triangleIndex = 0;

        int currentAnchorIndex = 0;

        int currentEvenlySpacedIndex = 0;
        int nextEvenlySpacedIndex = 0;
        
        int totalPoints = 0;
        int currentLerpPoint = 0;

        for (int i = 0; i < points.Length; i++)
        {
            // If the current evenly spaced point is a anchor equivalent
            if (anchorPointEquivalents.Contains(i))
            {
                // Get the index of the anchor equivalent
                currentAnchorIndex = anchorPointEquivalents.IndexOf(i);

                // Get the current and next evenly spaced indices that have anchor point equivalences
                currentEvenlySpacedIndex = i;
                nextEvenlySpacedIndex = anchorPointEquivalents[(currentAnchorIndex + 1 + anchorPointEquivalents.Count) % anchorPointEquivalents.Count];
                
                if (isClosed && currentAnchorIndex == anchorPointEquivalents.Count - 1)
                {
                    // Calculate the number points if they the next evenly spaced index is 0
                    totalPoints = points.Length - currentEvenlySpacedIndex;
                }
                else
                {
                    // Lerp only for the total points between
                    totalPoints = Mathf.Abs(nextEvenlySpacedIndex - currentEvenlySpacedIndex);
                }
                currentLerpPoint = 0;
            }
            else
            {
                currentLerpPoint++;
            }

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

            // Take average of first and second conditions. Not first or last point
            forward.Normalize();
            // Perpindicular vector
            Vector3 left = new Vector3(-forward.z, 0f, forward.x);
            float lerpedAngle = Mathf.Lerp(angles[currentAnchorIndex], angles[(currentAnchorIndex + 1 + angles.Count) % angles.Count], (float)currentLerpPoint / (float)totalPoints);
            left = Quaternion.AngleAxis(lerpedAngle, forward) * left;

            Vector3 edgeDirection = new Vector3(0, PathDepth, 0);

            // Add a vertex for the edges and main polygons for this segment
            vertices[vertexIndex] = points[i] + left * RoadWidth * (0.5f + PathEdgeWidth) - edgeDirection;
            vertices[vertexIndex + 1] = points[i] + left * RoadWidth * 0.5f;
            vertices[vertexIndex + 2] = points[i] - left * RoadWidth * 0.5f;
            vertices[vertexIndex + 3] = points[i] - left * RoadWidth * (0.5f + PathEdgeWidth) - edgeDirection;

            // Add each UV coordinate based off how complete the path is
            float completionPercent = i / (float)(points.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertexIndex]        = new Vector2(0.0f, v);
            uvs[vertexIndex + 1]    = new Vector2(0.1f, v);
            uvs[vertexIndex + 2]    = new Vector2(0.9f, v);
            uvs[vertexIndex + 3]    = new Vector2(1.0f, v);

            // Create a band of triangles
            if (i < points.Length - 1 || isClosed)
            {
                // First triangle
                triangles[triangleIndex]        = vertexIndex;
                triangles[triangleIndex + 1]    = (vertexIndex + 4) % vertices.Length;
                triangles[triangleIndex + 2]    = vertexIndex + 1;

                // Second triangle
                triangles[triangleIndex + 3]    = vertexIndex + 1;
                triangles[triangleIndex + 4]    = (vertexIndex + 4) % vertices.Length;
                triangles[triangleIndex + 5]    = (vertexIndex + 5) % vertices.Length;

                // Third triangle
                triangles[triangleIndex + 6] = vertexIndex + 1;
                triangles[triangleIndex + 7] = (vertexIndex + 5) % vertices.Length;
                triangles[triangleIndex + 8] = (vertexIndex + 2) % vertices.Length;

                // Fourth triangle
                triangles[triangleIndex + 9] = vertexIndex + 2;
                triangles[triangleIndex + 10] = (vertexIndex + 5) % vertices.Length;
                triangles[triangleIndex + 11] = (vertexIndex + 6) % vertices.Length;

                // Fifth triangle
                triangles[triangleIndex + 12] = vertexIndex + 2;
                triangles[triangleIndex + 13] = (vertexIndex + 6) % vertices.Length;
                triangles[triangleIndex + 14] = (vertexIndex + 3) % vertices.Length;

                // Sixth triangle
                triangles[triangleIndex + 15] = vertexIndex + 3;
                triangles[triangleIndex + 16] = (vertexIndex + 6) % vertices.Length;
                triangles[triangleIndex + 17] = (vertexIndex + 7) % vertices.Length;
            }

            vertexIndex += 4;
            triangleIndex += 18;
        }
        
        // Create a mesh with its corresponding data
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        return mesh;
    }

    /// <summary>
    /// Generate a list of evenly spaced points that are represented to the anchor points
    /// </summary>
    /// <param name="pathPoints">The list of path points which includes anchor and control points </param>
    /// <param name="evenlySpreadPoints">The list of evenly spaced points </param>
    private void GenerateAnchorPointsEquivalents(Vector3[] pathPoints, Vector3[] evenlySpreadPoints)
    {
        for(int i = 0; i < pathPoints.Length; i++)
        {
            // Only look for anchor points
            if (i % 3 != 0)
            {
                continue;
            }

            float shortestDistance = Mathf.Infinity;
            int shortestPoint = -1;
            // Find a evenly spaced point that is the closest to anchor point
            for (int j = 0; j < evenlySpreadPoints.Length; j++)
            {
                if (Vector3.Distance(pathPoints[i], evenlySpreadPoints[j]) < shortestDistance)
                {
                    // New point found that is closest to an anchor point
                    shortestPoint = j;
                    shortestDistance = Vector3.Distance(pathPoints[i], evenlySpreadPoints[j]);
                }
            }
            anchorPointEquivalents.Add(shortestPoint);
        }
    }
}
