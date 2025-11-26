using UnityEngine;
using System.Collections;

public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    public float shieldDuration = 5f;

    [Tooltip("Visual effect for the shield (assign a particle system or a glowing mesh object).")]
    public GameObject shieldVFX;

    [Tooltip("UI bar to show shield duration.")]
    public ShieldBarUI shieldBarUI;

    public bool IsActive { get; private set; } = false;

    private Coroutine routine;

    private void Start()
    {
        DisableVFX();
    }

    /// <summary>Activate the shield for the full duration. Multiple calls will restart the timer.</summary>
    public void ActivateShield()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        IsActive = true;
        EnableVFX();

        if (shieldBarUI != null)
            shieldBarUI.StartTimer(shieldDuration);

        yield return new WaitForSeconds(shieldDuration);

        EndShield();
    }

    private void EndShield()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        DisableVFX();
        IsActive = false;
    }

    // --- Helpers ---
    private void EnableVFX()
    {
        if (shieldVFX == null) return;

        var ps = shieldVFX.GetComponent<ParticleSystem>();
        if (ps != null) { ps.Clear(); ps.Play(); }
        else { shieldVFX.SetActive(true); }
    }

    private void DisableVFX()
    {
        if (shieldVFX == null) return;

        var ps = shieldVFX.GetComponent<ParticleSystem>();
        if (ps != null) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        else { shieldVFX.SetActive(false); }
    }
}
