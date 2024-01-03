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
    private Vector3 _shootDirection => (_target.position - _shootPoint.position).normalized;
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
        obj.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0,1,0.5f,.8f,.8f,1f);
        obj.AddForce(new Vector3(Fx, Fy, Fz), ForceMode.Impulse);
        obj.AddRelativeTorque(Random.insideUnitSphere * 10, ForceMode.Impulse);
        Destroy(obj.gameObject, 5f);
        Invoke(nameof(SpawnAndThrowCanonBall), Random.Range(_SpawnRate.x, _SpawnRate.y));
    }
    private IEnumerator Shrink(Transform toShrink, float delay = 2f)
    {
        yield return new WaitForSeconds(delay);
        if (toShrink == null) yield break;
        var duration = .5f;
        var steptime = 0f;
        var initialScale = toShrink.localScale;
        while (steptime < duration)
        {
            if (toShrink == null) yield break;

            steptime += Time.deltaTime;
            toShrink.localScale = Vector3.Lerp(initialScale, Vector3.zero, steptime / duration);
            yield return new WaitForEndOfFrame();
        }
        Destroy(toShrink.gameObject);
    }
}
