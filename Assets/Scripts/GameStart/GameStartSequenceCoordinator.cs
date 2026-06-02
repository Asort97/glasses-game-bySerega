using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameStartSequenceCoordinator : MonoBehaviour
{
    private const string RuntimeObjectName = "GameStartSequenceCoordinator";

    [Header("Participants")]
    [SerializeField] private GameStartMinigame[] gameStarts;
    [SerializeField] private int requiredActivations = 2;

    [Header("Menu")]
    [SerializeField] private GameObject[] menuIcons;
    [SerializeField] private string menuIconName = "test icon";

    [Header("Hearts")]
    [SerializeField] private GameObject[] heartsRoots;
    [SerializeField] private string[] heartsRootNames = { "LeftLensHearts", "RightLensHearts" };

    [Header("Timing")]
    [SerializeField] private float menuIconHideDuration = 0.12f;
    [SerializeField] private float menuIconHideInterval = 0.12f;
    [SerializeField] private float heartAppearDuration = 0.22f;
    [SerializeField] private float heartAppearInterval = 0.13f;
    [SerializeField] private float startDelayAfterReady = 0.35f;
    [SerializeField] private float fastAnimationMultiplier = 3.5f;

    [Header("Music Ramp Placeholder")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float musicStartPitch = 1f;
    [SerializeField] private float musicTargetPitch = 1.45f;
    [SerializeField] private float musicRampDuration = 1.5f;

    public float MusicRampProgress => _musicRampProgress;

    private readonly HashSet<GameStartMinigame> _registered = new HashSet<GameStartMinigame>();
    private readonly HashSet<GameStartMinigame> _activated = new HashSet<GameStartMinigame>();
    private readonly Dictionary<Transform, Vector3> _startScales = new Dictionary<Transform, Vector3>();

    private Coroutine _sequenceRoutine;
    private bool _prepared;
    private bool _accelerated;
    private bool _completed;
    private float _musicRampProgress;

    public static GameStartSequenceCoordinator FindOrCreate()
    {
        GameStartSequenceCoordinator coordinator =
            FindFirstObjectByType<GameStartSequenceCoordinator>(FindObjectsInactive.Include);
        if (coordinator != null)
            return coordinator;

        GameObject obj = new GameObject(RuntimeObjectName);
        return obj.AddComponent<GameStartSequenceCoordinator>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public void Register(GameStartMinigame gameStart)
    {
        if (gameStart != null)
            _registered.Add(gameStart);

        ResolveReferences();
        PrepareInitialState();
    }

    public void NotifyActivated(GameStartMinigame gameStart)
    {
        if (_completed)
            return;

        Register(gameStart);

        if (gameStart != null)
            _activated.Add(gameStart);

        if (_activated.Count >= Mathf.Max(1, requiredActivations))
            AccelerateSequence();

        if (_sequenceRoutine == null)
            _sequenceRoutine = StartCoroutine(StartSequenceRoutine());
    }

    private IEnumerator StartSequenceRoutine()
    {
        ResolveReferences();
        PrepareInitialState();

        yield return HideMenuIconsRoutine();
        yield return ShowHeartsRoutine();

        while (_activated.Count < Mathf.Max(1, requiredActivations))
        {
            TickMusicRamp();
            yield return null;
        }

        AccelerateSequence();
        yield return WaitSequence(startDelayAfterReady);

        CompleteGameStarts();
    }

    private IEnumerator HideMenuIconsRoutine()
    {
        if (menuIcons == null)
            yield break;

        foreach (GameObject icon in menuIcons)
        {
            if (icon == null)
                continue;

            icon.SetActive(true);
            yield return AnimateScale(icon.transform, GetStartScale(icon.transform), Vector3.zero, menuIconHideDuration);
            icon.SetActive(false);
            yield return WaitSequence(menuIconHideInterval);
        }
    }

    private IEnumerator ShowHeartsRoutine()
    {
        List<GameObject> hearts = CollectHeartObjects();
        foreach (GameObject heart in hearts)
            SetHeartVisible(heart, false);

        foreach (GameObject root in heartsRoots)
        {
            if (root != null)
                root.SetActive(true);
        }

        foreach (GameObject heart in hearts)
        {
            if (heart == null)
                continue;

            heart.SetActive(true);
            yield return AnimateHeartAppear(heart);
            yield return WaitSequence(heartAppearInterval);
        }
    }

    private IEnumerator AnimateHeartAppear(GameObject heart)
    {
        Transform heartTransform = heart.transform;
        Vector3 startScale = GetStartScale(heartTransform);
        SetGraphicsAlpha(heart, 0f);
        heartTransform.localScale = startScale * 0.72f;

        float elapsed = 0f;
        while (elapsed < heartAppearDuration)
        {
            elapsed += Time.deltaTime * GetSequenceSpeed();
            float t = Mathf.Clamp01(elapsed / Mathf.Max(heartAppearDuration, 0.0001f));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float blink = 0.72f + Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 0.28f;

            heartTransform.localScale = Vector3.LerpUnclamped(startScale * 0.72f, startScale, eased);
            SetGraphicsAlpha(heart, Mathf.Clamp01(eased * blink));
            TickMusicRamp();
            yield return null;
        }

        heartTransform.localScale = startScale;
        SetGraphicsAlpha(heart, 1f);
    }

    private IEnumerator AnimateScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * GetSequenceSpeed();
            float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            target.localScale = Vector3.LerpUnclamped(from, to, t * t);
            TickMusicRamp();
            yield return null;
        }

        target.localScale = to;
    }

    private IEnumerator WaitSequence(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime * GetSequenceSpeed();
            TickMusicRamp();
            yield return null;
        }
    }

    private void AccelerateSequence()
    {
        _accelerated = true;
    }

    private float GetSequenceSpeed()
    {
        return _accelerated ? Mathf.Max(1f, fastAnimationMultiplier) : 1f;
    }

    private void TickMusicRamp()
    {
        float duration = Mathf.Max(0.0001f, musicRampDuration);
        _musicRampProgress = Mathf.MoveTowards(
            _musicRampProgress,
            1f,
            Time.deltaTime * GetSequenceSpeed() / duration);

        if (musicSource != null)
            musicSource.pitch = Mathf.Lerp(musicStartPitch, musicTargetPitch, _musicRampProgress);
    }

    private void CompleteGameStarts()
    {
        if (_completed)
            return;

        _completed = true;

        foreach (GameStartMinigame gameStart in GetKnownGameStarts())
        {
            if (gameStart != null)
                gameStart.CompleteFromStartSequence();
        }
    }

    private void PrepareInitialState()
    {
        if (_prepared || _completed)
            return;

        _prepared = true;
        _accelerated = false;
        _musicRampProgress = 0f;

        if (musicSource != null)
            musicSource.pitch = musicStartPitch;

        if (menuIcons != null)
        {
            foreach (GameObject icon in menuIcons)
            {
                if (icon == null)
                    continue;

                icon.SetActive(true);
                icon.transform.localScale = GetStartScale(icon.transform);
                SetGraphicsAlpha(icon, 1f);
            }
        }

        foreach (GameObject root in heartsRoots)
        {
            if (root != null)
                root.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (gameStarts == null || gameStarts.Length == 0)
            gameStarts = FindObjectsByType<GameStartMinigame>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (menuIcons == null || menuIcons.Length == 0)
        {
            GameObject icon = FindSceneObject(menuIconName);
            if (icon != null)
                menuIcons = new[] { icon };
        }

        if ((heartsRoots == null || heartsRoots.Length == 0) && heartsRootNames != null)
        {
            List<GameObject> roots = new List<GameObject>(heartsRootNames.Length);
            foreach (string rootName in heartsRootNames)
            {
                GameObject root = FindSceneObject(rootName);
                if (root != null)
                    roots.Add(root);
            }

            heartsRoots = roots.ToArray();
        }
    }

    private IEnumerable<GameStartMinigame> GetKnownGameStarts()
    {
        HashSet<GameStartMinigame> known = new HashSet<GameStartMinigame>();

        if (gameStarts != null)
        {
            foreach (GameStartMinigame gameStart in gameStarts)
            {
                if (gameStart != null && known.Add(gameStart))
                    yield return gameStart;
            }
        }

        foreach (GameStartMinigame gameStart in _registered)
        {
            if (gameStart != null && known.Add(gameStart))
                yield return gameStart;
        }
    }

    private List<GameObject> CollectHeartObjects()
    {
        List<GameObject> hearts = new List<GameObject>();
        if (heartsRoots == null)
            return hearts;

        foreach (GameObject root in heartsRoots)
        {
            if (root == null)
                continue;

            HeartWidget[] widgets = root.GetComponentsInChildren<HeartWidget>(true);
            if (widgets.Length > 0)
            {
                foreach (HeartWidget widget in widgets)
                    hearts.Add(widget.gameObject);
                continue;
            }

            for (int i = 0; i < root.transform.childCount; i++)
                hearts.Add(root.transform.GetChild(i).gameObject);
        }

        return hearts;
    }

    private void SetHeartVisible(GameObject heart, bool visible)
    {
        if (heart == null)
            return;

        heart.SetActive(visible);
        if (visible)
            SetGraphicsAlpha(heart, 1f);
    }

    private Vector3 GetStartScale(Transform target)
    {
        if (target == null)
            return Vector3.one;

        if (!_startScales.TryGetValue(target, out Vector3 scale))
        {
            scale = target.localScale;
            _startScales.Add(target, scale);
        }

        return scale;
    }

    private static void SetGraphicsAlpha(GameObject obj, float alpha)
    {
        if (obj == null)
            return;

        Graphic[] graphics = obj.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == objectName && obj.scene.IsValid())
                return obj;
        }

        return null;
    }
}
