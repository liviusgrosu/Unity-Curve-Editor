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

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Create New"))
        {
            Undo.RecordObject(creator, "Create new");
            creator.CreatePath();
            path = creator.path;
        }

        bool isClosed = GUILayout.Toggle(path.IsClosed, "Toggle Closed Path");
        if (isClosed != path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            path.IsClosed = isClosed;
        }

        bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
        if (autoSetControlPoints != path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set controls");
            path.AutoSetControlPoints = autoSetControlPoints;
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
            Undo.RecordObject(creator, "AddSegment");
            path.AddSegement(mousePosition);
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
        {
            float minimumDistance = 0.05f;
            int closestAnchorIndex = -1;

            for (int i = 0; i < path.NumPoints; i += 3)
            {
                float distance = Vector2.Distance(mousePosition, path[i]);
                if (distance < minimumDistance)
                {
                    minimumDistance = distance;
                    closestAnchorIndex = i;
                }
            }

            if (closestAnchorIndex != -1)
            {
                Undo.RecordObject(creator, "Delete segment");
                path.DeleteSegment(closestAnchorIndex);
            }
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

        
        for(int i = 0; i < path.NumPoints; i++)
        {
            Handles.color = path.AutoSetControlPoints && i % 3 != 0 ? Color.gray : Color.red;
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
