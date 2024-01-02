using UnityEngine;

public class KatanaColisionHandler : MonoBehaviour
{
    [SerializeField] private CutterController _cutter;
    private void Awake()
    {
        _cutter.OnAttackStartedEvent += TurnONCollider;
        _cutter.OnAttackFinishEvent += TurnOFFCollider;

        TurnOFFCollider();

    }

    private void OnDestroy()
    {
        _cutter.OnAttackStartedEvent -= TurnONCollider;
        _cutter.OnAttackFinishEvent -= TurnOFFCollider;
    }
    public void TurnONCollider()
    {
        GetComponent<Collider>().enabled = true;
    }
    public void TurnOFFCollider()
    {
        GetComponent<Collider>().enabled = false;

    }


    private void OnTriggerEnter(Collider other)
    {
        var isInLayer = other.gameObject.layer == 6;
        if (isInLayer && other.TryGetComponent<MeshRenderer>(out var rederer))
        {
            if (other.TryGetComponent<Rigidbody>(out var rb))
            {
                _cutter.CutObject(rb.velocity, rederer);
            }
            else
                _cutter.CutObject(Vector3.zero, rederer);

        }
    }
}