using UnityEngine;

public class Fisher : MonoBehaviour 
{
    [SerializeField] private Transform hookTransform;
    [SerializeField] private Transform reelTransform;
    [SerializeField] private float startHeight;
    [SerializeField] private float endHeight;
    [SerializeField] private float speedDown;
    private bool _isDowning;
    private bool _isUp;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !_isDowning && !_isUp)
        {
            _isDowning = true;
        }        

        if(_isDowning)
        {
            hookTransform.localPosition = new Vector2(
                hookTransform.localPosition.x,
                Mathf.Clamp(
                    hookTransform.localPosition.y - (speedDown * Time.deltaTime),
                    endHeight,
                    startHeight
                )
            );

            if(hookTransform.localPosition.y <= endHeight)
            {
                _isDowning = false;
                _isUp = true;
            }
        }

        if(_isUp)
        {
            hookTransform.localPosition = new Vector2(
                hookTransform.localPosition.x,
                Mathf.Clamp(
                    hookTransform.localPosition.y + (speedDown * Time.deltaTime),
                    endHeight,
                    startHeight
                )
            );

            if(hookTransform.localPosition.y >= startHeight)
            {
                _isUp = false;
            }
        }
    }    
}