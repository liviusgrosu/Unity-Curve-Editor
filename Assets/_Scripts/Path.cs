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
    [SerializeField, HideInInspector]
    bool autoSetControlPoints;

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

    public bool IsClosed
    {
        get
        {
            return isClosed;
        }
        set
        {
            if (isClosed != value)
            {
                isClosed = value;

                if (isClosed)
                {
                    // Adding the missing controls points for first and last anchor points
                    points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                    points.Add(points[0] * 2 - points[1]);

                    if (autoSetControlPoints)
                    {
                        AutoSetAnchorControlPoints(0);
                        AutoSetAnchorControlPoints(points.Count - 3);
                    }
                }
                else
                {
                    // Same thing as removing first and last control points. RemoveRange overlaps
                    points.RemoveRange(points.Count - 2, 2);
                    if (autoSetControlPoints)
                    {
                        AutoSetStartAndEndControls();
                    }
                }
            }
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

    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if (autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if (autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    public void AddSegement(Vector2 anchorPos)
    {
        // P3 + (P3 - P2) => (P3 * 2) - P2
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }
    }

    public void SplitSegment(Vector2 anchorPosition, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { Vector2.zero, anchorPosition, Vector2.zero });
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }
    }

    public void DeleteSegment(int anchorIndex)
    {
        if(NumSegments > 2 || !isClosed && NumSegments > 1)
        {
            if (anchorIndex == 0)
            {
                if (isClosed)
                {
                    points[points.Count - 1] = points[2];
                    points.RemoveRange(0, 3);
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !isClosed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }
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
        if (i % 3 == 0 || !autoSetControlPoints)
        {
            points[i] = position;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }
            else
            {
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
        }
    }

    private void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
    {
        for (int i = updateAnchorIndex - 3; i <= updateAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || isClosed)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();
    }

    private void AutoSetAllControlPoints()
    {
        for(int i = 0; i < points.Count; i += 3)
        {
            AutoSetAnchorControlPoints(i);
        }

        AutoSetStartAndEndControls();
    }

    private void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchorPosition = points[anchorIndex];
        Vector2 direction = Vector2.zero;
        float[] neighbourDistances = new float[2];

        if(anchorIndex - 3 >= 0 || isClosed)
        {
            // Get the vector from current anchor to the previous anchor point
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPosition;
            direction += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        if (anchorIndex + 3 >= 0 || isClosed)
        {
            // Get the vector from current anchor to the next anchor point
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPosition;
            direction -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        direction.Normalize();

        for(int i = 0; i < 2; i++)
        {
            // control index will either anchorIndex - 1 or anchorIndex + 1
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || isClosed)
            {
                points[LoopIndex(controlIndex)] = anchorPosition + direction * neighbourDistances[i] * 0.5f;
            }
        }
    }

    private void AutoSetStartAndEndControls()
    {
        if (!isClosed)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
        }
    }

    private int LoopIndex(int i)
    {
        // Adding points.count to I such that if I is negative, this return is still valid
        return (i + points.Count) % points.Count;
    }
}
