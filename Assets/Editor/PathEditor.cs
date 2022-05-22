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
        }

        bool isClosed = GUILayout.Toggle(Path.IsClosed, "Toggle Closed Path");
        if (isClosed != Path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            Path.IsClosed = isClosed;
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

    private void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            if (selectedSegmentIndex != -1)
            {
                Undo.RecordObject(creator, "Split Segment");
                Path.SplitSegment(mousePosition, selectedSegmentIndex);
            }
            else if(!Path.IsClosed)
            {
                Undo.RecordObject(creator, "Add Segment");
                Path.AddSegement(mousePosition);
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
                Vector2[] points = Path.GetPointsInSegement(i);
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
    }

    private void Draw()
    {
        for(int i = 0; i < Path.NumSegments; i++)
        {
            Vector2[] points = Path.GetPointsInSegement(i);
            if (creator.displayControlPoints)
            {
                Handles.color = Color.black;
                // Draw the control points lines to anchor point
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);
            }

            Color segmentColour = (i == selectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentColour : creator.segmentColour;

            // Draw curve between anchor points
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColour, null, 2f);
        }

        
        for(int i = 0; i < Path.NumPoints; i++)
        {
            //Handles.color = Path.AutoSetControlPoints && i % 3 != 0 ? Color.gray : Color.red;
            if (i % 3 == 0 || creator.displayControlPoints)
            {
                Handles.color = (i % 3 == 0) ? creator.anchorColour : creator.controlColour;
                float handleSize = (i % 3 == 0) ? creator.anchorDiameter : creator.controlDiameter;

                Vector2 newPosition = Handles.FreeMoveHandle(Path[i], Quaternion.identity, handleSize, Vector2.zero, Handles.CylinderHandleCap);
                if (Path[i] != newPosition)
                {
                    Undo.RecordObject(creator, "MovePoint");
                    Path.MovePoint(i, newPosition);
                }
            }
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
