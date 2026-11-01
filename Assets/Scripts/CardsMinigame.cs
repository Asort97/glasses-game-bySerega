using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CardsMinigame : MinigameBase
{
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private MemoryCardView[] cards;
    [SerializeField] private Sprite[] pairSprites;
    [SerializeField] private Sprite backSprite;
    [Min(0.01f)] [SerializeField] private float flipDuration = 0.18f;
    [Min(0f)] [SerializeField] private float mismatchPause = 0.6f;

    private MemoryCardView _first;
    private Sequence _turn;
    private float _elapsed;
    private int _matchedPairs;
    private bool _isPlaying, _busy;

    public override void StartGame()
    {
        StopGame();
        if (!pointer || !backSprite || cards == null || pairSprites == null ||
            cards.Length == 0 || cards.Length % 2 != 0 || pairSprites.Length < cards.Length / 2)
        {
            Debug.LogError("Cards minigame references or pair count are invalid.", this);
            return;
        }
        for (int i = 0; i < cards.Length; i++)
            if (!cards[i])
            {
                Debug.LogError("Cards minigame has a missing card or sprite.", this);
                return;
            }

        int[] pairIds = new int[pairSprites.Length];
        for (int i = 0; i < pairIds.Length; i++)
        {
            if (!pairSprites[i])
            {
                Debug.LogError("Cards minigame has a missing pair sprite.", this);
                return;
            }
            pairIds[i] = i;
        }
        for (int i = pairIds.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pairIds[i], pairIds[j]) = (pairIds[j], pairIds[i]);
        }
        int[] deck = new int[cards.Length];
        for (int i = 0; i < deck.Length; i++) deck[i] = pairIds[i / 2];
        for (int i = deck.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        for (int i = 0; i < cards.Length; i++)
            cards[i].ResetCard(deck[i], pairSprites[deck[i]], backSprite);

        base.StartGame();
        _first = null;
        _elapsed = 0f;
        _matchedPairs = 0;
        _isPlaying = true;
    }

    public override void StopGame()
    {
        _isPlaying = _busy = false;
        _turn?.Kill();
        _turn = null;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_isPlaying) return;
        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (_elapsed >= surviveTime)
        {
            StopGame();
            RaiseLose();
            return;
        }
        if (_busy || !Input.GetMouseButtonDown(0) ||
            (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
        if (!pointer.TryGetPosition(cards[0].WorldZ, true, out Vector3 point)) return;
        foreach (MemoryCardView card in cards)
            if (card.Contains(point))
            {
                SelectCard(card);
                break;
            }
    }

    private void SelectCard(MemoryCardView card)
    {
        if (!_isPlaying || _busy || card.IsMatched || card == _first) return;
        _busy = true;
        _turn = DOTween.Sequence().Append(card.Flip(true, flipDuration));
        if (_first == null)
        {
            _first = card;
            _turn.OnComplete(EndTurn);
            return;
        }

        MemoryCardView first = _first;
        bool matches = first.PairId == card.PairId;
        if (matches)
            _turn.Append(first.Collapse(flipDuration))
                .Join(card.Collapse(flipDuration));
        else
            _turn.AppendInterval(mismatchPause)
                .Append(first.Flip(false, flipDuration))
                .Join(card.Flip(false, flipDuration));

        _turn.OnComplete(() =>
        {
            _first = null;
            EndTurn();
            if (!matches) return;
            first.MarkMatched();
            card.MarkMatched();
            _matchedPairs++;
            if (_matchedPairs == cards.Length / 2)
            {
                StopGame();
                RaiseWin();
            }
        });
    }

    private void EndTurn()
    {
        _turn = null;
        _busy = false;
    }
}
