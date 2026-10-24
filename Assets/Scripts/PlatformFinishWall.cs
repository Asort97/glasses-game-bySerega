using UnityEngine;

public sealed class PlatformFinishWall : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerBody;
    public event System.Action Reached;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == playerBody)
            Reached?.Invoke();
    }
}
