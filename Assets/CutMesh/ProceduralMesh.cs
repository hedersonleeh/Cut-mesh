using System.Collections;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    Plane _planeCut;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var normal = Random.insideUnitCircle;
            _planeCut = new Plane(normal, -Vector3.Distance(normal, transform.position));

            if (MeshCutter.CutMesh(_planeCut, gameObject.GetComponent<MeshFilter>().mesh, out var cutResult, out var cutPoints))
            {
                var A = new GameObject("A");
                var B = new GameObject("B");

                Rigidbody rbA = CreateRigidBodyObj(cutResult.Item1);
                Rigidbody rbB = CreateRigidBodyObj(cutResult.Item2);

                rbA.AddForce(_planeCut.normal, ForceMode.Impulse);
                rbB.AddForce(-_planeCut.normal, ForceMode.Impulse);


                A.transform.position = transform.position;
                B.transform.position = transform.position;

            }


        }

    }
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
}
