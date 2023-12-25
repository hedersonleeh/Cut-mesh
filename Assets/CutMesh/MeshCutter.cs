using System.Collections.Generic;
using UnityEngine;
using static GeometryUtils;

public static class MeshCutter
{
    public static bool CutMesh(Plane planeCut, Mesh targetMesh, out (Mesh, Mesh) result, out List<Vector3> cutPoints)
    {
        var originalMesh = targetMesh;      

        var meshCutAbove = new Mesh();
        meshCutAbove.name = targetMesh.name + " " + "Mesh Above";
        var meshCutBelow = new Mesh();
        meshCutBelow.name = targetMesh.name + " " + "Mesh Below";

        //dectect which mesh belongs each vertex
        _vertexAbove = new List<Vector3>();
        _vertexBelow = new List<Vector3>();
        List<Vector3> totalCutPoints = new List<Vector3>();

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
                            totalCutPoints.Add(FindCutPoint(planeCut, v3, v2));
                        if (GetSlope(v3, v1) == 0)
                            totalCutPoints.Add(FindCutPoint(planeCut, v3, v1));
                        break;
                    case true when !v2IsAbove && v3IsAbove:
                    case false when v2IsAbove && !v3IsAbove:
                        if (GetSlope(v2, v3) == 0)
                            totalCutPoints.Add(FindCutPoint(planeCut, v2, v3));
                        if (GetSlope(v2, v1) == 0)
                            totalCutPoints.Add(FindCutPoint(planeCut, v2, v1));
                        break;
                    case true when !v2IsAbove && !v3IsAbove:
                    case false when v2IsAbove && v3IsAbove:
                        if (GetSlope(v1, v2) == 0)
                            totalCutPoints.Add(FindCutPoint(planeCut, v1, v2));
                        if (GetSlope(v1, v3) == 0)
                            totalCutPoints.Add(FindCutPoint(planeCut, v1, v3));
                        break;


                    default:
                        Debug.LogError("Caso no soportado revisar");

                        break;
                }
            }

        }

        result = (null, null);
        cutPoints = null;

        if (totalCutPoints.Count < 2) return false;

        cutPoints = new List<Vector3>() { totalCutPoints[0], totalCutPoints[^1] };

        #endregion

        #region Triangulation

        var upVertices = new List<Vector3>(_vertexAbove);
        upVertices.AddRange((cutPoints));

        var downVertices = new List<Vector3>(_vertexBelow);
        downVertices.AddRange((cutPoints));

        SortPolygonPoints(upVertices);
        SortPolygonPoints(downVertices);

        var upTriangleMesh = GetTrianglesFromPoints(upVertices);
        var downTriangleMesh = GetTrianglesFromPoints(downVertices);

        var upMesh = CreateMesh(upVertices, upTriangleMesh, originalMesh);
        var downMesh = CreateMesh(downVertices, downTriangleMesh, originalMesh);

        result = (upMesh, downMesh);

        return true;


        #endregion
    }

    private static bool VertexIsAbovePlane(Plane plane, Vector3 vertex)
    {
        var isAbove = plane.GetSide(vertex);
        return isAbove;

    }
    static List<Vector3> _vertexAbove;
    static List<Vector3> _vertexBelow;
    private static void AddToList(Vector3 vertex, bool above)
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

}
