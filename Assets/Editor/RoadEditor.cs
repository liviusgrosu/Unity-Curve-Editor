using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadCreator))]
public class RoadEditor : Editor
{
    RoadCreator creator;

    private void OnSceneGUI()
    {
        // Whenever a GUI element has occured, update the road
        if (creator.AutoUpdate && Event.current.type == EventType.Repaint)
        {
            creator.UpdateRoad();
        }
    }

    private void OnEnable()
    {
        creator = (RoadCreator)target;
    }
}
