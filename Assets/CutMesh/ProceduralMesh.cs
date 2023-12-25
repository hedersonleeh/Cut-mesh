using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GeometryUtils;
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
        planeCut = new Plane(Vector3.up, -halfToCut);
        var meshCutAbove = new Mesh();
        meshCutAbove.name = "Triangle Above";
        var meshCutBelow = new Mesh();
        meshCutBelow.name = "Triangle Below";

        //dectect which mesh belongs each vertex
        _vertexAbove = new List<Vector3>();
        _vertexBelow = new List<Vector3>();
        List<Vector3> cutPoints = new List<Vector3>();
        #region Mesh Cut
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
        if (cutPoints.Count < 2) return;
        _cutPoints = new List<Vector3>() { cutPoints[0], cutPoints[^1] };

        #endregion

        #region Triangulation

        var upVertices = new List<Vector3>(_vertexAbove);
        upVertices.AddRange((_cutPoints));

        var downVertices = new List<Vector3>(_vertexBelow);
        downVertices.AddRange((_cutPoints));

        SortPolygonPoints(upVertices);
        SortPolygonPoints(downVertices);

        var upTriangleMesh = GetTrianglesFromPoints(upVertices);
        var downTriangleMesh = GetTrianglesFromPoints(downVertices);

        var upMesh = CreateMesh(upVertices, upTriangleMesh);
        var downMesh = CreateMesh(downVertices, downTriangleMesh);

        var A = new GameObject("A");
        var B = new GameObject("B");

        BoxCollider boxA = CreateCutObj(upMesh, A);
        BoxCollider boxB = CreateCutObj(downMesh, B);

      
   

        A.transform.position = transform.position;
        B.transform.position = transform.position;
        #endregion
    }

    private BoxCollider CreateCutObj(Mesh upMesh, GameObject A)
    {
        A.AddComponent<MeshFilter>().mesh = upMesh;
        A.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        A.AddComponent<Rigidbody>().AddForce(planeCut.normal * .1f, ForceMode.Impulse);
        var box = A.AddComponent<BoxCollider>();
        box.size = upMesh.bounds.size + Vector3.forward * .1f;
        return box;
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

    private bool VertexIsAbovePlane(Plane plane, Vector3 vertex)
    {
        var isAbove = plane.GetSide(vertex);
        Debug.DrawLine(plane.ClosestPointOnPlane(vertex), vertex, isAbove ? Color.green : Color.red, 100);
        return isAbove;

    }
    List<Vector3> _vertexAbove = new List<Vector3>();
    List<Vector3> _vertexBelow = new List<Vector3>();
    List<Vector3> _cutPoints = new List<Vector3>();

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
