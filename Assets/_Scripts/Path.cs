using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    List<Vector2> points;
    [SerializeField, HideInInspector]
    bool isClosed;

    public Path(Vector2 centre)
    {
        points = new List<Vector2>
        {
            centre + Vector2.left,
            centre + (Vector2.left + Vector2.up) * 0.5f, 
            centre + (Vector2.right + Vector2.down) * 0.5f,
            centre + (Vector2.right)
        };
    }

    public Vector2 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public int NumPoints 
    { 
        get 
        { 
            return points.Count; 
        } 
    }

    public int NumSegments
    {
        get
        {
            return points.Count / 3;
        }
    }

    public void AddSegement(Vector2 anchorPos)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);
    }

    public Vector2[] GetPointsInSegement(int i)
    {
        return new Vector2[] 
        { 
            points[i * 3], 
            points[i * 3 + 1], 
            points[i * 3 + 2], 
            points[LoopIndex(i * 3 + 3)]
        };
    }

    public void MovePoint(int i, Vector2 position)
    {
        Vector2 deltaMove = position - points[i];
        points[i] = position;

        // Moving an anchor point
        if (i % 3 == 0)
        {
            // Move points connected to anchor the same way
            if (i + 1 < points.Count || isClosed)
            {
                points[LoopIndex(i + 1)] += deltaMove;
            }
            if (i - 1 > 0 || isClosed)
            {
                points[LoopIndex(i - 1)] += deltaMove;
            }
        }
        else
        {
            bool isNextPointIsAnchor = (i + 1) % 3 == 0;
            int correspondingControlIndex = isNextPointIsAnchor ? i + 2 : i - 2;
            int anchorIndex = isNextPointIsAnchor ? i + 1 : i - 1;

            if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || isClosed)
            {
                float distance = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
                Vector2 direction = (points[LoopIndex(anchorIndex)] - position).normalized;
                points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + direction * distance;
            }
        }
    }
    
    public void ToggleClosed()
    {
        isClosed = !isClosed;

        if (isClosed)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add(points[0] * 2 - points[1]);
        }
        else
        {
            points.RemoveRange(points.Count - 2, 2);
        }
    }

    private int LoopIndex(int i)
    {
        return (i + points.Count) % points.Count;
    }
}
