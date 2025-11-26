using System;
using UnityEngine;

/// <summary>
/// Tracks collected orbs and fires events. No UI logic here.
/// Make exactly ONE in the scene (e.g., on "GameSystems").
/// </summary>
public class OrbManager : MonoBehaviour
{
    public static OrbManager Instance { get; private set; }

    [Header("Orb Target")]
    [Min(1)]
    [SerializeField] private int requiredOrbs = 5;

    [Header("UI Notifications")]
    [Tooltip("If ON, UI will still receive OnOrbChanged even after completion (count is capped).")]
    [SerializeField] private bool alwaysNotifyAfterComplete = false;

    public int CurrentOrbs { get; private set; } = 0;
    public int RequiredOrbs => requiredOrbs;
    public bool Completed => targetFired;

    /// <summary>Fired whenever the count changes: (current, required).</summary>
    public event Action<int, int> OnOrbChanged;

    /// <summary>Fired exactly once when CurrentOrbs reaches RequiredOrbs.</summary>
    public event Action OnTargetReached;

    private bool targetFired = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Main API to add orbs.</summary>
    public void AddOrb(int amount = 1)
    {
        int delta = Mathf.Max(1, amount);

        // Clamp to target so it never exceeds requiredOrbs
        int before = CurrentOrbs;
        CurrentOrbs = Mathf.Clamp(CurrentOrbs + delta, 0, requiredOrbs);

        // Notify UI before completion (and optionally after)
        if (!targetFired || alwaysNotifyAfterComplete)
            OnOrbChanged?.Invoke(CurrentOrbs, requiredOrbs);

        // Handle completion once
        if (!targetFired && CurrentOrbs >= requiredOrbs)
        {
            CurrentOrbs = requiredOrbs;
            targetFired = true;
            OnTargetReached?.Invoke();
        }
    }

    /// <summary>Compatibility wrapper for older pickup scripts.</summary>
    public void ReportOrbCollected() => AddOrb(1);

    /// <summary>Change target at runtime.</summary>
    public void SetRequiredOrbs(int newRequired, bool resetProgress = true)
    {
        requiredOrbs = Mathf.Max(1, newRequired);
        if (resetProgress) ResetOrbs();
        else OnOrbChanged?.Invoke(CurrentOrbs, requiredOrbs);
    }

    [ContextMenu("Reset Orbs")]
    public void ResetOrbs()
    {
        CurrentOrbs = 0;
        targetFired = false;
        OnOrbChanged?.Invoke(CurrentOrbs, requiredOrbs);
    }
}
