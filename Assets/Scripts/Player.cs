using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpForce;
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        if(Input.GetKeyDown(KeyCode.Space))
        {
            _rigidbody2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _animator.SetTrigger("jump");
        }
    }

}
