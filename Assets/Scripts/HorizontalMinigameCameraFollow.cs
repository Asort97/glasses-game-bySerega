using UnityEngine;

public sealed class HorizontalMinigameCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform gameCamera;
    [SerializeField] private Transform timerRoot;
    [Min(0f)] [SerializeField] private float followSharpness = 8f;

    private Vector3 _cameraStart;
    private Vector3 _timerStart;
    private float _playerStartX;
    private bool _following;

    public void BeginFollowing()
    {
        StopFollowing();
        _cameraStart = gameCamera.localPosition;
        if (timerRoot != null) _timerStart = timerRoot.position;
        _playerStartX = target.position.x;
        _following = true;
    }

    public void StopFollowing()
    {
        if (!_following) return;
        _following = false;
        if (gameCamera != null) gameCamera.localPosition = _cameraStart;
        if (timerRoot != null) timerRoot.position = _timerStart;
    }

    private void LateUpdate()
    {
        if (!_following) return;
        float wantedOffset = Mathf.Max(0f, target.position.x - _playerStartX);
        Vector3 origin = gameCamera.parent != null
            ? gameCamera.parent.TransformPoint(_cameraStart) : _cameraStart;
        Vector3 position = gameCamera.position;
        position.x = Mathf.Lerp(position.x, origin.x + wantedOffset,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        gameCamera.position = position;
        if (timerRoot != null)
            timerRoot.position = _timerStart + Vector3.right * (position.x - origin.x);
    }

    private void OnDisable() => StopFollowing();
}
