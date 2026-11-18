using UnityEngine;
using UnityEngine.UI;

public sealed class LensSkipView : MonoBehaviour
{
    [SerializeField] private RectTransform rewardsRoot;
    [SerializeField] private GameObject[] plusSigns;
    [SerializeField] private Image timerFill;
    [SerializeField] private RectTransform thresholdMarker;
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Renderer lensRenderer;
    [SerializeField] private Renderer otherLensRenderer;
    [SerializeField] private LensSkipEffect effect;
    [SerializeField] private float edgeOffset = 28f;

    public void Refresh(int charges, bool visible, bool showMarker, float remainingThreshold)
    {
        for (int i = 0; i < plusSigns.Length; i++)
            if (plusSigns[i]) plusSigns[i].SetActive(i < charges);
        if (rewardsRoot) rewardsRoot.gameObject.SetActive(visible && charges > 0);
        if (thresholdMarker)
        {
            thresholdMarker.gameObject.SetActive(showMarker);
            float x = timerFill.fillOrigin == (int)Image.OriginHorizontal.Right
                ? 1f - remainingThreshold : remainingThreshold;
            thresholdMarker.anchorMin = new Vector2(x, 0f);
            thresholdMarker.anchorMax = new Vector2(x, 1f);
            thresholdMarker.anchoredPosition = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!rewardsRoot || !rewardsRoot.gameObject.activeSelf || !screenCamera || !lensRenderer) return;
        Renderer displayed = otherLensRenderer && effect && effect.LensMaterial
            && otherLensRenderer.sharedMaterial == effect.LensMaterial ? otherLensRenderer : lensRenderer;
        Bounds bounds = displayed.bounds;
        Vector3 center = screenCamera.WorldToScreenPoint(bounds.center);
        float minX = center.x, maxX = center.x;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            float x = screenCamera.WorldToScreenPoint(corner).x;
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
        }
        float edge = center.x < Screen.width * 0.5f ? minX - edgeOffset : maxX + edgeOffset;
        Vector2 screen = new Vector2(Mathf.Clamp(edge, 32f, Screen.width - 32f),
            Mathf.Clamp(center.y, 65f, Screen.height - 65f));
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rewardsRoot.parent, screen, null, out Vector2 local))
            rewardsRoot.anchoredPosition = local;
    }

    public void Hide()
    {
        if (rewardsRoot) rewardsRoot.gameObject.SetActive(false);
        if (thresholdMarker) thresholdMarker.gameObject.SetActive(false);
    }
}
