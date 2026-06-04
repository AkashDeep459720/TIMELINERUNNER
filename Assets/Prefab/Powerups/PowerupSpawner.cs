using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
    [Header("Setup")]
    public SegmentLoopGenerator segmentGenerator;
    public Transform powerupsRoot;

    [Header("Powerup Prefabs (set names same as pool tags)")]
    public GameObject magnetPrefab;
    public GameObject shieldPrefab;
    public GameObject speedPrefab;

    [Header("Spawn Rules")]
    [Tooltip("World distance between powerup groups.")]
    public float minGroupSpacing = 100f;
    [Tooltip("Chance (0-1) of spawning powerups per segment).")]
    [Range(0f, 1f)] public float spawnChance = 0.2f;

    [Header("Debug Forces (Inspector)")]
    public bool forceMagnet;
    public bool forceShield;
    public bool forceSpeed;
    public bool forceAll;

    private float lastSpawnZ = -999f;
    private float lastSpawnDistance = -999f;
    private bool rootRegistered;

    private void Start()
    {
        if (!segmentGenerator) segmentGenerator = FindFirstObjectByType<SegmentLoopGenerator>();
        if (!powerupsRoot)
        {
            GameObject root = new GameObject("PowerupsRoot");
            powerupsRoot = root.transform;
        }

        TryRegisterRoot();

        SegmentLoopGenerator.OnSegmentSpawned += HandleSegmentSpawned;
    }

    private void OnDestroy()
    {
        SegmentLoopGenerator.OnSegmentSpawned -= HandleSegmentSpawned;
    }

    private void HandleSegmentSpawned(GameObject segment)
    {
        TryRegisterRoot();
        float segZ = segment.transform.position.z;
        float runDistance = GetRunDistance();

        // spacing check
        if (runDistance - lastSpawnDistance < minGroupSpacing) return;

        // debug overrides
        if (forceAll) { SpawnAll(segZ); return; }
        if (forceMagnet) { SpawnSingle(magnetPrefab, segZ); return; }
        if (forceShield) { SpawnSingle(shieldPrefab, segZ); return; }
        if (forceSpeed) { SpawnSingle(speedPrefab, segZ); return; }

        // normal random roll
        if (Random.value > spawnChance) return;

        int roll = Random.Range(1, 4); // 1=single,2=pair,3=all three
        if (roll == 1) SpawnSingle(GetRandomPowerup(), segZ);
        else if (roll == 2) SpawnPair(segZ);
        else SpawnAll(segZ);
    }

    // ---------------- Spawn helpers ----------------
    private GameObject GetRandomPowerup()
    {
        int roll = Random.Range(0, 3);
        if (roll == 0) return magnetPrefab;
        if (roll == 1) return shieldPrefab;
        return speedPrefab;
    }

    private void SpawnSingle(GameObject prefab, float z)
    {
        if (!prefab) return;
        float laneDistance = GetLaneDistance();
        int lane = Random.Range(-1, 2); // -1,0,1
        Vector3 pos = new Vector3(lane * laneDistance, prefab.transform.position.y, z + 10f);
        ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, Quaternion.identity, powerupsRoot);
        lastSpawnZ = z;
        lastSpawnDistance = GetRunDistance();
    }

    private void SpawnPair(float z)
    {
        float laneDistance = GetLaneDistance();
        List<GameObject> pool = new List<GameObject> { magnetPrefab, shieldPrefab, speedPrefab };
        pool.RemoveAt(Random.Range(0, pool.Count)); // remove one

        int laneIndex = -1;
        foreach (var prefab in pool)
        {
            if (!prefab) continue;
            Vector3 pos = new Vector3(laneIndex * laneDistance, prefab.transform.position.y, z + 10f);
            ObjectPoolManager.Instance.SpawnFromPool(prefab.name, pos, Quaternion.identity, powerupsRoot);
            laneIndex += 2; // -1 → +1
        }
        lastSpawnZ = z;
        lastSpawnDistance = GetRunDistance();
    }

    private void SpawnAll(float z)
    {
        float laneDistance = GetLaneDistance();
        GameObject[] all = { magnetPrefab, shieldPrefab, speedPrefab };
        int[] lanes = { -1, 0, 1 };

        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i]) continue;
            Vector3 pos = new Vector3(lanes[i] * laneDistance, all[i].transform.position.y, z + 10f);
            ObjectPoolManager.Instance.SpawnFromPool(all[i].name, pos, Quaternion.identity, powerupsRoot);
        }
        lastSpawnZ = z;
        lastSpawnDistance = GetRunDistance();
    }

    private float GetLaneDistance()
    {
        if (PlayerMovement.Instance != null)
            return Mathf.Max(0.1f, PlayerMovement.Instance.LaneDistanceValue);
        return 5f;
    }

    private void TryRegisterRoot()
    {
        if (rootRegistered || powerupsRoot == null || WorldScrollController.Instance == null) return;
        WorldScrollController.Instance.RegisterScrollRoot(powerupsRoot);
        rootRegistered = true;
    }

    private float GetRunDistance()
    {
        if (WorldScrollController.Instance != null)
            return WorldScrollController.Instance.DistanceTravelled;
        if (PlayerMovement.Instance != null)
            return PlayerMovement.Instance.transform.position.z;
        return 0f;
    }
}
