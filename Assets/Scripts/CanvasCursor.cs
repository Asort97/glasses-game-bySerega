using UnityEngine;
using UnityEngine.UI;

public class CanvasCursor : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private Sprite rightLensCursor;
    [SerializeField] private Sprite defaultCursor;
    [SerializeField] private Color rightLensColor = Color.white;
    [SerializeField] private Color defaultCursorColor = Color.white;
    [SerializeField] private Image cursorImage;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider rightLensCollider;
    [SerializeField] private bool hideSystemCursor = true;

    private bool _isOverRightLens;
    private bool _appearanceInitialized;
    private bool _spriteVisible = true;
    private bool _uiInteractionOverride;

    private void Awake()
    {
        if (cursorImage != null)
            _spriteVisible = cursorImage.enabled;
    }

    public void SetCursorSpriteVisible(bool visible)
    {
        _spriteVisible = visible;
        ApplyCursorVisibility();
    }

    public void SetUiInteractionOverride(bool active)
    {
        _uiInteractionOverride = active;
        ApplyCursorVisibility();
    }

    private void OnEnable()
    {
        if (hideSystemCursor)
            Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (hideSystemCursor)
            Cursor.visible = true;
    }

    private void Update()
    {
        if (canvas == null || cursor == null)
            return;

        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransform canvasRect = (RectTransform)canvas.transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                GameInput.CursorPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            cursor.anchoredPosition = localPosition;
        }

        UpdateCursorAppearance();
    }

    private void UpdateCursorAppearance()
    {
        if (cursorImage == null || mainCamera == null || rightLensCollider == null)
            return;

        bool isOverRightLens = rightLensCollider.Raycast(
            mainCamera.ScreenPointToRay(GameInput.MousePosition),
            out _,
            Mathf.Infinity);

        if (_appearanceInitialized && _isOverRightLens == isOverRightLens)
            return;

        _appearanceInitialized = true;
        _isOverRightLens = isOverRightLens;
        cursorImage.sprite = _isOverRightLens ? rightLensCursor : defaultCursor;
        cursorImage.color = _isOverRightLens ? rightLensColor : defaultCursorColor;
    }

    private void ApplyCursorVisibility()
    {
        if (cursorImage != null)
            cursorImage.enabled = _spriteVisible || _uiInteractionOverride;
    }
}
