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

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Create New"))
        {
            Undo.RecordObject(creator, "Create new");
            creator.CreatePath();
            
            selectedAnchorPoint = -1;
            selectedControlPointA = -1;
            selectedControlPointB = -1;
        }

        bool isClosed = GUILayout.Toggle(Path.IsClosed, "Toggle Closed Path");
        if (isClosed != Path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            Path.IsClosed = isClosed;
            
            selectedAnchorPoint = -1;
            selectedControlPointA = -1;
            selectedControlPointB = -1;
        }

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

    void UpdateMouseOverInfo(Ray mouseRay)
    {
        // Get direction of the mouse
        hoverOverPoint = -1;
        Vector3 mouseDir = mouseRay.direction.normalized;

        for (int i = 0; i < Path.NumPoints; i++)
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
                Debug.Log("Hovering over a point");
            }
        }
    }

    private void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        // Convert mouse position to world position via a ray
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            if (guiEvent.shift)
            {
                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(creator, "Split Segment");
                    Path.SplitSegment(mousePosition, selectedSegmentIndex);
                }
                else if (!Path.IsClosed)
                {
                    Undo.RecordObject(creator, "Add Segment");
                    Path.AddSegement(mousePosition);
                }
            }
            else
            {
                if (hoverOverPoint != -1)
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

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
        {
            float minimumDistance = creator.anchorDiameter * 0.5f;
            int closestAnchorIndex = -1;

            for (int i = 0; i < Path.NumPoints; i += 3)
            {
                float distance = Vector2.Distance(mousePosition, Path[i]);
                if (distance < minimumDistance)
                {
                    minimumDistance = distance;
                    closestAnchorIndex = i;
                }
            }

            if (closestAnchorIndex != -1)
            {
                Undo.RecordObject(creator, "Delete segment");
                Path.DeleteSegment(closestAnchorIndex);
            }
        }

        if (guiEvent.type == EventType.MouseMove)
        {
            float minimumDistanceToSegment = segmentSelectDistanceThreshold;
            int newSelecectedSegmentIndex = -1;
            for (int i = 0; i < Path.NumSegments; i++)
            {
                // Get distance from mouse point to segment bezier curve
                Vector3[] points = Path.GetPointsInSegement(i);
                float distance = HandleUtility.DistancePointBezier(mousePosition, points[0], points[3], points[1], points[2]);
                if (distance < minimumDistanceToSegment)
                {
                    minimumDistanceToSegment = distance;
                    newSelecectedSegmentIndex = i;
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

    private void Draw()
    {
        for(int i = 0; i < Path.NumSegments; i++)
        {
            Vector3[] points = Path.GetPointsInSegement(i);
            Color segmentColour = (i == selectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentColour : creator.segmentColour;

            // Draw curve between anchor points
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColour, null, 2f);
        }

        
        for(int i = 0; i < Path.NumPoints; i++)
        {
            if (i % 3 == 0 || (selectedAnchorPoint != -1 && (selectedAnchorPoint == i || selectedControlPointA == i || selectedControlPointB == i)))
            {
                Handles.color = (i % 3 == 0) ? creator.anchorColour : creator.controlColour;
                Handles.SphereHandleCap(0, Path[i], Quaternion.LookRotation(Vector3.up), 0.1f, EventType.Repaint);
                // Select the anchor point and its corresponding control points
                if (selectedAnchorPoint != -1 && (selectedAnchorPoint == i || selectedControlPointA == i || selectedControlPointB == i))
                {
                    Vector3 newPosition = Handles.PositionHandle(Path[i], Quaternion.identity);
                    if (Path[i] != newPosition) 
                    {
                        Undo.RecordObject(creator, "MovePoint");
                        Path.MovePoint(i, newPosition);
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
        creator = (PathCreator)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
    }
}
