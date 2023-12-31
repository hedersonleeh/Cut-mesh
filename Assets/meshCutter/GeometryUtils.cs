﻿using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class GeometryUtils
{
    public static Mesh CreateMesh(List<Vector3> vertices, Dictionary<int, List<int>> triangles, List<Vector2> uvs, Transform transform)
    {
        var mesh = new Mesh();
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i] - transform.position;
        }
        mesh.SetVertices(vertices.ToArray());
        mesh.subMeshCount = 2;
        foreach (var subMesh in triangles.Keys)
            mesh.SetTriangles(triangles[subMesh].ToArray(), subMesh);

        mesh.uv = uvs.ToArray();
        mesh.RecalculateTangents();

        var normals = GetNormals(vertices, triangles[0]);

        mesh.SetNormals(normals);




        mesh.RecalculateBounds();

        return mesh;
    }


    private static List<Vector3> GetNormals(List<Vector3> vertices, List<int> triangles)
    {
        var normals = new List<Vector3>(vertices);


        for (int i = 0; i < triangles.Count; i += 3)
        {
            int vertIndex1 = triangles[i];
            int vertIndex2 = triangles[i + 1];
            int vertIndex3 = triangles[i + 2];

            Vector3 triangleNormal = ComputeNormal(vertices[vertIndex1], vertices[vertIndex2], vertices[vertIndex3]);

            normals[vertIndex1] += triangleNormal;
            normals[vertIndex2] += triangleNormal;
            normals[vertIndex3] += triangleNormal;
        }

        //normals.ForEach(x =>
        //{
        //    x.Normalize();
        //});
        return normals;
    }
    private static Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        Vector3 side1 = vertex2 - vertex1;
        Vector3 side2 = vertex3 - vertex1;

        Vector3 normal = Vector3.Cross(side1, side2);

        return normal;
    }
    public static List<Vector3> SortPolygonPoints(List<Vector3> points, bool clockwise = false)
    {
        //if (clockwise)
        //    points.Sort(CompareClockWise);
        //else
        //    points.Sort(CompareCounterClockWise);

        Dictionary<Vector3, List<int>> planeCount = new Dictionary<Vector3, List<int>>();

        for (int i = 0; i < points.Count; i += 3)
        {
            Vector3 v1 = points[i % points.Count];
            Vector3 v2 = points[(i + 1) % points.Count];
            Vector3 v3 = points[(i + 2) % points.Count];

            var plane = new Plane(v1, v2, v3);
            if (!planeCount.ContainsKey(plane.normal))
            {
                planeCount.Add(plane.normal, new List<int>());
            }
            planeCount[plane.normal].Add(i);
            planeCount[plane.normal].Add((i + 1) % points.Count);
            planeCount[plane.normal].Add((i + 2) % points.Count);
        }
        List<Vector3> result = new List<Vector3>();
        foreach (var pNormal in planeCount.Keys)
        {
            var orderedIndexList = planeCount[pNormal].OrderBy((idx) =>
            {
                var cross = Vector3.Cross(pNormal, points[idx]);
                return points[idx].y;
            });
            foreach (var idx in orderedIndexList)
            {
                result.Add(points[idx]);
            }
        }
        result = new List<Vector3>(points);
        result.Sort(CompareCounterClockWise);
        return result;
    }

    public static int CompareClockWise(Vector3 A, Vector3 B)
    {

        if (Mathf.Atan2(A.y, A.x) < Mathf.Atan2(B.y, B.x))
        {

            return -1;
        }
        else
        if (Mathf.Atan2(A.y, A.x) > Mathf.Atan2(B.y, B.x))
        {

            return 1;
        }
        else
        {
            if (A == B)
                return 0;
            else
            {
                if (A.x > B.x)
                    return -1;
                else
                if (A.x > B.x)
                    return 1;
                else if (A.y > B.y)
                    return -1;
                else
                    return 1;
            }
        }

    }
    private static int CompareCounterClockWise(Vector3 A, Vector3 B)
    {
        var c = Vector3.Cross(A, B);
        if (Mathf.Abs(c.x) > 0)
        {
            A.x = A.z;
            B.x = B.z;
        }
        else
        if (Mathf.Abs(c.y) > 0)
        {
            A.y = A.z;
            B.y = B.z;
        }
        if (Mathf.Abs(c.z) > 0)
        {

        }
        if (Mathf.Atan2(A.x, A.y) < Mathf.Atan2(B.x, B.y))
        {

            return -1;
        }
        else
        if (Mathf.Atan2(A.x, A.y) > Mathf.Atan2(B.x, B.y))
        {

            return 1;
        }
        else
        {
            if (A.x == B.x)
            {
                if (A.y < B.y)
                    return -1;
                else
                    return 1;
            }
            else
            {
                if (A.x > B.x)
                    return -1;
                else if (A.x < B.x)
                    return 1;
            }

            return 0;

        }

    }
    public static float GetSlope(Vector3 a, Vector3 b)
    {
        if (b.x - a.x == 0) return 0;
        return (b.y - a.y) / (b.x - a.x);
    }
    /// <summary>
    /// Will return (A,B,C) from = Ax+By+C  line form
    /// </summary>
    /// <param name="P1"></param>
    /// <param name="P2"></param>
    /// <returns></returns>
    public static Vector3 GetLineOnGeneralForm(Vector3 P1, Vector3 P2)
    {
        return new Vector3(P2.y - P1.y,
                                      P1.x - P2.x,
                                      -(P1.x * P2.y) - (P2.x * P1.y));
    }
    public static Vector3 GetCutPoint(Plane plane, Vector3 P1, Vector3 P2, out float distance)
    {
        var P1toP2 = (P2 - P1).normalized;
        Ray rayToPlane = new Ray(P1, P1toP2);
        var raycastHit = plane.Raycast(rayToPlane, out distance);
        Debug.Assert(raycastHit, "The raycast to plane doesn't hit there was an error on the vectors or plane");
        var cutPoint = rayToPlane.GetPoint(distance);
        Debug.DrawLine(P1, cutPoint, Color.gray, 10);
        return cutPoint;
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
            // The lines are parallel.          
            return C;
        }
        else
        {
            float x = (b2 * c1 - b1 * c2) / determinant;
            float y = (a1 * c2 - a2 * c1) / determinant;
            return new Vector3(x, y);
        }
    }
    /// <summary>
    /// Implementation of Ear Clipping algorithm
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static int[] GetTrianglesFromPoints(List<Vector3> points)
    {
        if (points.Count < 3)
        {
            Debug.LogError("Cant create triangle with less than 3 vertices");
            return null;
        }
        var indexBuffer = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            indexBuffer.Add(i);
        }
        var triangles = new int[(points.Count - 2) * 3];
        int triangleIndex = 0;
        while (indexBuffer.Count > 3)
        {
            for (int i = 0; i < indexBuffer.Count; i++)
            {
                var pIndex = GetItem(indexBuffer, i - 1);
                var cIndex = GetItem(indexBuffer, i);
                var nIndex = GetItem(indexBuffer, i + 1);

                var previusPoint = points[pIndex];
                var currentPoint = points[cIndex];
                var nextPoint = points[nIndex];

                var angle = Vector3.Angle(previusPoint - currentPoint, nextPoint - currentPoint);

                if (angle == 180f) continue;
                //Checks if any verticies is inside the new triangle
                bool canCut = true;
                for (int j = 0; j < indexBuffer.Count; j++)
                {
                    if (j == pIndex || j == cIndex || j == nIndex) continue;

                    if (IsPoinOnTriangle(points[j], previusPoint, currentPoint, nextPoint))
                    {
                        canCut = false;
                        break;
                    }

                }
                if (canCut)
                {
                    triangles[triangleIndex++] = pIndex;
                    triangles[triangleIndex++] = cIndex;
                    triangles[triangleIndex++] = nIndex;
                    indexBuffer.Remove(cIndex);
                    break;
                }

            }



        }

        triangles[triangleIndex++] = indexBuffer[0];
        triangles[triangleIndex++] = indexBuffer[1];
        triangles[triangleIndex++] = indexBuffer[2];
        return triangles;
    }

    private static bool IsPoinOnTriangle(Vector3 point, Vector3 A, Vector3 B, Vector3 C)
    {
        var AB = B - A;
        var BC = C - B;
        var CA = A - C;

        var ABcP = Vector3.Cross(AB, point - A).z;
        var BCcP = Vector3.Cross(BC, point - B).z;
        var CAcP = Vector3.Cross(CA, point - C).z;

        if (ABcP > 0f || BCcP > 0f || CAcP > 0f)
            return false;
        return true;
    }

    private static T GetItem<T>(List<T> list, int i)
    {
        if (i > list.Count)
            return list[i % list.Count];
        if (i < 0)
            return list[(i % list.Count) + list.Count];

        return list[i];
    }



}
