using UnityEngine;

public class CanvasCursor : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private bool hideSystemCursor = true;

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
                Input.mousePosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            cursor.anchoredPosition = localPosition;
        }
    }
}
