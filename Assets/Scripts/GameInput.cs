using UnityEngine;

public static class GameInput
{
    private static float _mouseRotation;

    public static Vector3 MousePosition
    {
        get
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
    }

    public static void SetMouseRotation(float degrees)
    {
        _mouseRotation = degrees;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _mouseRotation = 0f;
    }
}
