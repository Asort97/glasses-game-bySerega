using UnityEngine;
using UnityEngine.UI;

public class CanvasCursor : MonoBehaviour
{
    private const float LensRayDistance = 1000f;

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
    private bool _showOutsideRightLensWhenHidden;

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

    public void SetShowOutsideRightLensWhenHidden(bool active)
    {
        _showOutsideRightLensWhenHidden = active;
        UpdateCursorAppearance();
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

        bool isOverRightLens = IsPointerOverRightLens();

        if (!_appearanceInitialized || _isOverRightLens != isOverRightLens)
        {
            _appearanceInitialized = true;
            _isOverRightLens = isOverRightLens;
            cursorImage.sprite = _isOverRightLens ? rightLensCursor : defaultCursor;
            cursorImage.color = _isOverRightLens ? rightLensColor : defaultCursorColor;
        }

        ApplyCursorVisibility();
    }

    private bool IsPointerOverRightLens()
    {
        Ray ray = mainCamera.ScreenPointToRay(GameInput.CursorPosition);
        if (rightLensCollider.Raycast(ray, out _, LensRayDistance))
            return true;

        // MeshCollider can face away from the camera. Cast back through the same
        // screen point so the visible lens is detected regardless of winding.
        Ray reverseRay = new Ray(ray.GetPoint(LensRayDistance), -ray.direction);
        return rightLensCollider.Raycast(reverseRay, out _, LensRayDistance);
    }

    private void ApplyCursorVisibility()
    {
        if (cursorImage != null)
        {
            if (_showOutsideRightLensWhenHidden && _isOverRightLens)
            {
                cursorImage.enabled = false;
                return;
            }

            bool showOutsideLens = _showOutsideRightLensWhenHidden && !_isOverRightLens;
            cursorImage.enabled = _spriteVisible || _uiInteractionOverride || showOutsideLens;
        }
    }
}
