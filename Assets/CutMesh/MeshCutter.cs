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

                    cut1 = CutAndAddToTriangelist(planeCut, v2, v3);
                    cut2 = CutAndAddToTriangelist(planeCut, v3, v1);

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

                        cut1 = CutAndAddToTriangelist(planeCut, v1, v2);
                        cut2 = CutAndAddToTriangelist(planeCut, v2, v3);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV2, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV1, sameSideCuts.Item1, idxV3, side);
                        AddToTriangleList(idxV3, sameSideCuts.Item1, sameSideCuts.Item2, side);
                    }
                    else
                    {
                        side = !v1IsAbove;

                        cut1 = CutAndAddToTriangelist(planeCut, v3, v1);
                        cut2 = CutAndAddToTriangelist(planeCut, v1, v2);

                        aloneCuts = (GetCutIndex(cut1, !side), GetCutIndex(cut2, !side));
                        sameSideCuts = (GetCutIndex(cut1, side), GetCutIndex(cut2, side));

                        AddToTriangleList(idxV1, aloneCuts.Item2, aloneCuts.Item1, !side);

                        AddToTriangleList(idxV3, sameSideCuts.Item1, idxV2, side);
                        AddToTriangleList(idxV2, sameSideCuts.Item1, sameSideCuts.Item2, side);
                    }
                }

            }

        }

        result = (null, null, null);


        Vector3 centroid = Vector3.zero;

        _cutPoints.ForEach(p => centroid += p);
        centroid /= _cutPoints.Count;
        var coverATriangle = new List<int>();
        var coverBTriangle = new List<int>();

        _verticesAbove.Add(centroid);
        _verticesBelow.Add(centroid);
        for (int i = 0; i < _cutPoints.Count; i++)
        {
            Debug.DrawLine(_cutPoints[i], _cutPoints[(i + 1) % _cutPoints.Count], Color.red, 100);

            var point1 = _cutPoints[i];
            var point2 = _cutPoints[(i + 1) % _cutPoints.Count];
            var point3 = centroid;

            var triangleA = new int[] { _verticesAbove.IndexOf(point1), _verticesAbove.IndexOf(point2), _verticesAbove.IndexOf(centroid) };
            var triangleB = new int[] { _verticesBelow.IndexOf(point2), _verticesBelow.IndexOf(point1), _verticesBelow.IndexOf(centroid) };

            var cross = Vector3.Cross(point2 - point3, point1 - point3);

            var dot = Vector3.Dot(cross, planeCut.normal);

            Debug.Log("Dot: " + dot);
            if (dot < 0)
            {
                //triangleA = FlipTriangle(triangleA);
                //triangleB = FlipTriangle(triangleB);
            }
            else
            {

            }
            //var a1 = _verticesAbove.IndexOf(_cutPoints[i]);
            //var a2 = _verticesAbove.IndexOf(_cutPoints[(i + 1)]);
            //var a3 = _verticesAbove.IndexOf(centroid);

            //int b1 = _verticesBelow.IndexOf(_cutPoints[i]);
            //int b2 = _verticesBelow.IndexOf(_cutPoints[(i + 1)]);
            //int b3 = _verticesBelow.IndexOf(centroid);

            //var cNormal = new Plane(_cutPoints[i], _cutPoints[i + 1], centroid);
            //var n2 =GetHalfwayPoint(out var distance).normalized;
            //var dot = Vector3.Dot(cNormal.normal, planeCut.normal);
            //if (dot > 0)
            //{
            coverATriangle.AddRange(triangleA);


            coverBTriangle.AddRange(triangleB);

            //}
            //else
            //{
            //    coverATriangle.Add(a1);
            //    coverATriangle.Add(a2);
            //    coverATriangle.Add(a3);

            //    coverBTriangle.Add(b3);
            //    coverBTriangle.Add(b2);
            //    coverBTriangle.Add(b1);
            //}

        }


        _trianglesAbove.AddRange(coverATriangle);
        _trianglesBelow.AddRange(coverBTriangle);
        var upMesh = CreateMesh(_verticesAbove, _trianglesAbove, originalMesh, transform);
        var downMesh = CreateMesh(_verticesBelow, _trianglesBelow, originalMesh, transform);

        result = (upMesh, downMesh, _cutPoints);

        return true;


    }
    private static void OrderVertices(Plane plane, ref List<Vector3> vertices, bool clockwise = false)
    {
        var normal = plane.normal;
        bool ready = true;
        while (ready)
        {


            ready = false;
            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[(i + 1) % vertices.Count];
                var v3 = vertices[(i + 2) % vertices.Count];

                var cross = Vector3.Cross(v2 - v1, v3 - v2);
                var dot = Vector3.Dot(cross, normal);

                var triangle = new Vector3[] { v1, v2, v3 };

                if (dot > 0)
                {
                    triangle = FlipTriangleVertices(triangle);

                    vertices[i] = triangle[0];
                    vertices[(i + 1) % vertices.Count] = triangle[1];
                    vertices[(i + 2) % vertices.Count] = triangle[2];
                    ready = true;
                    break;
                }


            }
        }
    }
    private static Vector3[] FlipTriangleVertices(Vector3[] triangleVertices)
    {
        return new Vector3[] { triangleVertices[0], triangleVertices[2], triangleVertices[1] };
    }
    private static int[] FlipTriangle(int[] triangle)
    {
        return new int[] { triangle[1], triangle[0], triangle[2] };
    }
    private static Vector3 CutAndAddToTriangelist(Plane planeCut, Vector3 from, Vector3 to)
    {
        var cut = GetCutPoint(planeCut, from, to);

        if (!_cutPoints.Contains(cut))
            _cutPoints.Add(cut);
        AddToVertexList(cut, true);
        AddToVertexList(cut, false);
        return cut;

    }
    private static Vector3 GetHalfwayPoint(out float distance)
    {
        if (_cutPoints.Count > 0)
        {
            Vector3 firstPoint = _cutPoints[0];
            Vector3 furthestPoint = Vector3.zero;
            distance = 0f;

            foreach (Vector3 point in _cutPoints)
            {
                float currentDistance = 0f;
                currentDistance = Vector3.Distance(firstPoint, point);

                if (currentDistance > distance)
                {
                    distance = currentDistance;
                    furthestPoint = point;
                }
            }

            return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
        }
        else
        {
            distance = 0;
            return Vector3.zero;
        }
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
            if (_verticesAbove.Contains(vertex))
            {
                return _verticesAbove.IndexOf(vertex);
            }
            _verticesAbove.Add(vertex);
            return _verticesAbove.Count - 1;

        }
        else
        {
            if (_verticesBelow.Contains(vertex))
            {
                return _verticesBelow.IndexOf(vertex);
            }

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
