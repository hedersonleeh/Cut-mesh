using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GeometryUtils;

public static class MeshCutter
{
    private struct CutPoint
    {
        public Vector3 point;
        public int indexAbove;
        public int indexBelow;

        public CutPoint(Vector3 point, int indexAbove, int indexBelow)
        {
            this.point = point;
            this.indexAbove = indexAbove;
            this.indexBelow = indexBelow;
        }

        public override bool Equals(object obj)
        {
            return obj is CutPoint point &&
                   this.point.Equals(point.point);
        }
    }
    static List<Vector3> _verticesAbove;
    static List<Vector3> _verticesBelow;

    static List<int> _trianglesAbove;
    static List<int> _trianglesBelow;

    static List<Vector2> _upUVs;
    static List<Vector2> _downUVs;

    static List<CutPoint> _cutPoints;
    public static bool CutMesh(Plane planeCut, Transform transform, Mesh targetMesh, out (Mesh, Mesh) result)
    {
        var originalMesh = targetMesh;
        var originalVertices = new List<Vector3>();
        var originalTriangles = new List<int>();
        var originalUVs = new List<Vector2>();

        originalMesh.GetVertices(originalVertices);
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
        originalTriangles.AddRange(originalMesh.GetTriangles(i));

        }
        originalMesh.GetUVs(0, originalUVs);

        var meshCutAbove = new Mesh();
        meshCutAbove.name = targetMesh.name + " " + "Mesh Above";
        var meshCutBelow = new Mesh();
        meshCutBelow.name = targetMesh.name + " " + "Mesh Below";

        //dectect which mesh belongs each vertex
        _verticesAbove = new List<Vector3>();
        _verticesBelow = new List<Vector3>();
        _trianglesAbove = new List<int>();
        _trianglesBelow = new List<int>();
        _cutPoints = new List<CutPoint>();
        _upUVs = new List<Vector2>();
        _downUVs = new List<Vector2>();
        List<Vector3> totalCutPoints = new List<Vector3>();



        for (int i = 0; i < originalTriangles.Count; i += 3)
        {
            var index1 = originalTriangles[i];
            var index2 = originalTriangles[i + 1];
            var index3 = originalTriangles[i + 2];

            Vector3 v1 = originalVertices[index1];
            Vector3 v2 = originalVertices[index2];
            Vector3 v3 = originalVertices[index3];

            var indexUv1 = index1;
            var indexUv2 = index2;
            var indexUv3 = index3;

            v1 = transform.TransformPoint(v1);
            v2 = transform.TransformPoint(v2);
            v3 = transform.TransformPoint(v3);

            var uv1 = originalUVs[indexUv1];
            var uv2 = originalUVs[indexUv2];
            var uv3 = originalUVs[indexUv3];

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
                CutPoint cut1, cut2;
                bool side;
                (int, int) aloneCuts;
                (int, int) sameSideCuts;
                int indexACut1, indexBCut1;
                int indexACut2, indexBCut2;
                if (v1IsAbove == v2IsAbove) //same side;
                {
                    side = v1IsAbove;

                    cut1 = CutAndAddToTriangelist(planeCut, v2, uv2, v3, uv3, out indexACut1, out indexBCut1);
                    cut2 = CutAndAddToTriangelist(planeCut, v3, uv3, v1, uv1, out indexACut2, out indexBCut2);

                    aloneCuts = (GetCutIndex(indexACut1, indexBCut1, !side), GetCutIndex(indexACut2, indexBCut2, !side));
                    sameSideCuts = (GetCutIndex(indexACut1, indexBCut1, side), GetCutIndex(indexACut2, indexBCut2, side));

                    AddToTriangleList(idxV3, aloneCuts.Item2, aloneCuts.Item1, !side);

                    AddToTriangleList(idxV1, idxV2, sameSideCuts.Item2, side);

                    AddToTriangleList(idxV2, sameSideCuts.Item1, sameSideCuts.Item2, side);



                }
                else
                {
                    if (v1IsAbove == v3IsAbove)
                    {
                        side = v1IsAbove;

                        cut1 = CutAndAddToTriangelist(planeCut, v1, uv1, v2, uv2, out indexACut1, out indexBCut1);
                        cut2 = CutAndAddToTriangelist(planeCut, v2, uv2, v3, uv3, out indexACut2, out indexBCut2);

                        aloneCuts = (GetCutIndex(indexACut1, indexBCut1, !side), GetCutIndex(indexACut2, indexBCut2, !side));
                        sameSideCuts = (GetCutIndex(indexACut1, indexBCut1, side), GetCutIndex(indexACut2, indexBCut2, side));

                        AddToTriangleList(idxV2, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV1, sameSideCuts.Item1, idxV3, side);

                        AddToTriangleList(idxV3, sameSideCuts.Item1, sameSideCuts.Item2, side);



                    }
                    else
                    {
                        side = !v1IsAbove;

                        cut1 = CutAndAddToTriangelist(planeCut, v3, uv3, v1, uv1, out indexACut1, out indexBCut1);
                        cut2 = CutAndAddToTriangelist(planeCut, v1, uv1, v2, uv2, out indexACut2, out indexBCut2);

                        aloneCuts = (GetCutIndex(indexACut1, indexBCut1, !side), GetCutIndex(indexACut2, indexBCut2, !side));
                        sameSideCuts = (GetCutIndex(indexACut1, indexBCut1, side), GetCutIndex(indexACut2, indexBCut2, side));

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

        result = (null, null);
        if (_cutPoints.Count < 2) return false;
        Vector3 centroid = Vector3.zero;

        _cutPoints.ForEach(cP => centroid += cP.point);
        centroid /= _cutPoints.Count;




        var firtsPoints = _cutPoints[0].point - centroid;
        _cutPoints = _cutPoints.OrderBy(cP => Vector3.SignedAngle(firtsPoints.normalized, (cP.point - centroid).normalized, planeCut.normal)).ToList();


   
        _verticesAbove.Add(centroid);
        _verticesBelow.Add(centroid);
        _upUVs.Add(Vector2.zero);
        _downUVs.Add(Vector2.zero);
        Dictionary<int, List<int>> triangleWithSubMeshAbove = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> triangleWithSubMeshBelow = new Dictionary<int, List<int>>();
        var triangleA = new int[3];
        var triangleB = new int[3];

        triangleWithSubMeshAbove.Add(0, _trianglesAbove);
        triangleWithSubMeshBelow.Add(0, _trianglesBelow);

        triangleWithSubMeshAbove.Add(1, new List<int>());
        triangleWithSubMeshBelow.Add(1, new List<int>());

        for (int i = 0; i < _cutPoints.Count; i++)
        {
            var point1 = _cutPoints[i];
            var point2 = _cutPoints[(i + 1) % _cutPoints.Count];
            var point3 = centroid;

            Debug.DrawLine(point1.point, point2.point, Color.red, 100);

            triangleA[0] = point2.indexAbove;
            triangleA[1] = point1.indexAbove;
            triangleA[2] = _verticesAbove.Count - 1;

            triangleB[0] = point1.indexBelow;
            triangleB[1] = point2.indexBelow;
            triangleB[2] = _verticesBelow.Count - 1;

            triangleWithSubMeshAbove[1].AddRange(triangleA);
            triangleWithSubMeshBelow[1].AddRange(triangleB);


        }


        //_trianglesAbove.AddRange(coverATriangle);
        //_trianglesBelow.AddRange(coverBTriangle);

        var upMesh = CreateMesh(_verticesAbove, triangleWithSubMeshAbove, _upUVs,transform);
        var downMesh = CreateMesh(_verticesBelow, triangleWithSubMeshBelow, _downUVs,transform);

        result = (upMesh, downMesh);

        return true;


    }

    private static CutPoint CutAndAddToTriangelist(Plane planeCut, Vector3 from, Vector2 uvFrom, Vector3 to, Vector2 uvTo, out int indexCutA, out int indexCutB)
    {
        var cut = GetCutPoint(planeCut, from, to, out var distance);
        var interpolatedUV = Vector2.Lerp(uvFrom, uvTo, InverseLerp(from, to, cut));
        indexCutA = AddToVertexList(cut, interpolatedUV, true);
        indexCutB = AddToVertexList(cut, interpolatedUV, false);
        return new CutPoint(cut, indexCutA, indexCutB);

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

    private static int AddToVertexList(Vector3 vertex, Vector2 uv, bool above, bool firts = false)
    {
        if (above)
        {
            //if (_verticesAbove.Contains(vertex))
            //{
            //    return _verticesAbove.IndexOf(vertex);
            //}
            if (firts)
            {
                _verticesAbove.Insert(0, vertex);

            }
            else
                _verticesAbove.Add(vertex);
            _upUVs.Add(uv);
            return _verticesAbove.Count - 1;
        }
        else
        {
            //if (_verticesBelow.Contains(vertex))
            //{
            //    return _verticesBelow.IndexOf(vertex);
            //}
            if (firts)
            {
                _verticesBelow.Insert(0, vertex);
            }
            else
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
    private static int GetCutIndex(int indexAbove, int indexBelow, bool above)
    {
        return above ? indexAbove : indexBelow;
    }

}
