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
