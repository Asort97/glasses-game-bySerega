using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum MiniTutorialType
{
    None,
    KeyboardLeftRight,
    KeyboardLeftRightSpace,
    KeyboardFourDirections,
    KeyboardFourDirectionsSpace,
    MouseMove,
    MouseClick,
    MouseMoveAndClick,
    MouseDrag,
    MouseSlice,
    KeyboardSpaceOnly,
    MouseStill
}

public class MiniTutorialController : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image primaryImage;
    [SerializeField] private Image spaceImage;

    [Header("Keyboard Sprites")]
    [SerializeField] private Sprite keyboardLeft;
    [SerializeField] private Sprite keyboardRight;
    [SerializeField] private Sprite keyboardUp;
    [SerializeField] private Sprite keyboardDown;
    [SerializeField] private Sprite space;
    [SerializeField] private Sprite spaceBlink;

    [Header("Mouse Sprites")]
    [SerializeField] private Sprite mouse;
    [SerializeField] private Sprite mouseBlink;
    [SerializeField] private Sprite mouseMoving;
    [SerializeField] private Sprite mouseMovingClick;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float frameInterval = 0.3f;
    [FormerlySerializedAs("mouseMoveDistance")]
    [SerializeField, Min(0f)] private float mouseOrbitRadius = 35f;
    [SerializeField, Min(0.1f)] private float mouseOrbitDuration = 2f;
    [SerializeField] private Vector2 spaceOnlyPosition = new Vector2(115f, -33f);

    [Header("Recovery")]
    [SerializeField] private GameObject recoverySpaceParticlePrefab;
    [SerializeField] private GameObject recoveryMouseParticlePrefab;
    [SerializeField, Min(0f)] private float recoveryParticleShowDelay = 2f;
    [SerializeField, Min(0.01f)] private float recoveryParticleFadeDuration = 0.5f;

    private Coroutine _animationRoutine;
    private Coroutine _particleFadeRoutine;
    private GameObject _recoveryParticle;
    private Vector2 _primaryStartPosition;
    private Vector2 _spaceStartPosition;
    private bool _initialized;

    private void Awake()
    {
        EnsureInitialized();
        Hide();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (primaryImage != null)
            _primaryStartPosition = primaryImage.rectTransform.anchoredPosition;
        if (spaceImage != null)
            _spaceStartPosition = spaceImage.rectTransform.anchoredPosition;

        _initialized = true;
    }

    public void Show(MiniTutorialType type)
    {
        Show(type, frameInterval * 2f);
    }

    public void Show(MiniTutorialType type, float customFrameInterval)
    {
        EnsureInitialized();
        Hide();

        if (type == MiniTutorialType.None)
            return;

        bool usesPrimaryImage = type != MiniTutorialType.KeyboardSpaceOnly;
        if (usesPrimaryImage && primaryImage == null)
            return;

        if (primaryImage != null)
            primaryImage.gameObject.SetActive(usesPrimaryImage);

        if (spaceImage != null)
        {
            spaceImage.rectTransform.anchoredPosition =
                type == MiniTutorialType.KeyboardSpaceOnly ? spaceOnlyPosition : _spaceStartPosition;
        }

        _animationRoutine = StartCoroutine(Animate(type, customFrameInterval));
    }

    public void ShowRecoveryInput(MiniTutorialType type, float blinkInterval)
    {
        GameObject particlePrefab;
        Transform particleAnchor;

        if (type == MiniTutorialType.KeyboardSpaceOnly)
        {
            Show(type, blinkInterval * 2f);
            particlePrefab = recoverySpaceParticlePrefab;
            particleAnchor = spaceImage != null ? spaceImage.transform : null;
        }
        else if (type == MiniTutorialType.MouseClick)
        {
            Show(type, blinkInterval);
            particlePrefab = recoveryMouseParticlePrefab;
            particleAnchor = primaryImage != null ? primaryImage.transform : null;
        }
        else
        {
            return;
        }

        if (particlePrefab == null || particleAnchor == null)
            return;

        Vector3 position = particleAnchor.position;
        position.z -= 0.05f;
        _recoveryParticle = Instantiate(
            particlePrefab,
            position,
            particlePrefab.transform.rotation);

        ParticleSystem particleSystem = _recoveryParticle.GetComponent<ParticleSystem>();
        if (particleSystem != null)
            _particleFadeRoutine = StartCoroutine(FadeInRecoveryParticle(particleSystem));
    }

    public void Hide()
    {
        EnsureInitialized();

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        if (primaryImage != null)
        {
            primaryImage.rectTransform.anchoredPosition = _primaryStartPosition;
            primaryImage.gameObject.SetActive(false);
        }

        if (spaceImage != null)
        {
            spaceImage.rectTransform.anchoredPosition = _spaceStartPosition;
            spaceImage.gameObject.SetActive(false);
        }

        if (_recoveryParticle != null)
        {
            if (_particleFadeRoutine != null)
            {
                StopCoroutine(_particleFadeRoutine);
                _particleFadeRoutine = null;
            }

            Destroy(_recoveryParticle);
            _recoveryParticle = null;
        }

    }

    private IEnumerator FadeInRecoveryParticle(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        Color originalColor = main.startColor.color;
        Color transparentColor = originalColor;
        transparentColor.a = 0f;
        main.startColor = transparentColor;

        if (recoveryParticleShowDelay > 0f)
            yield return new WaitForSecondsRealtime(recoveryParticleShowDelay);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, recoveryParticleFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = originalColor;
            color.a *= Mathf.Clamp01(elapsed / duration);
            main.startColor = color;
            yield return null;
        }

        main.startColor = originalColor;
        _particleFadeRoutine = null;
    }

    private IEnumerator Animate(MiniTutorialType type, float customFrameInterval)
    {
        float interval = Mathf.Max(0.05f, customFrameInterval);
        float spaceInterval = interval * 0.5f;
        Sprite[] frames = GetPrimaryFrames(type);
        bool usesSpace = UsesSpace(type);
        bool usesMouse = UsesMouse(type);
        bool movesMouse = UsesMouseMovement(type);
        bool blinksMouse = UsesMouseClick(type);
        int frameIndex = 0;
        bool primaryBlinkState = false;
        bool spaceBlinkState = false;
        float primaryElapsed = interval;
        float spaceElapsed = spaceInterval;
        float orbitElapsed = 0f;

        if (spaceImage != null)
        {
            spaceImage.gameObject.SetActive(usesSpace);
            spaceImage.sprite = space;
        }

        while (true)
        {
            float deltaTime = Time.unscaledDeltaTime;
            primaryElapsed += deltaTime;
            spaceElapsed += deltaTime;

            if (movesMouse)
            {
                orbitElapsed += deltaTime;
                float angle = orbitElapsed / Mathf.Max(0.1f, mouseOrbitDuration) * Mathf.PI * 2f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * mouseOrbitRadius;
                primaryImage.rectTransform.anchoredPosition = _primaryStartPosition + offset;
            }

            if (primaryElapsed >= interval)
            {
                primaryElapsed %= interval;

                if (frames.Length > 0)
                {
                    primaryImage.sprite = frames[frameIndex];
                    frameIndex = (frameIndex + 1) % frames.Length;
                }

                primaryBlinkState = !primaryBlinkState;

                if (usesMouse)
                {
                    Sprite normalSprite = movesMouse && mouseMoving != null ? mouseMoving : mouse;
                    Sprite clickSprite = movesMouse && mouseMovingClick != null ? mouseMovingClick : mouseBlink;
                    primaryImage.sprite = blinksMouse && primaryBlinkState ? clickSprite : normalSprite;
                }
            }

            if (spaceElapsed >= spaceInterval)
            {
                spaceElapsed %= spaceInterval;

                if (usesSpace && spaceImage != null)
                {
                    spaceBlinkState = !spaceBlinkState;
                    spaceImage.sprite = spaceBlinkState ? spaceBlink : space;
                }
            }

            yield return null;
        }
    }

    private Sprite[] GetPrimaryFrames(MiniTutorialType type)
    {
        switch (type)
        {
            case MiniTutorialType.KeyboardLeftRight:
            case MiniTutorialType.KeyboardLeftRightSpace:
                return new[] { keyboardLeft, keyboardRight };

            case MiniTutorialType.KeyboardFourDirections:
            case MiniTutorialType.KeyboardFourDirectionsSpace:
                return new[] { keyboardLeft, keyboardUp, keyboardRight, keyboardDown };

            default:
                return new Sprite[0];
        }
    }

    private static bool UsesSpace(MiniTutorialType type)
    {
        return type == MiniTutorialType.KeyboardLeftRightSpace ||
               type == MiniTutorialType.KeyboardFourDirectionsSpace ||
               type == MiniTutorialType.KeyboardSpaceOnly;
    }

    private static bool UsesMouseMovement(MiniTutorialType type)
    {
        return type == MiniTutorialType.MouseMove ||
               type == MiniTutorialType.MouseMoveAndClick ||
               type == MiniTutorialType.MouseDrag ||
               type == MiniTutorialType.MouseSlice;
    }

    private static bool UsesMouse(MiniTutorialType type)
    {
        return type == MiniTutorialType.MouseStill ||
               type == MiniTutorialType.MouseMove ||
               type == MiniTutorialType.MouseClick ||
               type == MiniTutorialType.MouseMoveAndClick ||
               type == MiniTutorialType.MouseDrag ||
               type == MiniTutorialType.MouseSlice;
    }

    private static bool UsesMouseClick(MiniTutorialType type)
    {
        return type == MiniTutorialType.MouseStill ||
               type == MiniTutorialType.MouseClick ||
               type == MiniTutorialType.MouseMoveAndClick ||
               type == MiniTutorialType.MouseDrag;
    }
}
