using UnityEngine;
using System.Collections;

public class PlayerSpeedBoost : MonoBehaviour
{
    [Header("Boost")]
    public float speedMultiplier = 1.5f;
    public float defaultDuration = 5f;

    [Header("Refs")]
    public PlayerMovement player;   // drag your PlayerMovement
    public SpeedBarUI speedBar;     // drag SpeedBarUI (Step 1)
    public GameObject speedVFX;     // optional trail/glow

    bool active;
    float remaining;
    float baseSpeed;
    Coroutine co;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerMovement>();
    }

    void Start()
    {
        if (speedVFX) speedVFX.SetActive(false);
    }

    /// Call this from a pickup or powerup manager
    public void Activate(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;

        if (!active)
        {
            co = StartCoroutine(BoostRoutine(duration));
        }
        else
        {
            // refresh/extend duration while already active
            remaining = Mathf.Max(remaining, duration);
            if (speedBar) speedBar.StartTimer(remaining);
        }
    }

    IEnumerator BoostRoutine(float duration)
    {
        active = true;
        remaining = duration;

        baseSpeed = player ? player.playerSpeed : 0f;
        if (player) player.playerSpeed = baseSpeed * speedMultiplier;

        if (speedVFX) speedVFX.SetActive(true);
        if (speedBar) speedBar.StartTimer(remaining);

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        // restore
        if (player) player.playerSpeed = baseSpeed;
        if (speedVFX) speedVFX.SetActive(false);
        if (speedBar) speedBar.StopTimer();

        active = false;
        co = null;
    }

    void OnDisable()
    {
        // safety: restore if object is disabled mid-boost
        if (active)
        {
            if (player) player.playerSpeed = baseSpeed;
            if (speedVFX) speedVFX.SetActive(false);
            if (speedBar) speedBar.StopTimer();
            if (co != null) StopCoroutine(co);
            active = false;
        }
    }
}
