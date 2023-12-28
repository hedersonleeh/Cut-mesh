using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GeometryUtils;

public static class MeshCutter
{
    static List<Vector3> _verticesAbove;
    static List<Vector3> _verticesBelow;
    static List<int> _trianglesAbove;
    static List<int> _trianglesBelow;
    public static bool CutMesh(Plane planeCut, Transform transform, Mesh targetMesh, out (Mesh, Mesh) result, out List<Vector3> cutPoints)
    {
        var originalMesh = targetMesh;
        var meshCutAbove = new Mesh();
        meshCutAbove.name = targetMesh.name + " " + "Mesh Above";
        var meshCutBelow = new Mesh();
        meshCutBelow.name = targetMesh.name + " " + "Mesh Below";

        //dectect which mesh belongs each vertex
        _verticesAbove = new List<Vector3>();
        _verticesBelow = new List<Vector3>();
        _trianglesAbove = new List<int>();
        _trianglesBelow = new List<int>();
        List<Vector3> totalCutPoints = new List<Vector3>();



        for (int i = 0; i < originalMesh.triangles.Length; i += 3)
            {
            Vector3 v1 = originalMesh.vertices[originalMesh.triangles[i]];
            Vector3 v2 = originalMesh.vertices[originalMesh.triangles[i + 1]];
            Vector3 v3 = originalMesh.vertices[originalMesh.triangles[i + 2]];

            v1 = transform.TransformPoint(v1);
            v2 = transform.TransformPoint(v2);
            v3 = transform.TransformPoint(v3);

            var v1IsAbove = VertexIsAbovePlane(planeCut, v1);
            var v2IsAbove = VertexIsAbovePlane(planeCut, v2);
            var v3IsAbove = VertexIsAbovePlane(planeCut, v3);

            int idxV1 = AddToVertexList(v1, v1IsAbove);
            int idxV2 = AddToVertexList(v2, v2IsAbove);
            int idxV3 = AddToVertexList(v3, v3IsAbove);



            if ((v1IsAbove && v2IsAbove && v3IsAbove) || //all above
                ((!v1IsAbove && !v2IsAbove && !v3IsAbove))) //none above
            {
                AddToTriangleList(idxV1, idxV2, idxV3, v1IsAbove);
                continue; //no one get cut the plane
            }
            else
            {
                Vector3 cut1, cut2;
                bool side;
                (int, int) aloneCuts;
                (int, int) sameSideCuts;
                if (v1IsAbove == v2IsAbove) //same side;
                {
                    side = v1IsAbove;

                    CutAndAddToTriangelist(planeCut, v3, v1, v2, out cut1, out cut2);

                    aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                    sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                    AddToTriangleList(idxV3, aloneCuts.Item2, aloneCuts.Item1, !side);

                    AddToTriangleList(idxV1, sameSideCuts.Item1, idxV2, side);
                    AddToTriangleList(idxV1,sameSideCuts.Item1, sameSideCuts.Item2, side);


                }
                else
                {
                    if (v1IsAbove == v3IsAbove)
                    {
                        side = v1IsAbove;

                        CutAndAddToTriangelist(planeCut, v2, v1, v3, out cut1, out cut2);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV2, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV1, sameSideCuts.Item1, idxV3, side);
                        AddToTriangleList(idxV3, sameSideCuts.Item1, sameSideCuts.Item2, side);
                    }
                    else
                    {
                        side = !v1IsAbove;

                        CutAndAddToTriangelist(planeCut, v1, v2, v3, out cut1, out cut2);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV1, aloneCuts.Item1, aloneCuts.Item2, !side);

                        AddToTriangleList(idxV3, sameSideCuts.Item1,  idxV2, side);
                        AddToTriangleList(idxV3,sameSideCuts.Item2, sameSideCuts.Item1, side);
                    }
                }

            }

        }

        result = (null, null);
        cutPoints = null;


        //SortCutPoints(totalCutPoints);
        cutPoints = new List<Vector3>(totalCutPoints);


        var upMesh = CreateMesh(_verticesAbove, _trianglesAbove, originalMesh, transform);
        var downMesh = CreateMesh(_verticesBelow, _trianglesBelow, originalMesh, transform);

        result = (upMesh, downMesh);

        return true;


    }

    private static void CutAndAddToTriangelist(Plane planeCut, Vector3 alone, Vector3 firts, Vector3 second, out Vector3 cut1, out Vector3 cut2)
    {
        cut1 = GetCutPoint(planeCut, alone, firts);
        cut2 = GetCutPoint(planeCut, alone, second);

        AddToVertexList(cut1, true);
        AddToVertexList(cut1, false);

        AddToVertexList(cut2, true);
        AddToVertexList(cut2, false);

    }

    private static void SortCutPoints(List<Vector3> totalCutPoints)
    {
        totalCutPoints.Sort(CompareCutpoints);

        int CompareCutpoints(Vector3 A, Vector3 B)
        {
            if (GetSlope(A, B) == 0)
            {
                return A.x < B.x ? -1 : 1;
            }
            else
            {
                return A.y < B.y ? -1 : 1;
            }

        }
    }

    private static bool VertexIsAbovePlane(Plane plane, Vector3 vertex)
    {
        var isAbove = plane.GetSide(vertex);
        return isAbove;

    }

    private static int AddToVertexList(Vector3 vertex, bool above)
    {
        if (above)
        {
            _verticesAbove.Add(vertex);
            return _verticesAbove.Count - 1;

        }
        else
        {

            _verticesBelow.Add(vertex);
            return _verticesBelow.Count - 1;

        }
    }
    private static void AddToTriangleList(int v1, int v2, int v3, bool above)
    {
        if (above)
        {
            _trianglesAbove.Add(v1);
            _trianglesAbove.Add(v2);
            _trianglesAbove.Add(v3);

        }
        else
        {

            _trianglesBelow.Add(v1);
            _trianglesBelow.Add(v2);
            _trianglesBelow.Add(v3);

        }
    }
    private static int GetCutIndex(Vector3 cut, bool above)
    {
        if (above)
        {
            return _verticesAbove.IndexOf(cut);
        }
        else
        {
            return _verticesBelow.IndexOf(cut);

        }
    }

}
