using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    PathCreator creator;
    Path Path
    {
        get
        {
            return creator.path;
        }
    }

    int hoverOverPoint = -1;
    int selectedAnchorPoint = -1;
    int selectedControlPointA = -1;
    int selectedControlPointB = -1;

    const float segmentSelectDistanceThreshold = 0.5f;
    int selectedSegmentIndex = -1;
    Plane xzPlane = new Plane(Vector3.up, Vector3.zero);

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Button for creating a new path
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Create New"))
        {
            Undo.RecordObject(creator, "Create new");
            creator.CreatePath();
            ResetSelected();
        }

        // Button for toggling the paths closed state
        bool isClosed = GUILayout.Toggle(Path.IsClosed, "Toggle Closed Path");
        if (isClosed != Path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            Path.IsClosed = isClosed;
            ResetSelected();
        }

        // Radio button for toggling the auto set feature in the path
        bool autoSetControlPoints = GUILayout.Toggle(Path.AutoSetControlPoints, "Auto Set Control Points");
        if (autoSetControlPoints != Path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set controls");
            Path.AutoSetControlPoints = autoSetControlPoints;
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    /// <summary>
    /// Find the closest point to the mouse ray
    /// </summary>
    /// <param name="mouseRay">Ray coming off the mouse position on screen to world </param>
    void UpdateMouseOverInfo(Ray mouseRay)
    {
        // Get direction of the mouse
        hoverOverPoint = -1;
        Vector3 mouseDir = mouseRay.direction.normalized;

        // For each point find the closest one to the mouse ray
        for (int i = 0; i < Path.NumPoints; i += 3)
        {
            Vector3 point = Path[i];

            float x = point.x - mouseRay.origin.x;
            float y = point.y - mouseRay.origin.y;
            float z = point.z - mouseRay.origin.z;

            float t = (x * mouseDir.x + y * mouseDir.y + z * mouseDir.z) /
                        (mouseDir.x * mouseDir.x + mouseDir.y * mouseDir.y + mouseDir.z * mouseDir.z);

            float D1 = (mouseDir.x * mouseDir.x + mouseDir.y * mouseDir.y + mouseDir.z * mouseDir.z) * (t * t);
            float D2 = (x * mouseDir.x + y * mouseDir.y + z * mouseDir.z) * 2 * t;
            float D3 = (x * x + y * y + z * z);

            float D = D1 - D2 + D3;

            if (D < 0.005f)
            {
                hoverOverPoint = i;
            }
        }
    }

    /// <summary>
    /// Handle input from user
    /// </summary>
    private void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        // Convert mouse position to world position via a ray
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        
        
        Vector3 xyPoint = Vector3.zero;
        float hitdist = 0.0f;

        // Get the point that intersects with mouse ray and xz plane and use it for adding new segments
        if (xzPlane.Raycast(mouseRay, out hitdist))
        {
            xyPoint = mouseRay.GetPoint(hitdist);
        }
        // Take into consideration when point is behind the plane
        if (hitdist < -1.0f)
        {
            xyPoint = mouseRay.GetPoint(-hitdist);
        }

        // User pressing left-click 
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            if (guiEvent.shift)
            {
                // Add a new point in between a two segments
                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(creator, "Split Segment");
                    Path.SplitSegment(xyPoint, selectedSegmentIndex);
                }
                // Add a new point
                else if (!Path.IsClosed)
                {
                    Undo.RecordObject(creator, "Add Segment");
                    Path.AddSegement(xyPoint);
                }
            }
            else
            {
                // Can't select if its already selected
                if (hoverOverPoint != -1 && hoverOverPoint != selectedAnchorPoint)
                {
                    Undo.RecordObject(creator, "Select segment");
                    // Reset the control points
                    selectedControlPointA = -1;
                    selectedControlPointB = -1;
                    // Select new anchor point
                    selectedAnchorPoint = hoverOverPoint;

                    // Select corresponding control points
                    if (!Path.IsClosed && selectedAnchorPoint == 0)
                    {
                        selectedControlPointB = hoverOverPoint + 1;
                    }
                    else if (!Path.IsClosed && selectedAnchorPoint == Path.NumPoints - 1)
                    {
                        selectedControlPointA = hoverOverPoint - 1;
                    }
                    else
                    {
                        selectedControlPointA = (hoverOverPoint - 1 + Path.NumPoints) % Path.NumPoints;
                        selectedControlPointB = (hoverOverPoint + 1 + Path.NumPoints) % Path.NumPoints;
                    }
                }
            }
        }

        // User pressing left-click and holding down the control button
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control)
        {
            // Delete the point being hovered over
            if (hoverOverPoint != -1)
            {
                Undo.RecordObject(creator, "Delete segment");
                ResetSelected();
                Path.DeleteSegment(hoverOverPoint);
            }
        }

        if (guiEvent.type == EventType.MouseMove)
        {
            // Find the closest segement near the mouse ray
            float smallestDistanceToSegment = segmentSelectDistanceThreshold;
            int newSelecectedSegmentIndex = -1;

            for (int i = 0; i < Path.NumSegments; i++)
            {
                Vector3 closestPoint1, closestPoint2;
                Vector3[] points = Path.GetPointsInSegement(i);
                Vector3 segmentLine = points[3] - points[0];

                Vector3 sampleMousePoint = mouseRay.origin + mouseRay.direction * 0.5f;
                Vector3 sampleSegmentPoint = points[0] + segmentLine * 0.5f;

                if (ClosestPointsOnTwoLines(out closestPoint1, out closestPoint2, sampleMousePoint, mouseRay.direction, sampleSegmentPoint, segmentLine))
                {
                    float distance = Vector3.Distance(closestPoint1, closestPoint2);
                    if (distance < smallestDistanceToSegment)
                    {
                        smallestDistanceToSegment = distance;
                        newSelecectedSegmentIndex = i;
                    }
                }
            }

            if (newSelecectedSegmentIndex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newSelecectedSegmentIndex;
                HandleUtility.Repaint();
            }
        }

        // Stops the mesh from being selected
        HandleUtility.AddDefaultControl(0);
        UpdateMouseOverInfo(mouseRay);
    }


    /// <summary>
    /// Find the shortest vector between two skew lines
    /// </summary>
    /// <param name="closestPointLine1">Point A on shortest distance vector </param>
    /// <param name="closestPointLine2">Point B on shortest distance vector </param>
    /// <param name="linePoint1">Sample point on skew line 1 </param>
    /// <param name="lineVec1">Skew line 1 </param>
    /// <param name="linePoint2">Sample point on skew line 2 </param>
    /// <param name="lineVec2">Skew line 2 </param>
    public bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        // Find dot product on skew lines
        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        // Find determinant
        float determinant = a * e - b * b;

        // Lines should not be parallel
        if (determinant != 0.0f)
        {
            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / determinant;
            float t = (a * f - c * b) / determinant;

            // Get the points of shortest vector
            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Draw any GUI elements needed
    /// </summary>
    private void Draw()
    {
        // Draw bezier curve from each segment
        for(int i = 0; i < Path.NumSegments; i++)
        {
            Vector3[] points = Path.GetPointsInSegement(i);
            Color segmentColour = (i == selectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentColour : creator.segmentColour;

            // Draw curve between anchor points
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColour, null, 2f);
        }

        // Draw appropriate handles for selected anchor point and its corresponding control points
        for(int i = 0; i < Path.NumPoints; i++)
        {
            if (i % 3 == 0 || (selectedAnchorPoint != -1 && (selectedAnchorPoint == i || selectedControlPointA == i || selectedControlPointB == i)))
            {
                // Display a sphere for each point
                Handles.color = (i % 3 == 0) ? creator.anchorColour : creator.controlColour;
                Handles.SphereHandleCap(0, Path[i], Quaternion.LookRotation(Vector3.up), 0.1f, EventType.Repaint);
                // Select the anchor point and its corresponding control points
                if (selectedAnchorPoint != -1 && (selectedAnchorPoint == i || selectedControlPointA == i || selectedControlPointB == i))
                {
                    // Display a handle for each point such that users can use to move it around
                    Vector3 newPosition = Handles.PositionHandle(Path[i], Quaternion.identity);
                    if (Path[i] != newPosition) 
                    {
                        Undo.RecordObject(creator, "MovePoint");
                        Path.MovePoint(i, newPosition);
                    }

                    if (i % 3 == 0)
                    {
                        // Use control points to get the forward direction of the disc handle
                        Vector3 forwardDirection = Vector3.zero;
                        if (i == 0)
                        {
                            forwardDirection = Path[i + 1] - Path[i];
                        }
                        else if (i == Path.NumPoints - 1)
                        {
                            forwardDirection = Path[i - 1] - Path[i];
                        }
                        else
                        {
                            forwardDirection = Path[i + 1] - Path[i - 1];
                        }
                        // Draw disc handle for rotation
                        Quaternion newRotation = Handles.Disc(Quaternion.identity, Path[i], forwardDirection, 1, false, 1);

                        // Rotate the points rotation when the disc has been manipulated
                        if (Path.Rotations[i / 3] != newRotation)
                        {
                            Undo.RecordObject(creator, "RotatePoint");
                            Path.RotatePoint(i / 3, newRotation);
                        }

                        // Get the arcs up vector
                        Vector3 pointLeft = Vector3.Cross(forwardDirection, Vector3.up);
                        Vector3 pointUp = Vector3.Cross(pointLeft, forwardDirection);

                        // Display the points angle right above the arch
                        GUIStyle textStyle = new GUIStyle();
                        textStyle.normal.textColor = Color.cyan;
                        Handles.Label(Path[i] + pointUp.normalized * 1.25f, $"Δ {Path.Angles[i / 3]}", textStyle);
                    }
                }
            }
        }

        // Display the lines between the control and anchor point
        Handles.color = Color.black;
        if (selectedControlPointA != -1)
        {
            Handles.DrawLine(Path[selectedAnchorPoint], Path[selectedControlPointA]);
        }
        if (selectedControlPointB != -1)
        {
            Handles.DrawLine(Path[selectedAnchorPoint], Path[selectedControlPointB]);
        }
    }

    private void OnEnable()
    {
        // Create new path once object is enabled and no previous path exists
        creator = (PathCreator)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
    }

    /// <summary>
    /// Reset the previously selected points
    /// </summary>
    private void ResetSelected()
    {
        selectedAnchorPoint = -1;
        selectedControlPointA = -1;
        selectedControlPointB = -1;
    }
}
