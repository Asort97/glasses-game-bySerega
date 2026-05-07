using UnityEngine;

/// <summary>
/// Тонкий форвардер событий коллизий/триггеров.
/// Позволяет внешним MonoBehaviour подписываться на физические события
/// без прямого наследования или модификации целевого скрипта.
/// </summary>
public class ContactReporter : MonoBehaviour
{
    public event System.Action<Collider2D>  OnTriggerEntered;
    public event System.Action<Collision2D> OnCollisionEntered;

    private void OnTriggerEnter2D(Collider2D other)    => OnTriggerEntered?.Invoke(other);
    private void OnCollisionEnter2D(Collision2D other) => OnCollisionEntered?.Invoke(other);
}
