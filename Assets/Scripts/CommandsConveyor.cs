using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class CommandsConveyor : MonoBehaviour
{
    public enum Command { Up, Right, Down, Left, Space }

    [Header("References")]
    [SerializeField] private SpriteRenderer noteTemplate;
    [SerializeField] private Transform commandCenter;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite spaceSprite;

    [Header("Static Queue")]
    [Min(0.01f)] [SerializeField] private float symbolSpacing = 2.6f;
    [Min(0.01f)] [SerializeField] private float shiftDuration = 0.18f;
    [SerializeField] private float arrowUpAngle = 90f;
    [SerializeField] private Color movingColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private sealed class Note
    {
        public SpriteRenderer Renderer;
        public Command Command;
    }

    private readonly List<Note> _active = new List<Note>();
    private readonly Stack<Note> _pool = new Stack<Note>();
    private int _nextCommandIndex;
    private float _displayedIndex;
    private Tween _shiftTween;

    public bool IsConfigured => noteTemplate && commandCenter && arrowSprite && spaceSprite;
    public bool CanAcceptInput => _nextCommandIndex < _active.Count &&
        (_shiftTween == null || !_shiftTween.IsActive());

    public void ResetRound(int commandCount)
    {
        ClearNotes();
        noteTemplate.gameObject.SetActive(false);
        for (int i = 0; i < commandCount; i++)
        {
            Command command = Random.value < 0.5f ? Command.Space : (Command)Random.Range(0, 4);
            if (i >= 2 && _active[i - 1].Command == command && _active[i - 2].Command == command)
            {
                int alternative = Random.Range(0, 4);
                if (alternative >= (int)command) alternative++;
                command = (Command)alternative;
            }
            Spawn(command);
        }
        ArrangeNotes();
        RefreshColors();
    }

    public bool TryHit(Command command)
    {
        if (!CanAcceptInput || _active[_nextCommandIndex].Command != command) return false;
        _nextCommandIndex++;
        _shiftTween?.Kill();
        _shiftTween = DOTween.To(() => _displayedIndex, value =>
        {
            _displayedIndex = value;
            ArrangeNotes();
        }, _nextCommandIndex, shiftDuration).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                _shiftTween = null;
                RefreshColors();
            });
        RefreshColors();
        return true;
    }

    public void ShowWrongCommand(Command command)
    {
        if (!CanAcceptInput) return;
        SpriteRenderer current = _active[_nextCommandIndex].Renderer;
        SetSymbol(current, command);
        current.color = Color.white;
    }

    public void ClearNotes()
    {
        _shiftTween?.Kill();
        _shiftTween = null;
        for (int i = _active.Count - 1; i >= 0; i--) Recycle(i);
        _nextCommandIndex = 0;
        _displayedIndex = 0f;
    }

    private void Spawn(Command command)
    {
        Note note;
        if (_pool.Count > 0) note = _pool.Pop();
        else
        {
            SpriteRenderer renderer = Instantiate(noteTemplate, transform);
            renderer.name = "CommandSymbol";
            note = new Note { Renderer = renderer };
        }
        note.Command = command;
        note.Renderer.color = movingColor;
        SetSymbol(note.Renderer, command);
        note.Renderer.gameObject.SetActive(true);
        _active.Add(note);
    }

    private void ArrangeNotes()
    {
        Vector3 origin = transform.InverseTransformPoint(commandCenter.position);
        for (int i = 0; i < _active.Count; i++)
        {
            Note note = _active[i];
            note.Renderer.transform.localPosition = origin + Vector3.right * ((i - _displayedIndex) * symbolSpacing);
        }
    }

    private void RefreshColors()
    {
        bool settled = _shiftTween == null || !_shiftTween.IsActive();
        for (int i = 0; i < _active.Count; i++)
            _active[i].Renderer.color = settled && i == _nextCommandIndex ? Color.white : movingColor;
    }

    private void SetSymbol(SpriteRenderer renderer, Command command)
    {
        bool space = command == Command.Space;
        renderer.sprite = space ? spaceSprite : arrowSprite;
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, space ? 0f : arrowUpAngle - 90f * (int)command);
    }

    private void Recycle(int index)
    {
        Note note = _active[index];
        note.Renderer.gameObject.SetActive(false);
        _active.RemoveAt(index);
        _pool.Push(note);
    }

    private void OnDisable() => ClearNotes();
}
