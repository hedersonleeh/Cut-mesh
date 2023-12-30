using System;
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

    static List<Vector2> _upUVs;
    static List<Vector2> _downUVs;

    static List<Vector3> _cutPoints;
    public static bool CutMesh(Plane planeCut, Transform transform, Mesh targetMesh, out (Mesh, Mesh, List<Vector3>) result)
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
        _cutPoints = new List<Vector3>();
        _upUVs = new List<Vector2>();
        _downUVs = new List<Vector2>();
        List<Vector3> totalCutPoints = new List<Vector3>();



        for (int i = 0; i < originalMesh.triangles.Length; i += 3)
        {
            Vector3 v1 = originalMesh.vertices[originalMesh.triangles[i]];
            Vector3 v2 = originalMesh.vertices[originalMesh.triangles[i + 1]];
            Vector3 v3 = originalMesh.vertices[originalMesh.triangles[i + 2]];

            var indexUv1 = Array.IndexOf(originalMesh.vertices, v1);
            var indexUv2 = Array.IndexOf(originalMesh.vertices, v2);
            var indexUv3 = Array.IndexOf(originalMesh.vertices, v3);

            v1 = transform.TransformPoint(v1);
            v2 = transform.TransformPoint(v2);
            v3 = transform.TransformPoint(v3);

            var uv1 = originalMesh.uv[indexUv1];
            var uv2 = originalMesh.uv[indexUv2];
            var uv3 = originalMesh.uv[indexUv3];

            var v1IsAbove = VertexIsAbovePlane(planeCut, v1);
            var v2IsAbove = VertexIsAbovePlane(planeCut, v2);
            var v3IsAbove = VertexIsAbovePlane(planeCut, v3);


            int idxV1 = AddToVertexList(v1, uv1, v1IsAbove);
            int idxV2 = AddToVertexList(v2, uv2, v2IsAbove);
            int idxV3 = AddToVertexList(v3, uv3, v3IsAbove);





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

                    cut1 = CutAndAddToTriangelist(planeCut, v2, uv2, v3, uv3);
                    cut2 = CutAndAddToTriangelist(planeCut, v3, uv3, v1, uv1);

                    aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                    sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                    AddToTriangleList(idxV3, aloneCuts.Item2, aloneCuts.Item1, !side);

                    AddToTriangleList(idxV1, idxV2, sameSideCuts.Item2, side);

                    AddToTriangleList(idxV2, sameSideCuts.Item1, sameSideCuts.Item2, side);



                }
                else
                {
                    if (v1IsAbove == v3IsAbove)
                    {
                        side = v1IsAbove;

                        cut1 = CutAndAddToTriangelist(planeCut, v1, uv1, v2, uv2);
                        cut2 = CutAndAddToTriangelist(planeCut, v2, uv2, v3, uv3);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV2, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV1, sameSideCuts.Item1, idxV3, side);

                        AddToTriangleList(idxV3, sameSideCuts.Item1, sameSideCuts.Item2, side);



                    }
                    else
                    {
                        side = !v1IsAbove;

                        cut1 = CutAndAddToTriangelist(planeCut, v3, uv3, v1, uv1);
                        cut2 = CutAndAddToTriangelist(planeCut, v1, uv1, v2, uv2);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV1, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV3, sameSideCuts.Item1, idxV2, side);

                        AddToTriangleList(idxV2, sameSideCuts.Item1, sameSideCuts.Item2, side);



                    }

                }


                if (!_cutPoints.Contains(cut1))
                    _cutPoints.Add(cut1);
                if (!_cutPoints.Contains(cut2))
                    _cutPoints.Add(cut2);

            }

        }

        result = (null, null, null);

        if (_cutPoints.Count < 2) return false;
        Vector3 centroid = Vector3.zero;

        _cutPoints.ForEach(p => centroid += p);
        centroid /= _cutPoints.Count;

        var coverATriangle = new List<int>();
        var coverBTriangle = new List<int>();


        var firtsPoints = _cutPoints[0] - centroid;
        _cutPoints = _cutPoints.OrderBy(p => Vector3.SignedAngle(firtsPoints.normalized, (p - centroid).normalized, planeCut.normal)).ToList();

        _verticesAbove.Add(centroid);
        _verticesBelow.Add(centroid);
        _upUVs.Add(Vector2.zero);
        _downUVs.Add(Vector2.zero);

        for (int i = 0; i < _cutPoints.Count; i++)
        {
            var point1 = _cutPoints[i];
            var point2 = _cutPoints[(i + 1) % _cutPoints.Count];
            var point3 = centroid;

            Debug.DrawLine(point1, point2, Color.red, 100);

            var triangleA = new int[] { _verticesAbove.IndexOf(point2), _verticesAbove.IndexOf(point1), _verticesAbove.IndexOf(centroid) };
            var triangleB = new int[] { _verticesBelow.IndexOf(point1), _verticesBelow.IndexOf(point2), _verticesBelow.IndexOf(centroid) };

            coverATriangle.AddRange(triangleA);
            coverBTriangle.AddRange(triangleB);


        }


        _trianglesAbove.AddRange(coverATriangle);
        _trianglesBelow.AddRange(coverBTriangle);

        var upMesh = CreateMesh(_verticesAbove, _trianglesAbove, _upUVs, originalMesh, transform);
        var downMesh = CreateMesh(_verticesBelow, _trianglesBelow, _downUVs, originalMesh, transform);

        result = (upMesh, downMesh, _cutPoints);

        return true;


    }

    private static Vector3 CutAndAddToTriangelist(Plane planeCut, Vector3 from, Vector2 uvFrom, Vector3 to, Vector2 uvTo)
    {
        var cut = GetCutPoint(planeCut, from, to, out var distance);
        var interpolatedUV = Vector2.Lerp(uvFrom, uvTo, InverseLerp(from, to, cut));
        AddToVertexList(cut, interpolatedUV, true);
        AddToVertexList(cut, interpolatedUV, false);
        return cut;

    }
    public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
    {
        Vector3 AB = b - a;
        Vector3 AV = value - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }

    private static bool VertexIsAbovePlane(Plane plane, Vector3 vertex)
    {
        var isAbove = plane.GetSide(vertex);
        return isAbove;
    }

    private static int AddToVertexList(Vector3 vertex, Vector2 uv, bool above)
    {
        if (above)
        {
            if (_verticesAbove.Contains(vertex))
            {
                return _verticesAbove.IndexOf(vertex);
            }
            _verticesAbove.Add(vertex);
            _upUVs.Add(uv);
            return _verticesAbove.Count - 1;
        }
        else
        {
            if (_verticesBelow.Contains(vertex))
            {
                return _verticesBelow.IndexOf(vertex);
            }

            _verticesBelow.Add(vertex);
            _downUVs.Add(uv);

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
    //private static void AddToUVList(bool above, params Vector2[] uvs)
    //{
    //    if (above)
    //    {
    //        foreach (var uv in uvs)
    //        {
    //            //if (!_upUVs.Contains(uv))
    //                _upUVs.Add(uv);
    //        }
    //    }
    //    else
    //    {
    //        foreach (var uv in uvs)
    //        {
    //            //if (!_downUVs.Contains(uv))
    //                _downUVs.Add(uv);
    //        }
    //    }
    //}
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
