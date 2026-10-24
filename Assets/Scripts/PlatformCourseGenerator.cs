using System.Collections.Generic;
using UnityEngine;

public sealed class PlatformCourseGenerator : MonoBehaviour
{
    [SerializeField] private BoxCollider2D platformTemplate;
    [SerializeField] private BoxCollider2D finishWall;
    [Min(2)] [SerializeField] private int platformCount = 5;
    [SerializeField] private Vector2 widthRange = new Vector2(1.6f, 2.6f);
    [SerializeField] private Vector2 gapRange = new Vector2(0.7f, 1.1f);

    private readonly List<BoxCollider2D> _platforms = new List<BoxCollider2D>();
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private bool _initialized;

    public void Generate()
    {
        if (!_initialized)
        {
            _startPosition = platformTemplate.transform.localPosition;
            _startScale = platformTemplate.transform.localScale;
            _platforms.Add(platformTemplate);
            _initialized = true;
        }

        int count = Mathf.Max(2, platformCount);
        while (_platforms.Count < count)
        {
            var platform = Instantiate(platformTemplate, platformTemplate.transform.parent);
            platform.name = "Platform " + (_platforms.Count + 1);
            _platforms.Add(platform);
        }

        float rightEdge = 0f;
        float previousWidth = 0f;
        for (int i = 0; i < _platforms.Count; i++)
        {
            BoxCollider2D platform = _platforms[i];
            platform.gameObject.SetActive(i < count);
            if (i >= count) continue;

            float minWidth = Mathf.Max(0.1f, Mathf.Min(widthRange.x, widthRange.y));
            float maxWidth = Mathf.Max(minWidth, Mathf.Max(widthRange.x, widthRange.y));
            float width = Random.Range(minWidth, maxWidth);
            // Avoid adjacent platforms that look identical when the range allows variation.
            if (i > 0 && maxWidth - minWidth > 0.2f && Mathf.Abs(width - previousWidth) < 0.1f)
                width = previousWidth < (minWidth + maxWidth) * 0.5f ? maxWidth : minWidth;
            previousWidth = width;

            Vector3 scale = _startScale;
            scale.x = width / Mathf.Max(0.01f, platform.size.x);
            platform.transform.localScale = scale;
            Vector3 position = _startPosition;
            float offset = platform.offset.x * scale.x;
            if (i > 0)
                position.x = rightEdge + RandomRange(gapRange) + width * 0.5f - offset;
            platform.transform.localPosition = position;
            rightEdge = position.x + offset + width * 0.5f;
        }

        Physics2D.SyncTransforms();
        BoxCollider2D last = _platforms[count - 1];
        finishWall.gameObject.SetActive(true);
        Physics2D.SyncTransforms();
        Vector3 wallPosition = finishWall.transform.position;
        Vector3 centerOffset = finishWall.bounds.center - wallPosition;
        wallPosition.x = last.bounds.max.x - finishWall.bounds.extents.x - centerOffset.x;
        wallPosition.y = last.bounds.max.y + finishWall.bounds.extents.y - centerOffset.y;
        finishWall.transform.position = wallPosition;
        Physics2D.SyncTransforms();
    }

    private static float RandomRange(Vector2 range)
    {
        float min = Mathf.Max(0.1f, Mathf.Min(range.x, range.y));
        return Random.Range(min, Mathf.Max(min, Mathf.Max(range.x, range.y)));
    }
}
