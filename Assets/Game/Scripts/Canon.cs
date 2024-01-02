using System.Collections;
using UnityEngine;

public class Canon : MonoBehaviour
{
    [SerializeField] private AudioSource _source;
    [SerializeField] private Rigidbody _prefab;
    [SerializeField] private Transform _target;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private Vector2 _SpawnRate = Vector2.one;
    [SerializeField] private Vector2 _RandomForce = Vector2.one;
    private Vector3 _shootDirection => (_target.position-_shootPoint.position).normalized;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(Random.Range(_SpawnRate.x, _SpawnRate.y));
        SpawnAndThrowCanonBall();
    }
    private void Update()
    {
        var shotDir = _shootDirection;
        shotDir.y = 0;
        transform.right = shotDir;
    }
    private void SpawnAndThrowCanonBall()
    {
        _source.Play();
        var obj = Instantiate(_prefab);
        obj.transform.position = _shootPoint.position;

        obj.transform.forward = _shootDirection;
        obj.gameObject.layer = 6;
        var t = 2f;
        var Fx = (_target.position.x - _shootPoint.position.x) / t;
        var Fz = (_target.position.z - _shootPoint.position.z) / t;
        var Fy = -(Physics.gravity.y * t / 2f);

        obj.AddForce(new Vector3(Fx, Fy, Fz), ForceMode.Impulse);
        obj.AddRelativeTorque(Random.insideUnitSphere*10, ForceMode.Impulse);
        Destroy(obj, 3f);
        Invoke(nameof(SpawnAndThrowCanonBall), Random.Range(_SpawnRate.x, _SpawnRate.y));
    }
}
