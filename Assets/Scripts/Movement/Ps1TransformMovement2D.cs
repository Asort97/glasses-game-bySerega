using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class Ps1TransformMovement2D : MonoBehaviour
{
    private sealed class VisualProxy
    {
        public SpriteRenderer source;
        public SpriteRenderer renderer;
        public Transform transform;
        public bool sourceForceRenderingOff;
    }

    [Header("Visuals")]
    [Tooltip("Sprite renderers affected by the visual filter. Physics transforms are never changed.")]
    [SerializeField] private SpriteRenderer[] visualRenderers;

    [Header("PS1 Grid")]
    [Min(0.01f)] [SerializeField] private float gridCellSize = 0.1f;
    [Range(0f, 20f)] [SerializeField] private float directionJitterDegrees = 6f;
    [SerializeField] private Vector2 directionJitterInterval = new Vector2(0.12f, 0.28f);

    private readonly List<VisualProxy> _proxies = new List<VisualProxy>();
    private MaterialPropertyBlock _propertyBlock;
    private Vector3 _lastSourcePosition;
    private float _directionJitter;
    private float _nextJitterTime;
    private bool _initialized;

    private void OnEnable()
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        CreateVisualProxies();
        Resync();
    }

    private void OnDisable()
    {
        DestroyVisualProxies();
        _initialized = false;
    }

    private void OnDestroy()
    {
        DestroyVisualProxies();
    }

    private void LateUpdate()
    {
        Vector3 sourcePosition = transform.position;
        if (!_initialized)
        {
            Resync();
            return;
        }

        _nextJitterTime -= Time.deltaTime;
        if (_nextJitterTime <= 0f)
            PickDirectionJitter();

        Vector2 sourceDelta = sourcePosition - _lastSourcePosition;
        Vector2 jitteredDelta = Ps1GridMath2D.Rotate(sourceDelta, _directionJitter);
        Vector2 samplePosition = (Vector2)sourcePosition + jitteredDelta - sourceDelta;
        Vector2 snappedPosition = Ps1GridMath2D.Snap(samplePosition, gridCellSize);
        Vector3 visualOffset = new Vector3(
            snappedPosition.x - sourcePosition.x,
            snappedPosition.y - sourcePosition.y,
            0f);

        _lastSourcePosition = sourcePosition;
        UpdateVisualProxies(visualOffset);
    }

    public void Resync()
    {
        _lastSourcePosition = transform.position;
        _initialized = true;
        PickDirectionJitter();

        Vector2 snappedPosition = Ps1GridMath2D.Snap(_lastSourcePosition, gridCellSize);
        Vector3 visualOffset = new Vector3(
            snappedPosition.x - _lastSourcePosition.x,
            snappedPosition.y - _lastSourcePosition.y,
            0f);
        UpdateVisualProxies(visualOffset);
    }

    public void Configure(float cellSize, float jitterDegrees, Vector2 jitterInterval)
    {
        gridCellSize = Mathf.Max(0.01f, cellSize);
        directionJitterDegrees = Mathf.Clamp(jitterDegrees, 0f, 20f);
        directionJitterInterval = jitterInterval;
        Resync();
    }

    private void CreateVisualProxies()
    {
        DestroyVisualProxies();

        if (visualRenderers == null || visualRenderers.Length == 0)
        {
            SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();
            if (ownRenderer != null)
                visualRenderers = new[] { ownRenderer };
        }

        if (visualRenderers == null)
            return;

        HashSet<SpriteRenderer> uniqueRenderers = new HashSet<SpriteRenderer>();
        foreach (SpriteRenderer source in visualRenderers)
        {
            if (source == null || !uniqueRenderers.Add(source))
                continue;

            GameObject proxyObject = new GameObject(source.gameObject.name + "_PS1Visual")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = source.gameObject.layer
            };
            proxyObject.transform.SetParent(transform.parent, true);

            SpriteRenderer proxyRenderer = proxyObject.AddComponent<SpriteRenderer>();
            VisualProxy proxy = new VisualProxy
            {
                source = source,
                renderer = proxyRenderer,
                transform = proxyObject.transform,
                sourceForceRenderingOff = source.forceRenderingOff
            };

            source.forceRenderingOff = true;
            _proxies.Add(proxy);
        }
    }

    private void UpdateVisualProxies(Vector3 visualOffset)
    {
        foreach (VisualProxy proxy in _proxies)
        {
            if (proxy.source == null || proxy.renderer == null)
                continue;

            CopyRendererState(proxy.source, proxy.renderer);
            proxy.transform.SetPositionAndRotation(
                proxy.source.transform.position + visualOffset,
                proxy.source.transform.rotation);
            proxy.transform.localScale = GetLocalScaleForWorldScale(
                proxy.transform.parent,
                proxy.source.transform.lossyScale);
        }
    }

    private void CopyRendererState(SpriteRenderer source, SpriteRenderer target)
    {
        target.enabled = source.enabled && source.gameObject.activeInHierarchy;
        target.sprite = source.sprite;
        target.color = source.color;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.maskInteraction = source.maskInteraction;
        target.spriteSortPoint = source.spriteSortPoint;
        target.sharedMaterial = source.sharedMaterial;

        source.GetPropertyBlock(_propertyBlock);
        target.SetPropertyBlock(_propertyBlock);
    }

    private void DestroyVisualProxies()
    {
        foreach (VisualProxy proxy in _proxies)
        {
            if (proxy.source != null)
                proxy.source.forceRenderingOff = proxy.sourceForceRenderingOff;

            if (proxy.renderer != null)
                proxy.renderer.enabled = false;

            if (proxy.transform != null)
                Destroy(proxy.transform.gameObject);
        }

        _proxies.Clear();
    }

    private void PickDirectionJitter()
    {
        _directionJitter = Random.Range(-directionJitterDegrees, directionJitterDegrees);
        _nextJitterTime = Ps1GridMath2D.RandomInterval(directionJitterInterval);
    }

    private static Vector3 GetLocalScaleForWorldScale(Transform parent, Vector3 worldScale)
    {
        if (parent == null)
            return worldScale;

        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
    }
}
