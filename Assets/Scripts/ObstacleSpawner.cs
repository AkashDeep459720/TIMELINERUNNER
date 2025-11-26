using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Setup")]
    public SegmentLoopGenerator segmentGenerator;
    public ObstacleSpawnerConfig robotConfig;
    public ObstacleSpawnerConfig egyptConfig;
    public Transform obstaclesRoot;

    [Header("Difficulty & Scaling")]
    public bool useDifficultyScaling = true;
    public float distanceForMaxDifficulty = 500f;

    [Header("Combo Settings")]
    [Range(0f, 1f)] public float comboChanceStart = 0.1f;
    [Range(0f, 1f)] public float comboChanceEnd = 0.7f;
    public float minSpacingInsideCombo = 12f;
    public bool emptySegmentAfterCombo = true;

    [Header("Spacing Rules")]
    public float minGroupSpacing = 15f;
    public float maxGroupSpacing = 25f;
    public int emptySegmentInterval = 0;

    // 🔔 CoinSpawner listens to this
    public static System.Action<GameObject, string> OnObstacleSpawned;

    private ObstacleSpawnerConfig activeConfig;
    private int segmentCounter = 0;
    private float playerDistanceTravelled = 0f;
    private Transform player;
    private float lastSpawnZ = -999f;
    private bool skipNextSegment = false;

    private const float segmentLength = 50f;

    void Start()
    {
        if (!segmentGenerator) segmentGenerator = FindFirstObjectByType<SegmentLoopGenerator>();
        if (!obstaclesRoot) obstaclesRoot = new GameObject("ObstaclesRoot").transform;
        if (!player) player = GameObject.FindWithTag("Player")?.transform;

        activeConfig = robotConfig;
        Debug.Log("🔵 ObstacleSpawner: Using Robot config at start");

        SegmentLoopGenerator.OnSegmentSpawned += HandleSegmentSpawned;
        PortalEvents.OnPortalSpawned += HandlePortalSwap;
    }

    void OnDestroy()
    {
        SegmentLoopGenerator.OnSegmentSpawned -= HandleSegmentSpawned;
        PortalEvents.OnPortalSpawned -= HandlePortalSwap;
    }

    void Update()
    {
        if (player) playerDistanceTravelled = player.position.z;
    }

    private void HandleSegmentSpawned(GameObject segment)
    {
        if (activeConfig == null)
        {
            Debug.LogWarning("⚠️ ObstacleSpawner: activeConfig is NULL, skipping spawn");
            return;
        }

        bool hasAnyPrefabs =
            (activeConfig.jumpOverObstacles != null && activeConfig.jumpOverObstacles.Length > 0) ||
            (activeConfig.slideUnderObstacles != null && activeConfig.slideUnderObstacles.Length > 0) ||
            (activeConfig.avoidLaneObstacles != null && activeConfig.avoidLaneObstacles.Length > 0) ||
            (activeConfig.fullLaneObstacles != null && activeConfig.fullLaneObstacles.Length > 0);

        if (!hasAnyPrefabs)
        {
            Debug.LogWarning($"⚠️ ObstacleSpawner: Config '{activeConfig.name}' has no prefabs assigned");
            return;
        }

        segmentCounter++;

        if (skipNextSegment)
        {
            skipNextSegment = false;
            return;
        }

        if (emptySegmentInterval > 0 && segmentCounter % emptySegmentInterval == 0)
            return;

        float difficultyFactor = useDifficultyScaling
            ? Mathf.Clamp01(playerDistanceTravelled / distanceForMaxDifficulty)
            : 0f;

        float comboChance = Mathf.Lerp(comboChanceStart, comboChanceEnd, difficultyFactor);
        bool doCombo = Random.value < comboChance;

        if (doCombo)
        {
            SpawnCombo(segment, difficultyFactor);
            if (emptySegmentAfterCombo) skipNextSegment = true;
        }
        else
        {
            SpawnSingleGroup(segment, difficultyFactor);
        }
    }

    // ---------------- Spawning ----------------
    private void SpawnSingleGroup(GameObject segment, float difficultyFactor)
    {
        GameObject prefab = PickObstaclePrefab(activeConfig, difficultyFactor);
        if (!prefab) return;

        float zOffset = Random.Range(0.2f * segmentLength, 0.8f * segmentLength);
        float spawnZ = segment.transform.position.z + zOffset;

        float spacing = Mathf.Lerp(minGroupSpacing, maxGroupSpacing, difficultyFactor);
        if (spawnZ - lastSpawnZ < spacing) return;

        lastSpawnZ = spawnZ;
        SpawnGroup(prefab, spawnZ, segment);
    }

    private void SpawnCombo(GameObject segment, float difficultyFactor)
    {
        int comboLength = Random.Range(2, 4);
        float startZ = segment.transform.position.z + (0.2f * segmentLength);

        int baseLane = Random.Range(-1, 2);
        bool sameLane = Random.value < 0.5f;

        for (int i = 0; i < comboLength; i++)
        {
            GameObject prefab = PickObstaclePrefab(activeConfig, difficultyFactor, true);
            if (!prefab) continue;

            if (prefab.GetComponent<FullLaneObstacle>())
                continue;

            float spawnZ = startZ + (i * minSpacingInsideCombo);
            int lane = sameLane ? baseLane : Random.Range(-1, 2);

            SpawnGroup(prefab, spawnZ, segment, lane);
            lastSpawnZ = spawnZ;
        }
    }

    private void SpawnGroup(GameObject prefab, float spawnZ, GameObject segment, int forcedLane = int.MinValue)
    {
        float y = prefab.transform.position.y;
        ObstacleYOffset offset = prefab.GetComponent<ObstacleYOffset>();
        if (offset != null) y = offset.yOffset;

        Quaternion rot = prefab.GetComponent<SlideObstacle>()
            ? prefab.transform.rotation
            : Quaternion.identity;

        // --- Full-lane ---
        if (prefab.GetComponent<FullLaneObstacle>())
        {
            Vector3 pos = new Vector3(0f, y, spawnZ);
            ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
            Debug.Log($"🟠 Spawned FullLane: {prefab.name} at {pos}");
            OnObstacleSpawned?.Invoke(segment, "FullLane");
            return;
        }

        // --- Two-lane ---
        if (prefab.GetComponent<TwoLaneBlocker>())
        {
            int lane = (forcedLane != int.MinValue) ? forcedLane : ((Random.value < 0.5f) ? -1 : 1);
            Vector3 pos = new Vector3(lane * 3f, y, spawnZ);
            ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
            Debug.Log($"🟠 Spawned TwoLane: {prefab.name} at {pos}");
            OnObstacleSpawned?.Invoke(segment, "TwoLane");
            return;
        }

        // --- One-lane blocker + filler ---
        if (prefab.GetComponent<OneLaneBlocker>())
        {
            List<int> lanes = new List<int> { -1, 0, 1 };
            int removedLane = (forcedLane != int.MinValue) ? forcedLane : lanes[Random.Range(0, lanes.Count)];
            lanes.Remove(removedLane);

            foreach (int lane in lanes)
            {
                Vector3 pos = new Vector3(lane * 3f, y, spawnZ);
                ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
            }

            OnObstacleSpawned?.Invoke(segment, "OneLane");

            GameObject filler = PickJumpOrSlide(activeConfig);
            if (filler)
            {
                float fy = filler.transform.position.y;
                ObstacleYOffset fillerOffset = filler.GetComponent<ObstacleYOffset>();
                if (fillerOffset != null) fy = fillerOffset.yOffset;

                Quaternion fRot = filler.GetComponent<SlideObstacle>() ? filler.transform.rotation : Quaternion.identity;
                Vector3 fillerPos = new Vector3(removedLane * 3f, fy, spawnZ);
                ObjectPoolManager.Instance.SpawnFromPool(filler.name, fillerPos, fRot, obstaclesRoot);

                Debug.Log($"🟠 Spawned OneLane filler: {filler.name} at {fillerPos}");
            }
            return;
        }

        // --- Triplet (jump/slide) ---
        if (prefab.GetComponent<JumpObstacle>() || prefab.GetComponent<SlideObstacle>())
        {
            string type = prefab.GetComponent<JumpObstacle>() ? "JumpObstacle" : "SlideObstacle";

            if (forcedLane != int.MinValue)
            {
                Vector3 pos = new Vector3(forcedLane * 3f, y, spawnZ);
                ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
                Debug.Log($"🟠 Spawned {type}: {prefab.name} at {pos}");
                OnObstacleSpawned?.Invoke(segment, type);
            }
            else
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector3 pos = new Vector3(lane * 3f, y, spawnZ);
                    ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
                }
                Debug.Log($"🟠 Spawned Triplet {type}: {prefab.name} across all lanes at Z {spawnZ}");
                OnObstacleSpawned?.Invoke(segment, type);
            }
        }
    }

    // ---------------- Prefab Pickers ----------------
    private GameObject PickObstaclePrefab(ObstacleSpawnerConfig config, float difficultyFactor, bool excludeFullLane = false)
    {
        List<GameObject> pool = new List<GameObject>();
        if (config.jumpOverObstacles != null) pool.AddRange(config.jumpOverObstacles);
        if (config.slideUnderObstacles != null) pool.AddRange(config.slideUnderObstacles);
        if (config.avoidLaneObstacles != null) pool.AddRange(config.avoidLaneObstacles);
        if (!excludeFullLane && config.fullLaneObstacles != null) pool.AddRange(config.fullLaneObstacles);

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private GameObject PickJumpOrSlide(ObstacleSpawnerConfig config)
    {
        List<GameObject> pool = new List<GameObject>();
        if (config.jumpOverObstacles != null) pool.AddRange(config.jumpOverObstacles);
        if (config.slideUnderObstacles != null) pool.AddRange(config.slideUnderObstacles);

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private void HandlePortalSwap(Vector3 pos)
    {
        foreach (Transform child in obstaclesRoot)
            child.gameObject.SetActive(false);

        activeConfig = egyptConfig;
        segmentCounter = 0;
        lastSpawnZ = -999f;

        if (activeConfig == null)
        {
            Debug.LogError("❌ ObstacleSpawner: Egypt config is NULL!");
        }
        else
        {
            Debug.Log($"🟠 ObstacleSpawner: Switched to Egypt config '{activeConfig.name}' with pools: " +
                      $"{activeConfig.jumpOverObstacles?.Length ?? 0} jump, " +
                      $"{activeConfig.slideUnderObstacles?.Length ?? 0} slide, " +
                      $"{activeConfig.avoidLaneObstacles?.Length ?? 0} avoid, " +
                      $"{activeConfig.fullLaneObstacles?.Length ?? 0} full.");
        }
    }
}
