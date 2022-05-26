using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    public List<Vector3> points;
    [SerializeField, HideInInspector]
    private List<Quaternion> rotations;
    [SerializeField, HideInInspector]
    public List<float> Angles;
    [SerializeField, HideInInspector]
    private bool isClosed;
    [SerializeField, HideInInspector]
    private bool autoSetControlPoints;

    public Path(Vector3 centre)
    {
        // Create points for cubic bezier curve
        points = new List<Vector3>
        {
            centre + Vector3.left,
            centre + (Vector3.left + Vector3.forward) * 0.5f,
            centre + (Vector3.right + Vector3.back) * 0.5f,
            centre + (Vector3.right)
        };

        // Capture the default rotations for each anchor point
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
                    
                    // Auto set the first and last anchor points if enabled
                    if (autoSetControlPoints)
                    {
                        AutoSetAnchorControlPoints(0);
                        AutoSetAnchorControlPoints(points.Count - 3);
                    }
                }
                else
                {
                    // Same thing as removing first and last control points
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

    /// <summary>
    /// Adds a new segment at the end of the path 
    /// </summary>
    /// <param name="anchorPos">New anchor point position </param>
    public void AddSegement(Vector3 anchorPos)
    {
        // Add new points
        // P3 + (P3 - P2) => (P3 * 2) - P2
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);

        // Auto set the new points
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }

        // Add in rotation data for each point
        rotations.Add(Quaternion.identity);
        Angles.Add(0);
    }

    /// <summary>
    /// Adds a new segment in between two anchor points
    /// </summary>
    /// <param name="anchorPos">New anchor point position </param>
    /// <param name="segmentIndex">New intermediate segment index </param>
    public void SplitSegment(Vector3 anchorPosition, int segmentIndex)
    {
        // Insert new points
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPosition, Vector3.zero });
        
        // Auto set those points
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }

        // Add rotation data to those points
        rotations.Add(Quaternion.identity);
        Angles.Add(0);
    }

    /// <summary>
    /// Removes a segment
    /// </summary>
    /// <param name="anchorIndex">The anchor index used to delete a segment from </param>
    public void DeleteSegment(int anchorIndex)
    {
        // Must have more then 2 points to delete
        if(NumSegments > 2 || !isClosed && NumSegments > 1)
        {
            if (anchorIndex == 0)
            {
                if (isClosed)
                {
                    //Delete the first points but assign the next point as the last
                    points[points.Count - 1] = points[2];
                    points.RemoveRange(0, 3);
                }
                points.RemoveRange(0, 3);
            }
            // Remove last 3 points
            else if (anchorIndex == points.Count - 1 && !isClosed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }
        // Remove rotation data
        rotations.RemoveAt(anchorIndex / 3);
        Angles.RemoveAt(anchorIndex / 3);
    }

    /// <summary>
    /// Gets all points in a segment
    /// </summary>
    /// <param name="i">Starting index of the segment </param>
    public Vector3[] GetPointsInSegement(int i)
    {
        // Get the next 4 points given i
        return new Vector3[] 
        { 
            points[i * 3], 
            points[i * 3 + 1], 
            points[i * 3 + 2], 
            points[LoopIndex(i * 3 + 3)]
        };
    }

    /// <summary>
    /// Rotates a point
    /// </summary>
    /// <param name="anchorIndex">That anchor point index to rotate </param>
    /// <param name="rotation">New point rotation </param>
    public void RotatePoint(int anchorIndex, Quaternion rotation)
    {
        float angle = 0f;
        Vector3 angleAxis = Vector3.zero;
        // Get the angle compared to old and new rotation values
        (rotation * Quaternion.Inverse(rotations[anchorIndex])).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(Vector3.forward, angleAxis) > 90f)
        {
            angle = -angle;
        }
        // Save new rotation angle values
        rotations[anchorIndex] = rotation;
        Angles[anchorIndex] = angle;
    }

    /// <summary>
    /// Moves a point
    /// </summary>
    /// <param name="pointIndex">Point index </param>
    /// <param name="position">New point position </param>
    public void MovePoint(int pointIndex, Vector3 position)
    {
        // Change of movement
        Vector3 deltaMove = position - points[pointIndex];
        if (pointIndex % 3 == 0 || !autoSetControlPoints)
        {
            // Set the new anchor points position
            points[pointIndex] = position;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(pointIndex);
            }
            else
            {
                // Moving an anchor point
                if (pointIndex % 3 == 0)
                {
                    // Move points connected to anchor the same way
                    if (pointIndex + 1 < points.Count || isClosed)
                    {
                        points[LoopIndex(pointIndex + 1)] += deltaMove;
                    }
                    if (pointIndex - 1 > 0 || isClosed)
                    {
                        points[LoopIndex(pointIndex - 1)] += deltaMove;
                    }
                }
                else
                {
                    // Get the corresponding control index
                    bool isNextPointIsAnchor = (pointIndex + 1) % 3 == 0;
                    int correspondingControlIndex = isNextPointIsAnchor ? pointIndex + 2 : pointIndex - 2;
                    int anchorIndex = isNextPointIsAnchor ? pointIndex + 1 : pointIndex - 1;

                    // Move the corresponding control points tied to anchor alongst with it
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

    /// <summary>
    /// Get a list of evenly spaced points alongst the path
    /// </summary>
    /// <param name="spacing">The equal distance between each point </param>
    /// <param name="resolution">The accuracy of the newly spaced point </param>
    /// <returns> List of evenly spaced points </returns>
    public Vector3[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
    {
        List<Vector3> evenlySpacedPoints = new List<Vector3>();
        evenlySpacedPoints.Add(points[0]);
        Vector3 previousPoint = points[0];
        float distanceSinceLastEvenPoint = 0;

        for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
        {
            Vector3[] points = GetPointsInSegement(segmentIndex);
            // Get the length from point A to B around the curve
            float controlNetLength = Vector3.Distance(points[0], points[1]) + Vector3.Distance(points[1], points[2]) + Vector3.Distance(points[2], points[3]);
            // Using the estimate from before, calculate the estimated curves length 
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
                    // Add new points since theres a gap in the overshoot
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    distanceSinceLastEvenPoint = overshootDistance;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }

        return evenlySpacedPoints.ToArray();
    }

    /// <summary>
    /// Auto set the neighbouring points positions relative to the given anchor point
    /// </summary>
    /// <param name="updateAnchorIndex">The anchor point to base of its position </param>
    private void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
    {
        // Apply this calculation to the immediate points next to the given anchor point
        for (int i = updateAnchorIndex - 3; i <= updateAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || isClosed)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();
    }

    /// <summary>
    /// Auto set the all points in path
    /// </summary>
    private void AutoSetAllControlPoints()
    {
        for(int i = 0; i < points.Count; i += 3)
        {
            AutoSetAnchorControlPoints(i);
        }

        AutoSetStartAndEndControls();
    }

    /// <summary>
    /// Auto set the specified anchor point
    /// </summary>
    /// <param name="anchorIndex">The anchor point to auto set </param>
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

    /// <summary>
    /// Auto set the first and last points in the path
    /// </summary>
    private void AutoSetStartAndEndControls()
    {
        if (!isClosed)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
        }
    }

    /// <summary>
    /// Provide a wrapped index
    /// </summary>
    /// <param name="pointIndex">Given point index</param>
    /// <returns> Wrapped index </returns>
    private int LoopIndex(int pointIndex)
    {
        // Adding points.count to I such that if I is negative, this return is still valid
        return (pointIndex + points.Count) % points.Count;
    }
}
