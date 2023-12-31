using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ProceduralMesh : MonoBehaviour
{
    public Transform planeT;
    public Mesh _mesh;
    public Material covermaterial;
    Plane _planeCut;

    private void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            planeT.Rotate(Vector3.forward);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            planeT.Rotate(Vector3.back);

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {

            _planeCut = new Plane(planeT.transform.up, planeT.transform.position);
            if (MeshCutter.CutMesh(_planeCut, transform, _mesh, out var cutResult))
            {
                //gameObject.GetComponent<MeshRenderer>().enabled = false;


                Rigidbody rbA = CreateRigidBodyObj(cutResult.Item1);
                Rigidbody rbB = CreateRigidBodyObj(cutResult.Item2);

                //rbA.isKinematic = rbB.isKinematic = true;
                //rbA.AddForceAtPosition(_planeCut.normal, Random.insideUnitSphere, ForceMode.Impulse);
                //rbB.AddForceAtPosition(-_planeCut.normal, Random.insideUnitSphere, ForceMode.Impulse);


                rbA.transform.position = transform.position;
                rbB.transform.position = transform.position;
                Destroy(rbA.gameObject,10);
                Destroy(rbB.gameObject,10);

            }


        }

    }
    private Rigidbody CreateRigidBodyObj(Mesh mesh)
    {
        var Obj = new GameObject();
        Obj.name = mesh.name + " " + Obj.GetInstanceID();
        Obj.AddComponent<MeshFilter>().mesh = mesh;
        var renderer = Obj.AddComponent<MeshRenderer>();
        var originalRenderer = GetComponent<MeshRenderer>();
       var materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
           
            var mat = i < originalRenderer.materials.Length ? originalRenderer.materials[i] : covermaterial;
            materials[i] = mat;
        }
        renderer.materials = materials;
        var meshC = Obj.AddComponent<MeshCollider>();
        meshC.sharedMesh = mesh;
        meshC.convex = true;
        return Obj.AddComponent<Rigidbody>();
    }

   

}
