using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSkew : MonoBehaviour
{
    public Transform AStart, AEnd;
    public Transform BStart, BEnd;
    public Transform ClosestPointA, ClosestPointB;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 skewA = AEnd.position - AStart.position;
        Vector3 skewB = BEnd.position - BStart.position;

        Vector3 samplePointA = AStart.position + skewA * 0.1f;
        Vector3 samplePointB = BStart.position + skewB * 0.9f;

        Vector3 closestPointA, closestPointB;

        Debug.DrawRay(AStart.position, skewA, Color.green);
        Debug.DrawRay(BStart.position, skewB, Color.red);

        ClosestPointsOnTwoLines(out closestPointA, out closestPointB, samplePointA, skewA , samplePointB, skewB );

        ClosestPointA.position = closestPointA;
        ClosestPointB.position = closestPointB;

        Debug.DrawLine(ClosestPointA.position, ClosestPointB.position, Color.cyan);
        Debug.Break();
    }

    public bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {
            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        return false;
    }
}
