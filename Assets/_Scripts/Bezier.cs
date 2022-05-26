using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier
{
    /// <summary>
    /// Get point on point on quadratic curve given a time interval
    /// </summary>
    /// <param name="a">first point </param>
    /// <param name="b">second point </param>
    /// <param name="c">third point </param>
    /// <param name="t">time interval </param>
    /// <returns> point on the quadratic curve </returns>
    public static Vector3 EvaluateQuadratic(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 p0 = Vector3.Lerp(a, b, t);
        Vector3 p1 = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(p0, p1, t);
    }

    /// <summary>
    /// Get point on point on cubic curve given a time interval
    /// </summary>
    /// <param name="a">first point </param>
    /// <param name="b">second point </param>
    /// <param name="c">third point </param>
    /// <param name="d">fourth point </param>
    /// <param name="t">time interval </param>
    /// <returns> point on the cubic curve </returns>
    public static Vector3 EvaluateCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        // Get point on point on cubic curve given a time interval
        Vector3 p0 = EvaluateQuadratic(a, b, c, t);
        Vector3 p1 = EvaluateQuadratic(b, c, d, t);
        return Vector3.Lerp(p0, p1, t);
    }
}
