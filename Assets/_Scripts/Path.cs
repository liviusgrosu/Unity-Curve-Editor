using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    public List<Vector3> points;
    [SerializeField, HideInInspector]
    List<Quaternion> rotations;
    [SerializeField, HideInInspector]
    public List<float> Angles;
    [SerializeField, HideInInspector]
    bool isClosed;
    [SerializeField, HideInInspector]
    bool autoSetControlPoints;

    public Path(Vector3 centre)
    {
        points = new List<Vector3>
        {
            centre + Vector3.left,
            centre + (Vector3.left + Vector3.forward) * 0.5f,
            centre + (Vector3.right + Vector3.back) * 0.5f,
            centre + (Vector3.right)
        };

        rotations = new List<Quaternion>
        {
            Quaternion.identity,
            Quaternion.identity
        };

        Angles = new List<float>
        {
            0f,
            0f
        };
    }

    public Vector3 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public List<Quaternion> Rotations
    {
        get
        {
            return rotations;
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

    public void AddSegement(Vector3 anchorPos)
    {
        // P3 + (P3 - P2) => (P3 * 2) - P2
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }

        rotations.Add(Quaternion.identity);
        Angles.Add(0);
    }

    public void SplitSegment(Vector3 anchorPosition, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPosition, Vector3.zero });
        
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }

        rotations.Add(Quaternion.identity);
        Angles.Add(0);
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
        rotations.RemoveAt(anchorIndex);
        Angles.RemoveAt(anchorIndex);
    }

    public Vector3[] GetPointsInSegement(int i)
    {
        return new Vector3[] 
        { 
            points[i * 3], 
            points[i * 3 + 1], 
            points[i * 3 + 2], 
            points[LoopIndex(i * 3 + 3)]
        };
    }

    public void RotatePoint(int i, Quaternion rotation)
    {

        float angle = 0f;

        Vector3 angleAxis = Vector3.zero;
        (rotation * Quaternion.Inverse(rotations[i])).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(Vector3.forward, angleAxis) > 90f)
        {
            angle = -angle;
        }
        rotations[i] = rotation;
        Angles[i] = angle;
    }

    public void MovePoint(int i, Vector3 position)
    {
        Vector3 deltaMove = position - points[i];
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
                        Vector3 direction = (points[LoopIndex(anchorIndex)] - position).normalized;
                        points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + direction * distance;
                    }
                }
            }
        }
    }

    public Vector3[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
    {
        List<Vector3> evenlySpacedPoints = new List<Vector3>();
        evenlySpacedPoints.Add(points[0]);
        Vector3 previousPoint = points[0];
        float distanceSinceLastEvenPoint = 0;

        for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
        {
            Vector3[] points = GetPointsInSegement(segmentIndex);
            float controlNetLength = Vector3.Distance(points[0], points[1]) + Vector3.Distance(points[1], points[2]) + Vector3.Distance(points[2], points[3]);
            float estimatedCurveLength = Vector3.Distance(points[0], points[3]) + controlNetLength / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10f);

            float t = 0;
            while(t <= 1)
            {
                t += 1f / divisions;
                // Get the point on the curve
                Vector3 pointOnCurve = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], t);
                // Get distance from the last point
                distanceSinceLastEvenPoint += Vector3.Distance(previousPoint, pointOnCurve);

                while (distanceSinceLastEvenPoint >= spacing)
                {
                    // If overshot the distance to the next point, take the new point back by how much it overshot
                    float overshootDistance = distanceSinceLastEvenPoint - spacing;
                    Vector3 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDistance;
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    distanceSinceLastEvenPoint = overshootDistance;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }

        return evenlySpacedPoints.ToArray();
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
        Vector3 anchorPosition = points[anchorIndex];
        Vector3 direction = Vector3.zero;
        float[] neighbourDistances = new float[2];

        if(anchorIndex - 3 >= 0 || isClosed)
        {
            // Get the vector from current anchor to the previous anchor point
            Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPosition;
            direction += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        if (anchorIndex + 3 >= 0 || isClosed)
        {
            // Get the vector from current anchor to the next anchor point
            Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPosition;
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
