using UnityEngine;
using System.Collections;

public class PlayerSpeedBoost : MonoBehaviour
{
    [Header("Boost")]
    public float speedMultiplier = 1.5f;
    public float defaultDuration = 5f;

    [Header("Refs")]
    public PlayerMovement player;
    public SpeedBarUI speedBar;
    public GameObject speedVFX;

    bool active;
    float remaining;
    Coroutine co;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerMovement>();
    }

    void Start()
    {
        if (speedVFX) speedVFX.SetActive(false);
    }

    public void Activate(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;

        if (!active)
        {
            co = StartCoroutine(BoostRoutine(duration));
        }
        else
        {
            remaining = Mathf.Max(remaining, duration);
            if (speedBar) speedBar.StartTimer(remaining);
        }
    }

    IEnumerator BoostRoutine(float duration)
    {
        active = true;
        remaining = duration;

        if (speedVFX) speedVFX.SetActive(true);
        if (speedBar) speedBar.StartTimer(remaining);

        while (remaining > 0f)
        {
            WorldScrollController scroll = WorldScrollController.Instance;
            if (scroll != null)
                scroll.ActivateSpeedBoost(speedMultiplier, Mathf.Max(remaining, 0.01f));

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (speedVFX) speedVFX.SetActive(false);
        if (speedBar) speedBar.StopTimer();

        active = false;
        co = null;
    }

    void OnDisable()
    {
        if (active)
        {
            if (speedVFX) speedVFX.SetActive(false);
            if (speedBar) speedBar.StopTimer();
            if (co != null) StopCoroutine(co);
            active = false;
        }
    }
}
