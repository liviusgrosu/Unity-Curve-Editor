using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCreator : MonoBehaviour
{
    [HideInInspector]
    public Path path;

    public Color anchorColour = Color.red;
    public Color controlColour = Color.white;
    public Color segmentColour = Color.green;
    public Color selectedSegmentColour = Color.yellow;

    public float anchorDiameter = 0.1f;
    public float controlDiameter = 0.075f;
    public bool displayControlPoints = true;

    /// <summary>
    /// Creates a new point given the objects position
    /// </summary>
    public void CreatePath()
    {
        path = new Path(transform.position);
    }

    /// <summary>
    /// Resets the path by generating a new one
    /// </summary>
    private void Reset()
    {
        CreatePath();
    }
}
