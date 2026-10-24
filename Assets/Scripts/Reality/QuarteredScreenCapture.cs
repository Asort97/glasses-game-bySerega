using UnityEngine;
using UnityEngine.Rendering;

// Owns frozen full-frame images, independent of the desktop background capture.
public sealed class QuarteredScreenCapture : MonoBehaviour
{
    [SerializeField] private Camera captureCamera;
    [SerializeField] private Canvas[] overlayCanvases = new Canvas[0];
    [Header("Cut Position")]
    [Range(0f, 0.2f)] [SerializeField] private float cutRandomness = 0.08f;
    [Header("Strip Displacement")]
    [Range(0f, 0.1f)] [SerializeField] private float minHorizontalOffset = 0.005f;
    [Range(0f, 0.1f)] [SerializeField] private float maxHorizontalOffset = 0.018f;

    private readonly float[] _offsets = new float[4];
    private readonly Rect[] _strips = new Rect[4];
    private RenderTexture _snapshot;
    private bool _showSnapshot;
    private bool _coverVisible;
    public float BlackAlpha { get; private set; }

    public void CaptureAndShow()
    {
        ReleaseSnapshot();
        _snapshot = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height),
            24, RenderTextureFormat.ARGB32)
        {
            name = "Reality Frozen Frame",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _snapshot.Create();

        // Synchronous off-screen rendering prevents a whole, uncut demon frame flashing first.
        // The OnGUI presentation is not rendered by this camera and cannot capture itself.
        var originalTarget = captureCamera.targetTexture;
        var canvasStates = new CanvasState[overlayCanvases.Length];
        try
        {
            // Overlay UI normally bypasses target textures. Include it in this single render.
            for (int i = 0; i < overlayCanvases.Length; i++)
                canvasStates[i] = CanvasState.Include(overlayCanvases[i], captureCamera);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(captureCamera,
                new RenderPipeline.StandardRequest { destination = _snapshot });
        }
        finally
        {
            captureCamera.targetTexture = originalTarget;
            foreach (var state in canvasStates)
                state.Restore();
            Canvas.ForceUpdateCanvases();
        }

        RandomizeStrips();
        _showSnapshot = true;
    }

    private void RandomizeStrips()
    {
        const float stripHeight = 0.25f;
        float jitter = Mathf.Clamp(cutRandomness, 0f, 0.2f) * stripHeight;
        float top = 0f;
        float minOffset = Mathf.Min(minHorizontalOffset, maxHorizontalOffset);
        for (int i = 0; i < _strips.Length; i++)
        {
            float bottom = i == _strips.Length - 1
                ? 1f
                : (i + 1) * stripHeight + Random.Range(-jitter, jitter);
            _strips[i] = new Rect(0f, top, 1f, bottom - top);
            _offsets[i] = Random.Range(minOffset, maxHorizontalOffset) * (i % 2 == 0 ? -1f : 1f);
            top = bottom;
        }
    }

    public void SetBlackAlpha(float value)
    {
        BlackAlpha = Mathf.Clamp01(value);
        _coverVisible = BlackAlpha > 0f;
    }

    public void HideSnapshot()
    {
        _showSnapshot = false;
        ReleaseSnapshot();
    }

    public void Clear()
    {
        HideSnapshot();
        SetBlackAlpha(0f);
    }

    private void OnGUI()
    {
        if (!_showSnapshot && !_coverVisible)
            return;

        GUI.depth = int.MinValue;
        if (Event.current.type != EventType.Repaint)
        {
            if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel)
                Event.current.Use();
            return;
        }

        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        var screen = new Rect(0f, 0f, Screen.width, Screen.height);
        if (_showSnapshot && _snapshot != null)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = Color.white;
            for (int i = 0; i < 4; i++)
            {
                Rect source = _strips[i];
                var strip = new Rect((source.x + _offsets[i]) * screen.width,
                    source.y * screen.height, source.width * screen.width, source.height * screen.height);
                GUI.DrawTextureWithTexCoords(strip, _snapshot,
                    new Rect(source.x, 1f - source.yMax, source.width, source.height), false);
            }
        }
        if (_coverVisible)
        {
            GUI.color = new Color(0f, 0f, 0f, BlackAlpha);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
        }
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    private void ReleaseSnapshot()
    {
        if (_snapshot == null)
            return;
        _snapshot.Release();
        Destroy(_snapshot);
        _snapshot = null;
    }

    private void OnDisable() => Clear();

    private struct CanvasState
    {
        private Canvas _canvas;
        private Camera _camera;
        private float _distance;
        private Vector3 _position, _scale;
        private Quaternion _rotation;
        private Vector2 _size;

        public static CanvasState Include(Canvas canvas, Camera camera)
        {
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return default;
            var rect = (RectTransform)canvas.transform;
            var state = new CanvasState
            {
                _canvas = canvas, _camera = canvas.worldCamera, _distance = canvas.planeDistance,
                _position = rect.localPosition, _scale = rect.localScale,
                _rotation = rect.localRotation, _size = rect.sizeDelta
            };
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;
            return state;
        }

        public void Restore()
        {
            if (_canvas == null)
                return;
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = _camera;
            _canvas.planeDistance = _distance;
            var rect = (RectTransform)_canvas.transform;
            rect.localPosition = _position;
            rect.localRotation = _rotation;
            rect.localScale = _scale;
            rect.sizeDelta = _size;
        }
    }
}
