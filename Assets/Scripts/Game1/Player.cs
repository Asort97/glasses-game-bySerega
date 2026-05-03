using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpForce;
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    private bool _canJump;

    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        if( x != 0 || y != 0 )
        {
            _animator.SetBool("running", true);
        }
        else
        {
            _animator.SetBool("running", false);
        }

        transform.position = new Vector2(transform.position.x + (x*movementSpeed*Time.deltaTime) , transform.position.y + (y*movementSpeed*Time.deltaTime));

        if(Input.GetKeyDown(KeyCode.Space) && _canJump)
        {
            _rigidbody2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _animator.SetTrigger("jump");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {   
            _canJump = true;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.TryGetComponent(out Ring ring))
        {
            ring.Take();
        }    
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {   
            _canJump = false;
        }
    }
}
