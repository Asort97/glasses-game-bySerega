using System;
using UnityEngine;

public class Fisher : MonoBehaviour
{
    [SerializeField] private Transform hookTransform;
    [SerializeField] private Transform reelTransform;
    [SerializeField] private float startHeight;
    [SerializeField] private float endHeight;
    [SerializeField] private float speedDown;
    [SerializeField] private bool readSpaceInput = true;
    [SerializeField] private float reelAnchorOffsetY = 0.28f;
    [SerializeField] private float reelWidthPixels = 1f;
    [SerializeField] private float reelTopOverlap = 0.03f;
    [SerializeField] private float reelHookOverlap = 0.08f;
    [SerializeField] private Color reelLineColor = Color.white;
    [SerializeField] private int reelSortingOrder = 2;

    private bool _isDowning;
    private bool _isUp;
    private LineRenderer _reelLine;

    public event Action HookReachedBottom;
    public event Action HookReturnedToStart;

    public Transform HookTransform => hookTransform;
    public bool IsHookMoving => _isDowning || _isUp;
    public bool IsHookDropping => _isDowning;

    private void Awake()
    {
        EnsureHook();
        EnsureReelLine();
        ResetHook();
    }

    private void OnEnable()
    {
        UpdateReel();
    }

    private void Update()
    {
        if (readSpaceInput && Input.GetKeyDown(KeyCode.Space))
            BeginDrop();

        TickHook(Time.deltaTime);
        UpdateReel();
    }

    public void ResetHook()
    {
        EnsureHook();
        _isDowning = false;
        _isUp = false;

        if (hookTransform != null)
        {
            Vector3 localPosition = hookTransform.localPosition;
            localPosition.y = startHeight;
            hookTransform.localPosition = localPosition;
        }

        UpdateReel();
    }

    public bool BeginDrop()
    {
        if (hookTransform == null || _isDowning || _isUp)
            return false;

        _isDowning = true;
        return true;
    }

    public void BeginReturn()
    {
        if (hookTransform == null)
            return;

        _isDowning = false;
        _isUp = true;
    }

    private void TickHook(float dt)
    {
        if (hookTransform == null)
            return;

        if (_isDowning)
        {
            MoveHook(-speedDown * dt);
            if (hookTransform.localPosition.y <= endHeight)
            {
                _isDowning = false;
                _isUp = true;
                HookReachedBottom?.Invoke();
            }
        }

        if (_isUp)
        {
            MoveHook(speedDown * dt);
            if (hookTransform.localPosition.y >= startHeight)
            {
                _isUp = false;
                HookReturnedToStart?.Invoke();
            }
        }
    }

    private void MoveHook(float yDelta)
    {
        Vector3 localPosition = hookTransform.localPosition;
        localPosition.y = Mathf.Clamp(localPosition.y + yDelta, endHeight, startHeight);
        hookTransform.localPosition = localPosition;
    }

    private void EnsureHook()
    {
        if (hookTransform == null)
            hookTransform = transform.Find("hook");
    }

    private void EnsureReelLine()
    {
        if (reelTransform == null)
        {
            GameObject reel = new GameObject("reel");
            reel.transform.SetParent(transform, false);
            reel.transform.localPosition = hookTransform != null
                ? new Vector3(hookTransform.localPosition.x, startHeight + reelAnchorOffsetY, hookTransform.localPosition.z)
                : new Vector3(0f, startHeight + reelAnchorOffsetY, 0f);
            reel.layer = gameObject.layer;
            reelTransform = reel.transform;
        }
        else if (reelTransform.parent == transform)
        {
            Vector3 localPosition = reelTransform.localPosition;
            localPosition.y = startHeight + reelAnchorOffsetY;
            reelTransform.localPosition = localPosition;
        }

        _reelLine = reelTransform.GetComponent<LineRenderer>();
        if (_reelLine == null)
            _reelLine = reelTransform.gameObject.AddComponent<LineRenderer>();

        _reelLine.useWorldSpace = true;
        _reelLine.positionCount = 2;
        float reelWorldWidth = GetHookPixelWorldSize() * reelWidthPixels;
        _reelLine.startWidth = reelWorldWidth;
        _reelLine.endWidth = reelWorldWidth;
        _reelLine.numCapVertices = 0;
        _reelLine.sortingOrder = reelSortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null && _reelLine.sharedMaterial == null)
            _reelLine.sharedMaterial = new Material(shader);

        _reelLine.startColor = reelLineColor;
        _reelLine.endColor = reelLineColor;
    }

    private float GetHookPixelWorldSize()
    {
        SpriteRenderer hookRenderer = hookTransform != null ? hookTransform.GetComponent<SpriteRenderer>() : null;
        if (hookRenderer != null && hookRenderer.sprite != null)
        {
            Vector2 spritePixels = hookRenderer.sprite.rect.size;
            if (spritePixels.x > 0f && spritePixels.y > 0f)
            {
                Vector3 worldSize = hookRenderer.bounds.size;
                float pixelWidth = Mathf.Min(worldSize.x / spritePixels.x, worldSize.y / spritePixels.y);
                return Mathf.Max(0.001f, pixelWidth);
            }
        }

        return 0.01f;
    }

    private void UpdateReel()
    {
        if (hookTransform == null)
            return;

        EnsureReelLine();

        Vector3 start = reelTransform.position;
        Vector3 end = hookTransform.position;
        Vector3 direction = end - start;
        if (direction.sqrMagnitude > 0.000001f)
        {
            Vector3 normalized = direction.normalized;
            start -= normalized * reelTopOverlap;
            end += normalized * reelHookOverlap;
        }

        _reelLine.SetPosition(0, start);
        _reelLine.SetPosition(1, end);
    }
}
