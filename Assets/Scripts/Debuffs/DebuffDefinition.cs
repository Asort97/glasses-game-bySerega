using System;
using UnityEngine;

[Serializable]
public sealed class DebuffDefinition
{
    [SerializeField] private DebuffId id;
    [SerializeField] private string title;
    [SerializeField] private bool randomSpawnEnabled = true;
    [SerializeField] private bool canBeCancelled = true;
    [Min(0f)] [SerializeField] private float randomWeight = 1f;

    public DebuffId Id => id;
    public string Title => title;
    public bool RandomSpawnEnabled => randomSpawnEnabled;
    public bool CanBeCancelled => canBeCancelled;
    public float RandomWeight => randomWeight;
}
