using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class Doodler : MonoBehaviour
{
    [Header("Jump Cycle")]
    [Min(0.01f)]
    [SerializeField] private float jumpHeight = 3f;
    [Min(0.05f)]
    [SerializeField] private float jumpCycleDuration = 1.7666667f;
    [SerializeField] private float jumpSpeed;

    [Header("Horizontal Movement")]
    [SerializeField] private float horizontalSpeed = 12f;
    [Min(0.01f)]
    [SerializeField] private float cursorFollowSmoothTime = 0.12f;
    [SerializeField] private float leftLimit = -3.5f;
    [SerializeField] private float rightLimit = 3.5f;

    [Header("Mouse Mapping")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera rightGameCamera;
    [SerializeField] private Renderer rightLensRenderer;
    [SerializeField] private DoodlerPlatformSystem platformSystem;
    
    private static readonly int IsDoodling = Animator.StringToHash("IsDoodling");

    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private Vector2 _startPosition;
    private bool _started;
    private bool _canJump;
    private bool _jumpRequested;
    private bool _dead;
    private float _horizontalVelocity;

    private void OnValidate()
    {
        jumpHeight = Mathf.Max(0.01f, jumpHeight);
        jumpCycleDuration = Mathf.Max(0.05f, jumpCycleDuration);
        jumpSpeed = CalculateJumpSpeed();
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _startPosition = _rigidbody.position;

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.freezeRotation = true;
        ApplyJumpPhysics();
        ResetDoodler();
    }

    private void OnEnable()
    {
        if (_rigidbody != null)
            ResetDoodler();
    }

    private void OnDisable()
    {
        if (_rigidbody != null)
            _rigidbody.simulated = false;
    }

    public void ResetForBoss()
    {
        ResetDoodler();
    }

    private void ResetDoodler()
    {
        _started = false;
        _rigidbody.position = _startPosition;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;
        _animator.SetBool(IsDoodling, false);
        _canJump = false;
        _jumpRequested = false;
        _dead = false;
        _horizontalVelocity = 0f;
    }

    private void Update()
    {
        if (!_started)
        {
            if (Input.GetMouseButtonDown(0))
                BeginDoodling();

            return;
        }

        if (platformSystem != null && platformSystem.TryHandleDoodlerFall(_rigidbody.position.y))
        {
            StopDoodling();
            return;
        }

        if (TryGetMouseHorizontalPosition(out float targetX))
        {
            float nextX = Mathf.SmoothDamp(
                _rigidbody.position.x,
                targetX,
                ref _horizontalVelocity,
                cursorFollowSmoothTime,
                horizontalSpeed);

            if(_canJump == false)
            {
                _rigidbody.position = new Vector2(nextX, _rigidbody.position.y);
            }
        }
    }

    private void BeginDoodling()
    {
        _started = true;
        _animator.SetBool(IsDoodling, true);
        _rigidbody.simulated = true;
        // Jump();
    }

    private void StopDoodling()
    {
        if (_dead)
            return;

        _dead = true;
        _started = false;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;
        _animator.SetBool(IsDoodling, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        bool landedFromAbove = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                landedFromAbove = true;
                break;
            }
        }

        if (!landedFromAbove || _rigidbody.linearVelocity.y > 0f)
            return;

        DoodlerPlatform platform = collision.gameObject.GetComponent<DoodlerPlatform>();
        if (platform != null && platformSystem != null)
            platformSystem.LandedOn(platform);

        _canJump = true;
        TryJump();
    }

    // void OnCollisionExit2D(Collision2D collision)
    // {
    //     if(collision.gameObject.CompareTag("Ground")) _canJump = false;
    // }

    private void Jump()
    {
        _jumpRequested = true;
        TryJump();
    }

    private void TryJump()
    {
        if (!_canJump || !_jumpRequested)
            return;

        ApplyJumpPhysics();
        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, jumpSpeed);

        _canJump = false;
        _jumpRequested = false;
    }

    private void ApplyJumpPhysics()
    {
        jumpSpeed = CalculateJumpSpeed();

        float gravity = 8f * jumpHeight / (jumpCycleDuration * jumpCycleDuration);
        float worldGravity = Mathf.Abs(Physics2D.gravity.y);
        _rigidbody.gravityScale = worldGravity > 0.001f ? gravity / worldGravity : 0f;
    }

    private float CalculateJumpSpeed()
    {
        return 4f * jumpHeight / jumpCycleDuration;
    }

    private bool TryGetMouseHorizontalPosition(out float targetX)
    {
        targetX = transform.position.x;

        if (mainCamera == null || rightGameCamera == null || rightLensRenderer == null)
            return false;

        Bounds lensBounds = rightLensRenderer.bounds;
        float leftScreenX = mainCamera.WorldToScreenPoint(lensBounds.min).x;
        float rightScreenX = mainCamera.WorldToScreenPoint(lensBounds.max).x;
        float lensScreenX = Mathf.InverseLerp(leftScreenX, rightScreenX, Input.mousePosition.x);

        float centerX = rightGameCamera.transform.position.x;
        targetX = Mathf.Lerp(centerX + leftLimit, centerX + rightLimit, lensScreenX);
        return true;
    }

}
