using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    public Transform planeT;
    Plane _planeCut;
    List<Vector3> _cutPoints;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            _planeCut = new Plane(planeT.transform.up, planeT.transform.position);
            if (MeshCutter.CutMesh(_planeCut, transform, gameObject.GetComponent<MeshFilter>().mesh, out var cutResult, out _cutPoints))
            {
                gameObject.SetActive(false);


                Rigidbody rbA = CreateRigidBodyObj(cutResult.Item1);
                Rigidbody rbB = CreateRigidBodyObj(cutResult.Item2);
                //rbA.isKinematic = true;
                //rbB.isKinematic = true;
                //rbA.AddForce(_planeCut.normal, ForceMode.Impulse);
                //rbB.AddForce(-_planeCut.normal, ForceMode.Impulse);


                rbA.transform.position = transform.position;
                rbB.transform.position = transform.position;

                //rbA.transform.localScale = transform.localScale;
                //rbB.transform.localScale = transform.localScale;

            }


        }

    }
    private void OnEnable()
    {
        var mesh = new Mesh();
        mesh.name = "Triangle Mesh";

        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(0,1,0),new Vector3(1,1,0),
             new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(0,1,1),new Vector3(1,1,1),



        };
        mesh.triangles = new int[]
        {
           0,2,1,
           1,2,3,//forward

           4,5,7,
           4,7,6, //backwards

           2,6,3,
           6,7,3,//upward

           1,5,4,
           1,4,0,//downWards

           0,4,6,//left
           0,6,2,

           1,3,7,
           7,5,1//right

        };
        List<Vector3> vertices = mesh.vertices.ToList();
        //GeometryUtils.SortPolygonPoints(vertices);
        //mesh.triangles = GeometryUtils.GetTrianglesFromPoints(vertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.uv = new Vector2[]
        {
             new Vector2(0,0), new Vector2(1,0), new Vector2(0,1),new Vector2(1,1),
             new Vector2(0,0), -new Vector2(1,0), -new Vector2(0,1),-new Vector2(1,1),


        };
        GetComponent<MeshFilter>().mesh = mesh;

    }
    private Rigidbody CreateRigidBodyObj(Mesh mesh)
    {
        var Obj = new GameObject();
        Obj.name = mesh.name + " " + Obj.GetInstanceID();
        Obj.AddComponent<MeshFilter>().mesh = mesh;
        Obj.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        var box = Obj.AddComponent<BoxCollider>();
        box.size = mesh.bounds.size + Vector3.forward * .1f;
        return Obj.AddComponent<Rigidbody>();
    }

    private void OnDrawGizmos()
    {
        var rightLine = Quaternion.Euler(0, 0, 90) * _planeCut.normal;
        var offset = _planeCut.normal * (_planeCut.distance);
        Gizmos.DrawLine(-100 * rightLine - offset, 100 * rightLine - offset);

        if (_cutPoints != null)
            foreach (var p in _cutPoints)
            {
                Gizmos.DrawWireSphere(p, 0.1f);
            }

    }
}
