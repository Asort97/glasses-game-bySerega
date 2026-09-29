using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Temporarily blends the resolved stack, including URP defaults, without editing profiles.
public sealed class StartupVolumeBlend : IDisposable
{
    private sealed class Entry
    {
        public VolumeComponent Neutral;
        public VolumeComponent Saved;
        public VolumeComponent State;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private bool _applied;

    public void Apply(VolumeStack stack, float progress)
    {
        Restore();
        if (progress >= 1f || stack == null)
            return;

        if (_entries.Count == 0)
        {
            foreach (Type type in VolumeManager.instance.baseComponentTypeArray)
            {
                var neutral = (VolumeComponent)ScriptableObject.CreateInstance(type);
                var saved = (VolumeComponent)ScriptableObject.CreateInstance(type);
                neutral.hideFlags = saved.hideFlags = HideFlags.HideAndDontSave;
                neutral.SetAllOverridesTo(true);
                _entries.Add(new Entry { Neutral = neutral, Saved = saved });
            }
        }

        foreach (Entry entry in _entries)
        {
            entry.State = stack.GetComponent(entry.Neutral.GetType());
            if (entry.State == null)
                continue;
            Copy(entry.State, entry.Saved);
            entry.Neutral.Override(entry.State, 1f - Mathf.Clamp01(progress));
        }
        _applied = true;
    }

    public void Restore()
    {
        if (!_applied)
            return;
        foreach (Entry entry in _entries)
            if (entry.State != null)
                Copy(entry.Saved, entry.State);
        _applied = false;
    }

    private static void Copy(VolumeComponent source, VolumeComponent destination)
    {
        for (int i = 0; i < source.parameters.Count; i++)
        {
            destination.parameters[i].SetValue(source.parameters[i]);
            destination.parameters[i].overrideState = source.parameters[i].overrideState;
        }
    }

    public void Dispose()
    {
        Restore();
        foreach (Entry entry in _entries)
        {
            CoreUtils.Destroy(entry.Neutral);
            CoreUtils.Destroy(entry.Saved);
        }
        _entries.Clear();
    }
}
