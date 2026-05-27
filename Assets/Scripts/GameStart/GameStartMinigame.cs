using DG.Tweening;
using UnityEngine;

public class GameStartMinigame : MinigameBase
{
    [SerializeField] private Transform circle;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float nearThreshold = 0.1f;
    private bool _control = true;

    public override void StartGame()
    {
        base.StartGame();
    }

    public override void StopGame()
    {
        base.StopGame();
    }

    protected override void Update()
    {
        if(_control)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 input = new Vector3(h, v, 0f);

            if (input.sqrMagnitude > 1f) input.Normalize();

            circle.localPosition += input * moveSpeed * Time.deltaTime;
        }

        Vector2 pos2 = new Vector2(circle.localPosition.x, circle.localPosition.y);
        if (pos2.sqrMagnitude <= nearThreshold * nearThreshold)
        {
            Debug.Log("Circle is near Vector2.zero");
            if(Input.GetKeyDown(KeyCode.Space)) 
            {
                _control = false;
                circle.DOLocalMove(Vector2.zero, 0.5f).OnComplete(()=> RaiseWin());
            }
        }

        base.Update();
    }

}
