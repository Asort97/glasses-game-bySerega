using System.Collections;
using UnityEngine;

/// <summary>
/// Игрок прыгает между левой и правой стеной по нажатию Пробела.
/// Привяжи к объекту player внутри Walls_minigame.
/// </summary>
public class WallJumper : MonoBehaviour
{
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private float jumpDuration = 0.15f;

    private enum Wall { Left, Right }

    private SpriteRenderer _sr;
    private Animator       _anim;
    private Wall           _currentWall;
    private bool           _jumping;

    // Отступ игрока от центра стены (вычисляется один раз в Awake)
    private float _offsetFromWall;

    private void Awake()
    {
        _sr   = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();

        // Авто-поиск стен если не задано в Inspector
        if (leftWall == null || rightWall == null)
        {
            var wm = GameObject.Find("Walls_minigame");
            if (wm != null)
            {
                if (leftWall  == null) leftWall  = wm.transform.Find("leftWall");
                if (rightWall == null) rightWall = wm.transform.Find("rightWall");
            }
        }

        if (leftWall == null || rightWall == null)
        {
            Debug.LogError("[WallJumper] leftWall or rightWall not found!");
            return;
        }

        // Определяем стартовую стену по близости
        float distLeft  = Mathf.Abs(transform.position.x - leftWall.position.x);
        float distRight = Mathf.Abs(transform.position.x - rightWall.position.x);
        _currentWall = distLeft < distRight ? Wall.Left : Wall.Right;

        // Запоминаем отступ игрока от центра текущей стены
        _offsetFromWall = _currentWall == Wall.Right
            ? Mathf.Abs(rightWall.position.x - transform.position.x)
            : Mathf.Abs(transform.position.x - leftWall.position.x);

        ApplyWallState();
    }

    private void Update()
    {
        if (_jumping) return;

        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(JumpToOtherWall());
    }

    private IEnumerator JumpToOtherWall()
    {
        _jumping = true;
        _anim.SetBool("jump", true);

        Wall  targetWall = _currentWall == Wall.Left ? Wall.Right : Wall.Left;
        // Вычисляем X динамически от текущего положения стен
        float targetX = targetWall == Wall.Left
            ? leftWall.position.x  + _offsetFromWall
            : rightWall.position.x - _offsetFromWall;

        Vector3 startPos = transform.position;
        Vector3 endPos   = new Vector3(targetX, transform.position.y, transform.position.z);

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / jumpDuration);
            yield return null;
        }

        transform.position = endPos;
        _currentWall = targetWall;
        _anim.SetBool("jump", false);
        ApplyWallState();
        _jumping = false;
    }

    private void OnDisable()
    {
        // Если объект выключили в середине прыжка — сбросить состояние,
        // чтобы при следующем включении игрок мог прыгать снова
        StopAllCoroutines();
        _jumping = false;
        if (_anim != null) _anim.SetBool("jump", false);
    }

    private void ApplyWallState()
    {
        if (_sr != null)
            _sr.flipX = _currentWall == Wall.Left;
    }
}
