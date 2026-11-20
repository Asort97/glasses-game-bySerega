using UnityEngine;

public sealed class LensSkipEffect : MonoBehaviour
{
    [SerializeField] private Renderer lensRenderer;
    [SerializeField] private QuarteredScreenCapture capture;
    [SerializeField] private RealityAudioFreeze audioFreeze;
    [SerializeField] private Shader sliceShader;
    [Min(0.01f)] [SerializeField] private float duration = 0.2f;

    private Material _slices;
    private const int PatternChanges = 3;
    private readonly float[] _changeTimes = new float[PatternChanges];
    private RenderTexture _sourceFrame;
    private float _startedAt;
    private int _seed, _pattern;
    private RenderTexture _frozenHalf;
    private bool _left;
    public float Duration => duration;
    public Material LensMaterial { get; private set; }
    public bool IsConfigured => capture && audioFreeze && _slices;

    private void Awake()
    {
        if (lensRenderer) LensMaterial = lensRenderer.material;
        if (sliceShader) _slices = new Material(sliceShader);
    }

    public bool TryCapture(bool left)
    {
        ResetEffect();
        _left = left;
        int w = Mathf.Max(2, Screen.width), h = Mathf.Max(1, Screen.height);
        var previous = RenderTexture.active;
        try
        {
            _sourceFrame = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            audioFreeze.SetEarFreeze(left, true);
            capture.RenderFrame(_sourceFrame);
            _frozenHalf = RenderTexture.GetTemporary((w + 1) / 2, h, 0, RenderTextureFormat.ARGB32);
            _slices.SetFloat("_StartX", left ? 0f : 0.5f);
            _seed = Random.Range(0, 10000);
            ScheduleChanges();
            DrawPattern(0);
            _startedAt = Time.unscaledTime;
            return true;
        }
        catch (System.Exception error)
        {
            ResetEffect();
            Debug.LogException(error, this);
            return false;
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    private void ScheduleChanges()
    {
        int longHold = Random.Range(1, PatternChanges);
        float total = 0f;
        for (int i = 0; i <= PatternChanges; i++)
        {
            total += i == longHold ? Random.Range(2.8f, 3.1f) : Random.Range(1f, 1.25f);
            if (i < PatternChanges) _changeTimes[i] = total;
        }
        for (int i = 0; i < PatternChanges; i++)
            _changeTimes[i] *= duration / total;
    }

    private void LateUpdate()
    {
        if (!_frozenHalf) return;
        if (_pattern < PatternChanges && Time.unscaledTime - _startedAt >= _changeTimes[_pattern])
            DrawPattern(_pattern + 1);
    }

    private void DrawPattern(int pattern)
    {
        _pattern = pattern;
        _slices.SetFloat("_Seed", _seed + pattern * 31);
        var previous = RenderTexture.active;
        try { Graphics.Blit(_sourceFrame, _frozenHalf, _slices); }
        finally { RenderTexture.active = previous; }
    }

    private void OnGUI()
    {
        if (!_frozenHalf || Event.current.type != EventType.Repaint) return;
        GUI.depth = int.MinValue + 1;
        var color = GUI.color;
        var matrix = GUI.matrix;
        GUI.color = Color.white;
        GUI.matrix = Matrix4x4.identity;
        int split = Screen.width / 2;
        GUI.DrawTexture(new Rect(_left ? 0 : split, 0, _left ? split : Screen.width - split, Screen.height),
            _frozenHalf, ScaleMode.StretchToFill, false);
        GUI.matrix = matrix;
        GUI.color = color;
    }

    public void ResetEffect()
    {
        if (_sourceFrame && audioFreeze) audioFreeze.SetEarFreeze(_left, false);
        if (_sourceFrame) RenderTexture.ReleaseTemporary(_sourceFrame);
        if (_frozenHalf) RenderTexture.ReleaseTemporary(_frozenHalf);
        _sourceFrame = null;
        _frozenHalf = null;
    }

    private void OnDisable() => ResetEffect();
    private void OnDestroy() { if (_slices) Destroy(_slices); }
}
