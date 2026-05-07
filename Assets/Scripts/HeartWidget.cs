using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HeartWidget : MonoBehaviour
{
    [SerializeField] private Sprite spriteHealth0;
    [SerializeField] private Sprite spriteHealth1;
    [SerializeField] private Sprite spriteHealth2;

    private Image         _image;
    private RectTransform _rect;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect  = GetComponent<RectTransform>();
    }

    public void SetState(int state)
    {
        if (_image == null) return;
        switch (state)
        {
            case 0: _image.sprite = spriteHealth0; break;
            case 1: _image.sprite = spriteHealth1; break;
            case 2: _image.sprite = spriteHealth2; break;
        }
    }

    public void Shake()
    {
        if (_rect == null) return;
        _rect.DOKill(true);
        _rect.DOShakeAnchorPos(0.3f, new Vector2(8f, 8f), 25, 90f, false, true);
    }
}
