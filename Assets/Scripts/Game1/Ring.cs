using UnityEngine;

public class Ring : MonoBehaviour
{
    public event System.Action OnCollected;

    private Animator _animator;

    public void Take()
    {
        _animator = GetComponent<Animator>();
        _animator.SetTrigger("take");
        OnCollected?.Invoke();
    }
    
    public void TakeDestroy()
    {
        gameObject.SetActive(false);
    }
}
