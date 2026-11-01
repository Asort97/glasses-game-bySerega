using UnityEngine;

public sealed class ClockGridLayout : MonoBehaviour
{
    [SerializeField] private Vector2 areaSize = new Vector2(8.4f, 5.8f);
    [Min(0f)] [SerializeField] private float gap = 0.6f;
    [SerializeField] private Vector2 center = Vector2.zero;

    public bool Place(ClockDial[] clocks)
    {
        if (clocks == null || clocks.Length == 0) return false;
        Vector2[] extents = new Vector2[clocks.Length];
        Vector3[] offsets = new Vector3[clocks.Length];
        Vector3[] positions = new Vector3[clocks.Length];
        for (int i = 0; i < clocks.Length; i++)
        {
            Bounds bounds = clocks[i].FaceBounds;
            Vector3 min = transform.InverseTransformPoint(bounds.min);
            Vector3 max = transform.InverseTransformPoint(bounds.max);
            extents[i] = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)) * 0.5f;
            offsets[i] = transform.InverseTransformPoint(bounds.center)
                - transform.InverseTransformPoint(clocks[i].transform.position);
            if (areaSize.x < extents[i].x * 2f || areaSize.y < extents[i].y * 2f) return false;
        }

        // Retry the entire arrangement if an early placement blocks the remaining dials.
        // Never fall back to overlapping positions or a visible grid.
        for (int layoutAttempt = 0; layoutAttempt < 64; layoutAttempt++)
        {
            bool complete = true;
            for (int i = 0; i < clocks.Length; i++)
            {
                bool placed = false;
                Vector2 range = areaSize * 0.5f - extents[i];
                for (int attempt = 0; attempt < 128; attempt++)
                {
                    Vector3 candidate = transform.InverseTransformPoint(clocks[i].FaceBounds.center);
                    candidate.x = center.x + Random.Range(-range.x, range.x);
                    candidate.y = center.y + Random.Range(-range.y, range.y);
                    bool clear = true;
                    for (int j = 0; j < i; j++)
                    {
                        float separation = Mathf.Max(extents[i].x, extents[i].y)
                            + Mathf.Max(extents[j].x, extents[j].y) + Mathf.Max(0f, gap);
                        if (Vector2.Distance(candidate, positions[j]) < separation)
                        {
                            clear = false;
                            break;
                        }
                    }
                    if (!clear) continue;
                    positions[i] = candidate;
                    placed = true;
                    break;
                }
                if (placed) continue;
                complete = false;
                break;
            }
            if (!complete || !TryCenterGroup(positions, extents)) continue;
            for (int i = 0; i < clocks.Length; i++)
                clocks[i].transform.position = transform.TransformPoint(positions[i] - offsets[i]);
            return true;
        }
        return false;
    }

    private bool TryCenterGroup(Vector3[] positions, Vector2[] extents)
    {
        Vector2 groupCenter = Vector2.zero;
        foreach (Vector3 position in positions) groupCenter += (Vector2)position;
        groupCenter /= positions.Length;
        Vector2 shift = center - groupCenter;
        Vector2 halfArea = areaSize * 0.5f;

        // Translate the whole group; reject layouts that no longer fit after centering.
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] += (Vector3)shift;
            Vector2 relative = (Vector2)positions[i] - center;
            if (Mathf.Abs(relative.x) + extents[i].x > halfArea.x ||
                Mathf.Abs(relative.y) + extents[i].y > halfArea.y)
                return false;
        }
        return true;
    }
}
