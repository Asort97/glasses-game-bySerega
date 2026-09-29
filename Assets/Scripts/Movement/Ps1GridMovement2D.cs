using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class Ps1GridMovement2D : MonoBehaviour
{
    [Header("Motion")]
    [Min(0f)] [SerializeField] private float speed = 2f;
    [SerializeField] private Vector2 initialDirection = Vector2.right;

    [Header("PS1 Grid")]
    [Min(0.01f)] [SerializeField] private float gridCellSize = 0.1f;
    [Range(0f, 20f)] [SerializeField] private float directionJitterDegrees = 6f;
    [SerializeField] private Vector2 directionJitterInterval = new Vector2(0.12f, 0.28f);

    private Rigidbody2D _body;
    private Ps1TransformMovement2D _visualMovement;
    private Vector2 _direction;
    private Vector2 _unsnappedPosition;
    private bool _hasRuntimeDirection;
    private bool _paused;

    public Vector2 Direction => _direction;
    public float Speed => speed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        ResyncToBody();
    }

    private void OnDisable()
    {
        StopBody();
    }

    private void FixedUpdate()
    {
        if (_paused)
        {
            StopBody();
            return;
        }

        StopBody();

        _unsnappedPosition += _direction * speed * Time.fixedDeltaTime;
        _body.MovePosition(_unsnappedPosition);
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        _direction = direction.normalized;
        _hasRuntimeDirection = true;
    }

    public void SetSpeed(float value)
    {
        speed = Mathf.Max(0f, value);
    }

    public void SetVelocity(Vector2 velocity)
    {
        speed = velocity.magnitude;
        if (speed <= Mathf.Epsilon)
            return;

        _direction = velocity / speed;
        _hasRuntimeDirection = true;
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (paused)
            StopBody();
        else
            ResyncToBody();
    }

    public void ResyncToBody()
    {
        EnsureInitialized();
        _unsnappedPosition = _body.position;
        _visualMovement.Resync();
    }

    private void EnsureInitialized()
    {
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();

        if (_visualMovement == null)
        {
            _visualMovement = GetComponent<Ps1TransformMovement2D>();
            if (_visualMovement == null)
                _visualMovement = gameObject.AddComponent<Ps1TransformMovement2D>();

            _visualMovement.Configure(gridCellSize, directionJitterDegrees, directionJitterInterval);
        }

        _body.interpolation = RigidbodyInterpolation2D.None;

        if (_hasRuntimeDirection)
            return;

        _direction = initialDirection.sqrMagnitude > Mathf.Epsilon
            ? initialDirection.normalized
            : Vector2.right;
    }

    private void StopBody()
    {
        if (_body != null)
            _body.linearVelocity = Vector2.zero;
    }

}
