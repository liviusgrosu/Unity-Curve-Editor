using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    PathCreator creator;
    Path path;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create New"))
        {
            creator.CreatePath();
            path = creator.path;
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Toggle Closed"))
        {
            path.ToggleClosed();
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
            Undo.RecordObject(creator, "AddSegment");
            path.AddSegement(mousePosition);
        }
    }

    private void Draw()
    {
        for(int i = 0; i < path.NumSegments; i++)
        {
            Vector2[] points = path.GetPointsInSegement(i);
            Handles.color = Color.black;
            Handles.DrawLine(points[1], points[0]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2f);
        }

        Handles.color = Color.red;
        for(int i = 0; i < path.NumPoints; i++)
        {
            Vector2 newPosition = Handles.FreeMoveHandle(path[i], Quaternion.identity, 0.1f, Vector2.zero, Handles.CylinderHandleCap);
            if (path[i] != newPosition)
            {
                Undo.RecordObject(creator, "MovePoint");
                path.MovePoint(i, newPosition);
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
        path = creator.path;
    }
}
