using System.Collections;
using UnityEngine;
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
    KeyboardSpaceOnly
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
    [SerializeField, Min(0f)] private float mouseMoveDistance = 35f;
    [SerializeField] private Vector2 spaceOnlyPosition = new Vector2(115f, -33f);

    private Coroutine _animationRoutine;
    private Vector2 _primaryStartPosition;
    private Vector2 _spaceStartPosition;

    private void Awake()
    {
        if (primaryImage != null)
            _primaryStartPosition = primaryImage.rectTransform.anchoredPosition;
        if (spaceImage != null)
            _spaceStartPosition = spaceImage.rectTransform.anchoredPosition;

        Hide();
    }

    public void Show(MiniTutorialType type)
    {
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

        _animationRoutine = StartCoroutine(Animate(type));
    }

    public void Hide()
    {
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

    }

    private IEnumerator Animate(MiniTutorialType type)
    {
        Sprite[] frames = GetPrimaryFrames(type);
        bool usesSpace = UsesSpace(type);
        bool usesMouse = UsesMouse(type);
        bool movesMouse = UsesMouseMovement(type);
        bool blinksMouse = UsesMouseClick(type);
        int frameIndex = 0;
        bool blinkState = false;

        if (spaceImage != null)
        {
            spaceImage.gameObject.SetActive(usesSpace);
            spaceImage.sprite = space;
        }

        while (true)
        {
            if (frames.Length > 0)
            {
                primaryImage.sprite = frames[frameIndex];
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            blinkState = !blinkState;

            if (usesSpace && spaceImage != null)
                spaceImage.sprite = blinkState ? spaceBlink : space;

            if (usesMouse)
            {
                Sprite normalSprite = movesMouse && mouseMoving != null ? mouseMoving : mouse;
                Sprite clickSprite = movesMouse && mouseMovingClick != null ? mouseMovingClick : mouseBlink;
                primaryImage.sprite = blinksMouse && blinkState ? clickSprite : normalSprite;
            }

            if (movesMouse)
            {
                float offset = blinkState ? mouseMoveDistance : -mouseMoveDistance;
                primaryImage.rectTransform.anchoredPosition =
                    _primaryStartPosition + Vector2.right * offset;
            }

            yield return new WaitForSecondsRealtime(frameInterval);
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
        return type == MiniTutorialType.MouseMove ||
               type == MiniTutorialType.MouseClick ||
               type == MiniTutorialType.MouseMoveAndClick ||
               type == MiniTutorialType.MouseDrag ||
               type == MiniTutorialType.MouseSlice;
    }

    private static bool UsesMouseClick(MiniTutorialType type)
    {
        return type == MiniTutorialType.MouseClick ||
               type == MiniTutorialType.MouseMoveAndClick ||
               type == MiniTutorialType.MouseDrag;
    }
}
