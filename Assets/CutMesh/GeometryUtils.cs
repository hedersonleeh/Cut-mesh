using UnityEngine;

public static class GeometryUtils
{
    public static float GetSlope(Vector3 a, Vector3 b)
    {
        if (b.x - a.x == 0) return 0;
        return (b.y - a.y / (b.x - a.x));
    }
    public static Vector3 GetIntersection(Vector3 line1, Vector3 line2)
    {
        return Vector3.Cross(line1, line2);
    }
    public static Vector3 GetLineOnGeneralForm(Vector3 P1, Vector3 P2)
    {
        return new Vector3(P2.y - P1.y,
                                      P1.x - P2.x,
                                      -(P1.x * P2.y) - (P2.x * P1.y));
    }
    public static Vector3 FindCutPoint(Plane plane, Vector3 P1, Vector3 P2)
    {
        Vector3 P3 = plane.ClosestPointOnPlane(P1);
        Vector3 P4 = plane.ClosestPointOnPlane(P2);
        var result = GetIntersection(P1, P2, P3, P4);
        return result;


    }
    public static Vector3 GetIntersection(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        // Line AB represented as a1x + b1y = c1 
        float a1 = B.y - A.y;
        float b1 = A.x - B.x;
        float c1 = a1 * (A.x) + b1 * (A.y);

        // Line CD represented as a2x + b2y = c2 
        float a2 = D.y - C.y;
        float b2 = C.x - D.x;
        float c2 = a2 * (C.x) + b2 * (C.y);

        float determinant = a1 * b2 - a2 * b1;

        if (determinant == 0)
        {
            // The lines are parallel. This is simplified 
            // by returning a pair of FLT_MAX 
            return C;
        }
        else
        {
            float x = (b2 * c1 - b1 * c2) / determinant;
            float y = (a1 * c2 - a2 * c1) / determinant;
            return new Vector3(x, y);
        }
    }
}
