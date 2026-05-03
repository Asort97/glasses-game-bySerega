using UnityEngine;

public class Ring : MonoBehaviour
{
    private Animator _animator;

    public void Take()
    {
        _animator = GetComponent<Animator>();
        _animator.SetTrigger("take");
    }
    
    public void TakeDestroy()
    {
        Destroy(gameObject);
    }
}
