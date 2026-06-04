using System.Collections.Generic;
using UnityEngine;

public class WorldScrollController : MonoBehaviour
{
    public static WorldScrollController Instance { get; private set; }

    [Header("Scroll Speed Profile (Inspector)")]
    [SerializeField] private WorldScrollSpeedProfile scrollSpeedProfile;

    [Header("Legacy Fallback (if no profile)")]
    [SerializeField] private float baseScrollSpeed = 8f;
    [SerializeField] private float maxScrollSpeed = 35f;
    [SerializeField] private bool useDifficultyScaling = true;
    [SerializeField] private float speedIncreaseAmount = 0.2f;
    [SerializeField] private float speedIncreaseInterval = 60f;

    [Header("Roots To Scroll")]
    [SerializeField] private List<Transform> scrollRoots = new List<Transform>();

    private float nextSpeedIncreaseTime;
    private int rampTickCount;
    private float activeBoostMultiplier = 1f;
    private float boostEndTime = -1f;
    private bool externalPause;

    public float DistanceTravelled { get; private set; }
    public bool HasSpeedProfile => scrollSpeedProfile != null;
    public float CurrentBaseScrollSpeed => baseScrollSpeed;
    public float CurrentScrollSpeed => ShouldScroll() ? Mathf.Min(GetMaxScrollSpeed(), baseScrollSpeed * activeBoostMultiplier) : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyProfileIfPresent();
        ScheduleNextRamp();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null) return;

        WorldScrollController existing = FindFirstObjectByType<WorldScrollController>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("WorldScrollController");
#if UNITY_EDITOR
        go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
#else
        go.hideFlags = HideFlags.DontSave;
#endif
        go.AddComponent<WorldScrollController>();
    }

    private void Update()
    {
        TryApplyTimeRamp();

        if (boostEndTime > 0f && Time.time >= boostEndTime)
        {
            boostEndTime = -1f;
            activeBoostMultiplier = 1f;
        }

        float step = CurrentScrollSpeed * Time.deltaTime;
        if (step <= 0f) return;

        DistanceTravelled += step;
        Vector3 move = Vector3.back * step;
        for (int i = scrollRoots.Count - 1; i >= 0; i--)
        {
            Transform root = scrollRoots[i];
            if (root == null)
            {
                scrollRoots.RemoveAt(i);
                continue;
            }

            root.position += move;
        }
    }

    public void ApplySpeedProfile(WorldScrollSpeedProfile profile)
    {
        scrollSpeedProfile = profile;
        ApplyProfileIfPresent();
        ScheduleNextRamp();
    }

    private void ApplyProfileIfPresent()
    {
        if (scrollSpeedProfile == null) return;

        baseScrollSpeed = scrollSpeedProfile.startingScrollSpeed;
        maxScrollSpeed = scrollSpeedProfile.maxScrollSpeed;
        useDifficultyScaling = scrollSpeedProfile.useTimeBasedRamp;
        speedIncreaseAmount = scrollSpeedProfile.speedIncreaseAmount;
        speedIncreaseInterval = scrollSpeedProfile.speedIncreaseIntervalSeconds;
        rampTickCount = 0;
    }

    private void TryApplyTimeRamp()
    {
        if (!useDifficultyScaling) return;
        if (scrollSpeedProfile != null && scrollSpeedProfile.pauseRampWhenScrollPaused && !ShouldScroll())
            return;

        if (Time.timeSinceLevelLoad < nextSpeedIncreaseTime) return;

        float increment = speedIncreaseAmount;
        if (scrollSpeedProfile != null)
        {
            int maxTicks = EstimateMaxRampTicks();
            increment = scrollSpeedProfile.EvaluateIncrement(rampTickCount, maxTicks);
        }

        baseScrollSpeed = Mathf.Min(GetMaxScrollSpeed(), baseScrollSpeed + increment);
        rampTickCount++;
        ScheduleNextRamp();
    }

    private int EstimateMaxRampTicks()
    {
        float max = GetMaxScrollSpeed();
        float start = scrollSpeedProfile != null ? scrollSpeedProfile.startingScrollSpeed : baseScrollSpeed;
        float step = scrollSpeedProfile != null ? scrollSpeedProfile.speedIncreaseAmount : speedIncreaseAmount;
        if (step <= 0f) return 1;
        return Mathf.Max(1, Mathf.CeilToInt((max - start) / step));
    }

    private void ScheduleNextRamp()
    {
        float interval = scrollSpeedProfile != null
            ? scrollSpeedProfile.speedIncreaseIntervalSeconds
            : speedIncreaseInterval;
        nextSpeedIncreaseTime = Time.timeSinceLevelLoad + Mathf.Max(1f, interval);
    }

    private float GetMaxScrollSpeed()
    {
        return scrollSpeedProfile != null ? scrollSpeedProfile.maxScrollSpeed : maxScrollSpeed;
    }

    public void ConfigureBaseScrollSpeed(float speed)
    {
        if (speed > 0f)
            baseScrollSpeed = speed;
    }

    public void RegisterScrollRoot(Transform root)
    {
        if (root == null || scrollRoots.Contains(root)) return;
        scrollRoots.Add(root);
    }

    public void UnregisterScrollRoot(Transform root)
    {
        if (root == null) return;
        scrollRoots.Remove(root);
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f) return;
        activeBoostMultiplier = Mathf.Max(activeBoostMultiplier, multiplier);
        boostEndTime = Mathf.Max(boostEndTime, Time.time + duration);
    }

    public void SetScrollPaused(bool paused)
    {
        externalPause = paused;
    }

    public void ResetDistance()
    {
        DistanceTravelled = 0f;
    }

    private bool ShouldScroll()
    {
        if (GameState.IsPaused || externalPause) return false;
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.isFrozen) return false;
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
