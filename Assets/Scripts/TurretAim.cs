using UnityEngine;

public sealed class TurretAim : MonoBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float spriteForwardAngle = 180f;
    public Vector3 MuzzlePosition => shootPoint.position;
    public bool IsConfigured => shootPoint != null;

    public void AimAt(Vector3 position)
    {
        Vector2 direction = position - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - spriteForwardAngle);
    }
}
