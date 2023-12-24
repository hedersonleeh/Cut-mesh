using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    MeshFilter m_filter;
    private void OnEnable()
    {
        var mesh = new Mesh();
        mesh.name = "Triangle Mesh";

        mesh.vertices = new Vector3[]
        {
            Vector3.zero,Vector3.right,Vector3.up,new Vector3(1,1),Vector3.right*2f,new Vector3(2,1)
        };
        mesh.triangles = new int[]
        {
           0,2,1,
           1,2,3,
           1,3,4,
           3,5,4
        };
        mesh.normals = new Vector3[]
        {
            Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.back
        };
        mesh.tangents = new Vector4[] {
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
        };

        mesh.uv = new Vector2[]
        {
            Vector2.zero,Vector2.right/2,Vector2.up,new Vector2(0.5f,1),Vector2.right,Vector3.one

        };
        m_filter = GetComponent<MeshFilter>();
        m_filter.mesh = mesh;
    }
    Plane planeCut;

    [ContextMenu("Test cut")]
    private void Cut()
    {
        var originalMesh = m_filter.mesh;
        var y = originalMesh.bounds.size.y;
        var halfToCut = y / 2;
        planeCut = new Plane(Vector3.up + Vector3.left, halfToCut);
        var meshCutAbove = new Mesh();
        meshCutAbove.name = "Triangle Above";
        var meshCutBelow = new Mesh();
        meshCutBelow.name = "Triangle Below";

        //dectect which mesh belongs each vertex
        _vertexAbove = new List<Vector3>();
        _vertexBelow = new List<Vector3>();
        List<Vector3> cutPoints = new List<Vector3>();

        for (int i = 0; i < originalMesh.triangles.Length; i += 3)
        {
            Vector3 v1 = originalMesh.vertices[originalMesh.triangles[i % originalMesh.triangles.Length]];
            Vector3 v2 = originalMesh.vertices[originalMesh.triangles[(i + 1) % originalMesh.triangles.Length]];
            Vector3 v3 = originalMesh.vertices[originalMesh.triangles[(i + 2) % originalMesh.triangles.Length]];

            var v1IsAbove = VertexIsAbovePlane(planeCut, v1);
            var v2IsAbove = VertexIsAbovePlane(planeCut, v2);
            var v3IsAbove = VertexIsAbovePlane(planeCut, v3);

            AddToList(v1, v1IsAbove);
            AddToList(v2, v2IsAbove);
            AddToList(v3, v3IsAbove);
            if ((v1IsAbove && v2IsAbove && v3IsAbove) || //all above
                ((!v1IsAbove && !v2IsAbove && !v3IsAbove))) //none above
            {
                continue; //no one cuts the plane
            }
            else
            {
                switch (v1IsAbove)
                {
                    case true when v2IsAbove && !v3IsAbove:
                    case false when !v2IsAbove && v3IsAbove:
                        if (GetSlope(v3, v2) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v3, v2));
                        if (GetSlope(v3, v1) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v3, v1));
                        break;
                    case true when !v2IsAbove && v3IsAbove:
                    case false when v2IsAbove && !v3IsAbove:
                        if (GetSlope(v2, v3) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v2, v3));
                        if (GetSlope(v2, v1) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v2, v1));
                        break;
                    case true when !v2IsAbove && !v3IsAbove:
                    case false when v2IsAbove && v3IsAbove:
                        if (GetSlope(v1, v2) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v1, v2));
                        if (GetSlope(v1, v3) == 0)
                            cutPoints.Add(FindCutPoint(planeCut, v1, v3));
                        break;


                    default:
                        Debug.LogError("Caso no soportado revisar");

                        break;
                }
            }

        }
        Vector3 minPoint = Vector3.positiveInfinity;
        Vector3 maxPoint = Vector3.negativeInfinity;

        foreach (var cP in cutPoints)
        {
            minPoint = Vector3.Min(minPoint, cP);
            maxPoint = Vector3.Max(maxPoint, cP);
        }
        _cutPoints = new Vector3[cutPoints.Count];
        for (int i = 0; i < _cutPoints.Length; i++)
        {
            _cutPoints[i] = cutPoints[i];
        }
        //_cutPoints[0] = minPoint;
        //_cutPoints[1] = maxPoint;
    }

    private void AddToList(Vector3 vertex, bool above)
    {
        if (above)
        {
            if (!_vertexAbove.Contains(vertex))
                _vertexAbove.Add(vertex);
        }
        else
        {
            if (!_vertexBelow.Contains(vertex))
                _vertexBelow.Add(vertex);
        }
    }
    private float GetSlope(Vector3 a, Vector3 b)
    {
        if (b.x - a.x == 0) return 0;
        return (b.y - a.y / (b.x - a.x));
    }
    private Vector3 GetIntersection(Vector3 line1, Vector3 line2)
    {
        return Vector3.Cross(line1, line2);
    }
    private Vector3 GetLineOnGeneralForm(Vector3 P1, Vector3 P2)
    {
        return new Vector3(P2.y - P1.y,
                                      P1.x - P2.x,
                                      -(P1.x * P2.y) - (P2.x * P1.y));
    }
    private Vector3 FindCutPoint(Plane plane, Vector3 P1, Vector3 P2)
    {
        var lineA = GetLineOnGeneralForm(P1, P2);
        Vector3 P3 = plane.ClosestPointOnPlane(P1);
        Vector3 P4 = plane.ClosestPointOnPlane(P2);
        var lineB = GetLineOnGeneralForm(P3, P4);
        var intersectionPoint = GetIntersection(lineA, lineB);
        var result = GetIntersection(P1, P2, P3, P4);
        return result;


    }
    private Vector3 GetIntersection(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
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
    private bool VertexIsAbovePlane(Plane plane, Vector3 vertex)
    {
        var isAbove = plane.GetSide(vertex);
        Debug.DrawLine(plane.ClosestPointOnPlane(vertex), vertex, isAbove ? Color.green : Color.red, 100);
        return isAbove;

    }
    List<Vector3> _vertexAbove = new List<Vector3>();
    List<Vector3> _vertexBelow = new List<Vector3>();
    Vector3[] _cutPoints = new Vector3[2];

    private void OnDrawGizmos()
    {
        var rightLine = Quaternion.Euler(0, 0, 90) * planeCut.normal;
        var offset = planeCut.normal * planeCut.distance;
        Gizmos.DrawLine(-100 * rightLine - offset, 100 * rightLine - offset);
        if (_vertexAbove != null)
            foreach (var v in _vertexAbove)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(v, .1f);
            }
        if (_vertexBelow != null)
            foreach (var v in _vertexBelow)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(v, .1f);
            }
        if (_cutPoints != null)
            foreach (var v in _cutPoints)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(v, .1f);
            }
    }

}
