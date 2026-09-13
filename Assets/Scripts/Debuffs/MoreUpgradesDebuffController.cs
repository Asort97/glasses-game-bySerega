using UnityEngine;

public sealed class MoreUpgradesDebuffController : MonoBehaviour
{
    [SerializeField] private DebuffSpawner debuffSpawner;

    private void OnEnable()
    {
        debuffSpawner.DebuffApplied += HandleDebuffApplied;
    }

    private void OnDisable()
    {
        debuffSpawner.DebuffApplied -= HandleDebuffApplied;
    }

    private void HandleDebuffApplied(DebuffId id)
    {
        if (id == DebuffId.MoreUpgrades)
            ApplyStack();
    }

    [ContextMenu("Apply More Upgrades Stack")]
    public void ApplyStack()
    {
        debuffSpawner.AddWindowPerBatch();
    }
}
