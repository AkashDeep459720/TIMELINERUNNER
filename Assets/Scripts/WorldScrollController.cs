using System.Collections.Generic;
using UnityEngine;

public class WorldScrollController : MonoBehaviour
{
    public static WorldScrollController Instance { get; private set; }

    [Header("Scroll Speed")]
    [SerializeField] private float baseScrollSpeed = 8f;
    [SerializeField] private float maxScrollSpeed = 35f;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool useDifficultyScaling = true;
    [SerializeField] private float speedIncreaseAmount = 0.2f;
    [SerializeField] private float speedIncreaseInterval = 30f;

    [Header("Roots To Scroll")]
    [SerializeField] private List<Transform> scrollRoots = new List<Transform>();

    private float nextSpeedIncreaseTime;
    private float activeBoostMultiplier = 1f;
    private float boostEndTime = -1f;
    private bool externalPause;

    public float DistanceTravelled { get; private set; }
    public float CurrentScrollSpeed => ShouldScroll() ? Mathf.Min(maxScrollSpeed, baseScrollSpeed * activeBoostMultiplier) : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        nextSpeedIncreaseTime = Time.timeSinceLevelLoad + speedIncreaseInterval;
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
        go.AddComponent<WorldScrollController>();
    }

    private void Update()
    {
        if (useDifficultyScaling && Time.timeSinceLevelLoad >= nextSpeedIncreaseTime)
        {
            baseScrollSpeed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + speedIncreaseAmount);
            nextSpeedIncreaseTime = Time.timeSinceLevelLoad + speedIncreaseInterval;
        }

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
