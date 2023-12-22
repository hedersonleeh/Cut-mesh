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
            Vector3.zero,Vector3.right,Vector3.up,new Vector3(1,1)
        };
        mesh.triangles = new int[]
        {
           0,2,1,
           1,2,3
        };
        mesh.normals = new Vector3[]
        {
            Vector3.back,Vector3.back,Vector3.back,Vector3.back,
        };
        mesh.tangents = new Vector4[] {
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
            new Vector4(1f, 0f, 0f, -1f),
        };

        mesh.uv = new Vector2[]
        {
            Vector2.zero,Vector2.right,Vector2.up,Vector2.one

        };
        m_filter = GetComponent<MeshFilter>();
        m_filter.mesh = mesh;
    }
    [ContextMenu("Test cut")]
    private void Cut()
    {
        var originalMesh = m_filter.mesh;
        var y = originalMesh.bounds.size.y;
        var halfToCut = y / 2;
        var planeCut = new Plane(Vector3.up, -halfToCut);
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
                        cutPoints.Add(FindCutPoint(planeCut, v3, v2));
                        cutPoints.Add(FindCutPoint(planeCut, v3, v1));
                        break;
                    case false when v2IsAbove && !v3IsAbove:
                        cutPoints.Add(FindCutPoint(planeCut, v2, v3));
                        cutPoints.Add(FindCutPoint(planeCut, v2, v1));
                        break;
                    default:
                        cutPoints.Add(FindCutPoint(planeCut, v1, v2));
                        cutPoints.Add(FindCutPoint(planeCut, v1, v3));
                        break;
                }
            }
            
        }
        Vector3 minPoint = Vector3.positiveInfinity;
        Vector3 maxPoint = Vector3.negativeInfinity;

        foreach (var cP in cutPoints)
        {
            minPoint = Vector3.Min(minPoint,cP);
            maxPoint = Vector3.Max(maxPoint, cP);
        }
        _cutPoints = new Vector3[2];
        _cutPoints[0] = minPoint;
        _cutPoints[1] = maxPoint;
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

    private Vector3 FindCutPoint(Plane plane, Vector3 A, Vector3 B)
    {
        var closetPoint = plane.ClosestPointOnPlane(A);

        if ((B.x - A.x) != 0) //check there is an slope
        {
            var slope = B.y - A.y / (B.x - A.x);
            var x = (closetPoint.y-A.y ) / slope ;
            var y = closetPoint.y;
            return new Vector3(x, y);
        }
        return closetPoint;
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
        Gizmos.color = Color.blue;
        if (m_filter && m_filter.mesh)
            Gizmos.DrawLine(Vector3.up * m_filter.mesh.bounds.size.y / 2, Vector3.up * (m_filter.mesh.bounds.size.y / 2) + Vector3.right * 10);

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
