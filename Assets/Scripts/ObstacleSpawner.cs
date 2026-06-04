using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Setup")]
    public SegmentLoopGenerator segmentGenerator;
    public ObstacleSpawnerConfig robotConfig;
    public ObstacleSpawnerConfig egyptConfig;
    public Transform obstaclesRoot;

    [Header("Robot Tuning (Inspector)")]
    public RobotSpawnProfile robotSpawnProfile;

    [Header("World Scroll (Inspector)")]
    public WorldScrollSpeedProfile scrollSpeedProfile;

    [Header("Egypt Tuning (unchanged when egypt active)")]
    public bool useDifficultyScaling = true;
    public float distanceForMaxDifficulty = 500f;
    [Range(0f, 1f)] public float comboChanceStart = 0.1f;
    [Range(0f, 1f)] public float comboChanceEnd = 0.7f;
    public float minSpacingInsideCombo = 12f;
    public bool emptySegmentAfterCombo = true;
    public float minGroupSpacing = 15f;
    public float maxGroupSpacing = 25f;
    public int emptySegmentInterval = 0;

    public static System.Action<GameObject, string> OnObstacleSpawned;

    private ObstacleSpawnerConfig activeConfig;
    private int segmentCounter = 0;
    private float playerDistanceTravelled;
    private Transform player;
    private PlayerMovement playerMovement;
    private float lastSpawnZ = -999f;
    private float lastSpawnDistance = -999f;
    private bool skipNextSegment;
    private bool rootRegistered;

    private const float segmentLength = 50f;

    private bool UsingRobotProfile => activeConfig == robotConfig && robotSpawnProfile != null;

    void Start()
    {
        if (!segmentGenerator) segmentGenerator = FindFirstObjectByType<SegmentLoopGenerator>();
        if (!obstaclesRoot) obstaclesRoot = new GameObject("ObstaclesRoot").transform;
        if (!player) player = GameObject.FindWithTag("Player")?.transform;
        if (player) playerMovement = player.GetComponent<PlayerMovement>();

        TryRegisterRoot();
        ApplyWorldScrollProfile();
        activeConfig = robotConfig;

        SegmentLoopGenerator.OnSegmentSpawned += HandleSegmentSpawned;
        PortalEvents.OnEgyptTimelineEntered += HandleEgyptTimelineSwap;
    }

    void OnDestroy()
    {
        SegmentLoopGenerator.OnSegmentSpawned -= HandleSegmentSpawned;
        PortalEvents.OnEgyptTimelineEntered -= HandleEgyptTimelineSwap;
    }

    void Update()
    {
        TryRegisterRoot();
        if (WorldScrollController.Instance != null)
            playerDistanceTravelled = WorldScrollController.Instance.DistanceTravelled;
        else if (player)
            playerDistanceTravelled = player.position.z;
    }

    private void HandleSegmentSpawned(GameObject segment)
    {
        if (activeConfig == null) return;

        if (!HasAnyPrefabs(activeConfig)) return;

        segmentCounter++;

        if (skipNextSegment)
        {
            skipNextSegment = false;
            return;
        }

        if (GetEmptySegmentInterval() > 0 && segmentCounter % GetEmptySegmentInterval() == 0)
            return;

        float difficultyFactor = GetDifficultyFactor();

        if (UsingRobotProfile && segmentCounter <= robotSpawnProfile.graceSegmentsAfterRunStart)
        {
            SpawnSingleGroup(segment, difficultyFactor);
            return;
        }

        float comboChance = GetComboChance(difficultyFactor);
        bool doCombo = Random.value < comboChance;

        if (doCombo)
        {
            SpawnCombo(segment, difficultyFactor);
            if (GetEmptySegmentAfterCombo()) skipNextSegment = true;
        }
        else
        {
            SpawnSingleGroup(segment, difficultyFactor);
        }
    }

    private void SpawnSingleGroup(GameObject segment, float difficultyFactor)
    {
        GameObject prefab = PickObstaclePrefab(activeConfig, difficultyFactor);
        if (!prefab) return;

        float zOffset = Random.Range(0.2f * segmentLength, 0.8f * segmentLength);
        float spawnZ = segment.transform.position.z + zOffset;
        float spawnDistance = GetRunDistance() + zOffset;

        float spacing = GetGroupSpacing(difficultyFactor);
        if (spawnDistance - lastSpawnDistance < spacing) return;

        lastSpawnZ = spawnZ;
        lastSpawnDistance = spawnDistance;
        SpawnGroup(prefab, spawnZ, segment);
    }

    private void SpawnCombo(GameObject segment, float difficultyFactor)
    {
        float minInsideCombo = UsingRobotProfile ? robotSpawnProfile.minSpacingInsideCombo : minSpacingInsideCombo;

        int comboLength = Random.Range(2, 4);
        float startZ = segment.transform.position.z + (0.2f * segmentLength);

        int baseLane = Random.Range(-1, 2);
        bool sameLane = Random.value < 0.5f;

        for (int i = 0; i < comboLength; i++)
        {
            GameObject prefab = PickObstaclePrefab(activeConfig, difficultyFactor, true);
            if (!prefab || prefab.GetComponent<FullLaneObstacle>()) continue;

            float spawnZ = startZ + (i * minInsideCombo);
            float spawnDistance = GetRunDistance() + (0.2f * segmentLength) + (i * minInsideCombo);
            int lane = sameLane ? baseLane : Random.Range(-1, 2);

            SpawnGroup(prefab, spawnZ, segment, lane);
            lastSpawnZ = spawnZ;
            lastSpawnDistance = spawnDistance;
        }
    }

    private void SpawnGroup(GameObject prefab, float spawnZ, GameObject segment, int forcedLane = int.MinValue)
    {
        float laneDistance = GetLaneDistance();
        float y = prefab.transform.position.y;
        ObstacleYOffset offset = prefab.GetComponent<ObstacleYOffset>();
        if (offset != null) y = offset.yOffset;

        Quaternion rot = prefab.GetComponent<SlideObstacle>() ? prefab.transform.rotation : Quaternion.identity;

        if (prefab.GetComponent<FullLaneObstacle>())
        {
            Vector3 pos = new Vector3(0f, y, spawnZ);
            ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
            OnObstacleSpawned?.Invoke(segment, "FullLane");
            return;
        }

        if (prefab.GetComponent<TwoLaneBlocker>())
        {
            int lane = forcedLane != int.MinValue ? forcedLane : (Random.value < 0.5f ? -1 : 1);
            Vector3 pos = new Vector3(lane * laneDistance, y, spawnZ);
            ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
            OnObstacleSpawned?.Invoke(segment, "TwoLane");
            return;
        }

        if (prefab.GetComponent<OneLaneBlocker>())
        {
            List<int> lanes = new List<int> { -1, 0, 1 };
            int removedLane = forcedLane != int.MinValue ? forcedLane : lanes[Random.Range(0, lanes.Count)];
            lanes.Remove(removedLane);

            foreach (int lane in lanes)
            {
                Vector3 pos = new Vector3(lane * laneDistance, y, spawnZ);
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
                Vector3 fillerPos = new Vector3(removedLane * laneDistance, fy, spawnZ);
                ObjectPoolManager.Instance.SpawnFromPool(filler.name, fillerPos, fRot, obstaclesRoot);
            }
            return;
        }

        if (prefab.GetComponent<JumpObstacle>() || prefab.GetComponent<SlideObstacle>())
        {
            string type = prefab.GetComponent<JumpObstacle>() ? "JumpObstacle" : "SlideObstacle";

            if (forcedLane != int.MinValue)
            {
                Vector3 pos = new Vector3(forcedLane * laneDistance, y, spawnZ);
                ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
                OnObstacleSpawned?.Invoke(segment, type);
            }
            else
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector3 pos = new Vector3(lane * laneDistance, y, spawnZ);
                    ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, rot, obstaclesRoot);
                }
                OnObstacleSpawned?.Invoke(segment, type);
            }
        }
    }

    private GameObject PickObstaclePrefab(ObstacleSpawnerConfig config, float difficultyFactor, bool excludeFullLane = false)
    {
        if (UsingRobotProfile && robotSpawnProfile.useWeightedPick)
            return PickWeightedPrefab(config, difficultyFactor, excludeFullLane);

        return PickRandomPrefab(config, excludeFullLane);
    }

    private GameObject PickRandomPrefab(ObstacleSpawnerConfig config, bool excludeFullLane)
    {
        List<GameObject> pool = BuildFlatPool(config, excludeFullLane);
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private GameObject PickWeightedPrefab(ObstacleSpawnerConfig config, float difficultyFactor, bool excludeFullLane)
    {
        List<GameObject> jumpSlide = new List<GameObject>();
        List<GameObject> laneBlockers = new List<GameObject>();
        List<GameObject> fullLane = new List<GameObject>();

        if (config.jumpOverObstacles != null) jumpSlide.AddRange(config.jumpOverObstacles);
        if (config.slideUnderObstacles != null) jumpSlide.AddRange(config.slideUnderObstacles);
        if (config.avoidLaneObstacles != null) laneBlockers.AddRange(config.avoidLaneObstacles);
        if (!excludeFullLane && config.fullLaneObstacles != null) fullLane.AddRange(config.fullLaneObstacles);

        float wJump = robotSpawnProfile.weightJumpSlide * (1f - difficultyFactor * 0.7f);
        float wLane = robotSpawnProfile.weightLaneBlocker * Mathf.Lerp(0.5f, 1.2f, difficultyFactor);
        float wFull = robotSpawnProfile.weightFullLane * difficultyFactor;

        if (!excludeFullLane && fullLane.Count > 0 && Random.value > robotSpawnProfile.fullLaneMaxChance)
            wFull = 0f;

        int bucket = PickWeightedBucket(wJump, wLane, wFull, jumpSlide.Count, laneBlockers.Count, fullLane.Count);
        switch (bucket)
        {
            case 0: return jumpSlide.Count > 0 ? jumpSlide[Random.Range(0, jumpSlide.Count)] : null;
            case 1: return laneBlockers.Count > 0 ? laneBlockers[Random.Range(0, laneBlockers.Count)] : null;
            case 2: return fullLane.Count > 0 ? fullLane[Random.Range(0, fullLane.Count)] : null;
            default: return PickRandomPrefab(config, excludeFullLane);
        }
    }

    private static int PickWeightedBucket(float w0, float w1, float w2, int c0, int c1, int c2)
    {
        float total = 0f;
        if (c0 > 0) total += w0;
        if (c1 > 0) total += w1;
        if (c2 > 0) total += w2;
        if (total <= 0f) return -1;

        float roll = Random.value * total;
        if (c0 > 0)
        {
            if (roll < w0) return 0;
            roll -= w0;
        }
        if (c1 > 0)
        {
            if (roll < w1) return 1;
            roll -= w1;
        }
        if (c2 > 0) return 2;
        return -1;
    }

    private static List<GameObject> BuildFlatPool(ObstacleSpawnerConfig config, bool excludeFullLane)
    {
        List<GameObject> pool = new List<GameObject>();
        if (config.jumpOverObstacles != null) pool.AddRange(config.jumpOverObstacles);
        if (config.slideUnderObstacles != null) pool.AddRange(config.slideUnderObstacles);
        if (config.avoidLaneObstacles != null) pool.AddRange(config.avoidLaneObstacles);
        if (!excludeFullLane && config.fullLaneObstacles != null) pool.AddRange(config.fullLaneObstacles);
        return pool;
    }

    private GameObject PickJumpOrSlide(ObstacleSpawnerConfig config)
    {
        List<GameObject> pool = new List<GameObject>();
        if (config.jumpOverObstacles != null) pool.AddRange(config.jumpOverObstacles);
        if (config.slideUnderObstacles != null) pool.AddRange(config.slideUnderObstacles);
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private void HandleEgyptTimelineSwap()
    {
        foreach (Transform child in obstaclesRoot)
            child.gameObject.SetActive(false);

        activeConfig = egyptConfig;
        segmentCounter = 0;
        lastSpawnZ = -999f;
        lastSpawnDistance = -999f;
    }

    private float GetDifficultyFactor()
    {
        if (UsingRobotProfile)
            return robotSpawnProfile.EvaluateDifficultyFactor(playerDistanceTravelled, useDifficultyScaling);

        if (!useDifficultyScaling) return 0f;
        return Mathf.Clamp01(playerDistanceTravelled / distanceForMaxDifficulty);
    }

    private float GetComboChance(float difficultyFactor)
    {
        if (UsingRobotProfile)
            return robotSpawnProfile.EvaluateComboChance(difficultyFactor);
        return Mathf.Lerp(comboChanceStart, comboChanceEnd, difficultyFactor);
    }

    private float GetGroupSpacing(float difficultyFactor)
    {
        if (UsingRobotProfile)
            return robotSpawnProfile.EvaluateGroupSpacing(difficultyFactor);
        return Mathf.Lerp(minGroupSpacing, maxGroupSpacing, difficultyFactor);
    }

    private int GetEmptySegmentInterval()
    {
        if (UsingRobotProfile) return robotSpawnProfile.emptySegmentInterval;
        return emptySegmentInterval;
    }

    private bool GetEmptySegmentAfterCombo()
    {
        if (UsingRobotProfile) return robotSpawnProfile.emptySegmentAfterCombo;
        return emptySegmentAfterCombo;
    }

    private static bool HasAnyPrefabs(ObstacleSpawnerConfig config)
    {
        return (config.jumpOverObstacles != null && config.jumpOverObstacles.Length > 0) ||
               (config.slideUnderObstacles != null && config.slideUnderObstacles.Length > 0) ||
               (config.avoidLaneObstacles != null && config.avoidLaneObstacles.Length > 0) ||
               (config.fullLaneObstacles != null && config.fullLaneObstacles.Length > 0);
    }

    private float GetLaneDistance()
    {
        if (playerMovement != null)
            return Mathf.Max(0.1f, playerMovement.LaneDistanceValue);
        if (PlayerMovement.Instance != null)
            return Mathf.Max(0.1f, PlayerMovement.Instance.LaneDistanceValue);
        return 5f;
    }

    private float GetRunDistance()
    {
        if (WorldScrollController.Instance != null)
            return WorldScrollController.Instance.DistanceTravelled;
        return player != null ? player.position.z : 0f;
    }

    private void TryRegisterRoot()
    {
        if (rootRegistered || obstaclesRoot == null || WorldScrollController.Instance == null) return;
        WorldScrollController.Instance.RegisterScrollRoot(obstaclesRoot);
        rootRegistered = true;
    }

    private void ApplyWorldScrollProfile()
    {
        if (scrollSpeedProfile == null) return;

        WorldScrollController controller = WorldScrollController.Instance;
        if (controller == null)
            controller = FindFirstObjectByType<WorldScrollController>();

        if (controller != null)
            controller.ApplySpeedProfile(scrollSpeedProfile);
    }
}
