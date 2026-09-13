using UnityEngine;

public static class GameInput
{
    private static float _mouseRotation;
    private static bool _lensSidesSwapped;
    private static Vector2 _lensSwapOffset;
    private static float _lensSwapBoundaryX;

    public static Vector3 CursorPosition => GetRotatedMousePosition();

    public static Vector3 MousePosition
    {
        get
        {
            Vector3 position = GetRotatedMousePosition();
            if (!_lensSidesSwapped)
                return position;

            Vector2 direction = position.x < _lensSwapBoundaryX
                ? _lensSwapOffset
                : -_lensSwapOffset;

            return position + (Vector3)direction;
        }
    }

    public static void SetMouseRotation(float degrees)
    {
        _mouseRotation = degrees;
    }

    public static void SetLensSidesSwapped(Vector2 leftLensCenter, Vector2 rightLensCenter)
    {
        _lensSwapOffset = rightLensCenter - leftLensCenter;
        _lensSwapBoundaryX = (leftLensCenter.x + rightLensCenter.x) * 0.5f;
        _lensSidesSwapped = true;
    }

    public static void ResetLensSidesSwapped()
    {
        _lensSidesSwapped = false;
    }

    private static Vector3 GetRotatedMousePosition()
    {
        Vector3 position = Input.mousePosition;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 offset = (Vector2)position - center;
        float radians = _mouseRotation * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        Vector2 rotated = new Vector2(
            offset.x * cos - offset.y * sin,
            offset.x * sin + offset.y * cos);

        return new Vector3(
            center.x + rotated.x,
            center.y + rotated.y,
            position.z);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _mouseRotation = 0f;
        _lensSidesSwapped = false;
        _lensSwapOffset = Vector2.zero;
        _lensSwapBoundaryX = 0f;
    }
}
