using System.Collections;
using UnityEngine;
public class CutterController : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private Transform _katana;
    [SerializeField] private Transform _pickUpPosition;
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioSource _cutSfx;

    [SerializeField] private Animator _animator;
    [SerializeField] private float pickUpVelocity = 10;
    Camera _mainCamera;
    Coroutine _attackCoroutine;
    Vector3 _mouseInWorld;
    Plane _cameraPlane;
    Plane _cutPlane;
    bool _attacking;

    bool _grabbed;
    bool _gameStarted;

    public delegate void OnAttack();
    public OnAttack OnAttackStartedEvent;
    public OnAttack OnAttackFinishEvent;
    private void Awake()
    {
        _mainCamera = Camera.main;
        _cameraPlane = new Plane(_mainCamera.transform.forward, 4f);

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
        if (Input.GetMouseButtonDown(0) & !_grabbed)
        {
            StartCoroutine(GameStart());
            _grabbed = true;
        }
        SetMouseInfo();



        if (_gameStarted)
        {
            UpdateController();
        }
    }

    void UpdateController()
    {

        if (!_attacking)
        {
            var vpPos = _mainCamera.WorldToViewportPoint(_mouseInWorld);
            vpPos -= new Vector3(1, 1, 0) * .5f;
            vpPos *= 2f;
            vpPos.z = 0;
            var targetRotation = Quaternion.LookRotation(vpPos, vpPos.x * Vector3.up);
            _katana.rotation = targetRotation;
            _katana.position = Vector3.Lerp(_katana.position, _mouseInWorld, Time.smoothDeltaTime * pickUpVelocity);
        }

        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && !_attacking)
        {
            _source.pitch = 1 + Random.value * .2f;
            _source.Play();
            _animator.Play("Attack");
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _attackCoroutine = StartCoroutine(AttackCooldown(0.2f));
        }

    }

    private void SetMouseInfo()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = _mainCamera.nearClipPlane;
        var ray = _mainCamera.ScreenPointToRay(mousePos);

        if (_cameraPlane.Raycast(ray, out var distance))
        {
            _mouseInWorld = ray.GetPoint(distance);

        }
        Vector2 v = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    IEnumerator AttackCooldown(float duration)
    {
        OnAttackStartedEvent?.Invoke();

        var lastRotation = _katana.rotation;// Vector3.Lerp(_katana.forward, vpPos, Time.smoothDeltaTime * pickUpVelocity);

        _attacking = true;
        _cutPlane = new Plane(_katana.up, _katana.position);
        yield return new WaitForSeconds(duration / 2f);
        var planePoint1 = _cutPlane.ClosestPointOnPlane(Vector2.one * -1000);
        var planePoint2 = _cutPlane.ClosestPointOnPlane(Vector2.one * 1000);
        Debug.DrawLine(planePoint1, planePoint2, Color.cyan, 1);
        yield return new WaitForSeconds(duration / 2f);
        _attacking = false;
        _katana.rotation = lastRotation;// Vector3.Lerp(_katana.forward, vpPos, Time.smoothDeltaTime * pickUpVelocity);
        OnAttackFinishEvent?.Invoke();

    }
    IEnumerator GameStart()
    {
        while (true)
        {
            var vpPos = _mainCamera.WorldToViewportPoint(_mouseInWorld);
            vpPos -= new Vector3(1, 1, 0) * .5f;
            vpPos *= 2f;
            vpPos.z = 0;
            var targetRotation = Quaternion.LookRotation(vpPos, vpPos.x * Vector3.up);
            _katana.rotation = Quaternion.Lerp(_katana.rotation, targetRotation, Time.smoothDeltaTime * pickUpVelocity);

            _katana.position = Vector3.Lerp(_katana.position, _mouseInWorld, Time.smoothDeltaTime * pickUpVelocity);
            if ((_katana.position - _mouseInWorld).magnitude < 0.1f)
                break;
            yield return new WaitForEndOfFrame();
        }
        _gameStarted = true;


    }

    public void CutObject(Vector3 inputVeocity, MeshRenderer renderer)
    {
        if (MeshCutter.CutMesh(_cutPlane, renderer.transform, renderer.GetComponent<MeshFilter>().mesh, out var cutResult))
        {
            Rigidbody rbA = CreateRigidBodyObj(renderer, cutResult.Item1);
            Rigidbody rbB = CreateRigidBodyObj(renderer, cutResult.Item2);
            _cutSfx.pitch = 1 + Random.value * .3f;
            _cutSfx.PlayOneShot(_cutSfx.clip);

            var particles = Instantiate(_particles);
            particles.transform.position = renderer.transform.position;
            particles.transform.forward = _cutPlane.normal;
            var main = particles.GetComponent<ParticleSystemRenderer>();
            main.material.color = renderer.material.color;

            rbA.AddForceAtPosition(_cutPlane.normal, Random.insideUnitSphere * Random.value * 10, ForceMode.Impulse);
            rbB.AddForceAtPosition(-_cutPlane.normal, Random.insideUnitSphere * Random.value * 10, ForceMode.Impulse);
            rbA.velocity = rbB.velocity = inputVeocity;
            rbA.transform.position = renderer.transform.transform.position;
            rbB.transform.position = renderer.transform.transform.position;
            Destroy(renderer.gameObject);
            StartCoroutine(Shrink(rbA.transform, 1 + Random.value * 2f));
            StartCoroutine(Shrink(rbB.transform, 1 + Random.value * 2f));
        }
    }

    private Rigidbody CreateRigidBodyObj(MeshRenderer originalRenderer, Mesh mesh)
    {
        var Obj = new GameObject();
        Obj.name = mesh.name + " " + Obj.GetInstanceID();
        Obj.AddComponent<MeshFilter>().mesh = mesh;
        Invoke(nameof(DelayChangeLayer), .6f);
        Obj.layer = 6;
        var renderer = Obj.AddComponent<MeshRenderer>();
        var materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < mesh.subMeshCount; i++)
        {

            var mat = i < originalRenderer.materials.Length ? originalRenderer.materials[i] : originalRenderer.materials[0];
            materials[i] = mat;
        }
        renderer.materials = materials;
        var meshC = Obj.AddComponent<MeshCollider>();
        meshC.sharedMesh = mesh;
        meshC.convex = true;
        return Obj.AddComponent<Rigidbody>();
    }
    void DelayChangeLayer(GameObject target,int layer)
    {
        target.layer = layer;
    }
    private IEnumerator Shrink(Transform toShrink, float delay = 2f)
    {
        yield return new WaitForSeconds(delay);

        var duration = .5f;
        var steptime = 0f;
        if (toShrink == null) yield break;
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
