using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("Setup")]
    public Transform coinsRoot;
    public GameObject coinPrefab;

    [Header("Coin Heights")]
    public float coinGroundY = 1f;
    public float wavePeakY = 2.98f;

    [Header("Pattern Counts")]
    public int straightCount = 6;
    public int zigzagCount = 12;
    public int waveCount = 8;

    [Header("Debug Forces (Inspector)")]
    public bool forceStraight;
    public bool forceZigzag;
    public bool forceWave;
    public bool forceSlide;

    private void Start()
    {
        if (!coinsRoot)
        {
            GameObject root = new GameObject("CoinsRoot");
            coinsRoot = root.transform;
        }

        ObstacleSpawner.OnObstacleSpawned += HandleObstacleSpawned;
        SegmentLoopGenerator.OnSegmentSpawned += HandleSegmentSpawned;
    }

    private void OnDestroy()
    {
        ObstacleSpawner.OnObstacleSpawned -= HandleObstacleSpawned;
        SegmentLoopGenerator.OnSegmentSpawned -= HandleSegmentSpawned;
    }

    // --- When an obstacle spawns ---
    private void HandleObstacleSpawned(GameObject segment, string type)
    {
        if (forceStraight || forceZigzag || forceWave || forceSlide) return;

        switch (type)
        {
            case "JumpObstacle":
                SpawnWave(segment);
                break;
            case "SlideObstacle":
                SpawnSlide(segment);
                break;
            default:
                // let fallback handle
                break;
        }
    }

    // --- When a clean segment spawns (no obstacle event fired) ---
    private void HandleSegmentSpawned(GameObject segment)
    {
        if (forceStraight) { SpawnStraight(segment); return; }
        if (forceZigzag) { SpawnZigzag(segment); return; }
        if (forceWave) { SpawnWave(segment); return; }
        if (forceSlide) { SpawnSlide(segment); return; }

        // If no obstacles, random straight/zigzag
        if (Random.value < 0.5f) SpawnStraight(segment);
        else SpawnZigzag(segment);
    }

    // ---------------- Patterns ----------------
    private void SpawnStraight(GameObject segment)
    {
        int lane = Random.Range(-1, 2);
        float startZ = segment.transform.position.z + 10f;

        for (int i = 0; i < straightCount; i++)
            SpawnCoin(new Vector3(lane * 3f, coinGroundY, startZ + (i * 2f)));
    }

    private void SpawnZigzag(GameObject segment)
    {
        float startZ = segment.transform.position.z + 10f;
        int[] lanes = { -1, 0, 1, 0 };

        for (int i = 0; i < zigzagCount; i++)
        {
            int lane = lanes[i % lanes.Length];
            SpawnCoin(new Vector3(lane * 3f, coinGroundY, startZ + (i * 2f)));
        }
    }

    private void SpawnWave(GameObject segment)
    {
        int lane = 0;
        float startZ = segment.transform.position.z + 10f;

        for (int i = 0; i < waveCount; i++)
        {
            float t = (float)i / (waveCount - 1);
            float y = Mathf.Lerp(coinGroundY, wavePeakY, Mathf.Sin(t * Mathf.PI));
            SpawnCoin(new Vector3(lane * 3f, y, startZ + (i * 2f)));
        }
    }

    private void SpawnSlide(GameObject segment)
    {
        float startZ = segment.transform.position.z + 10f;
        for (int i = 0; i < straightCount; i++)
            SpawnCoin(new Vector3(0f, coinGroundY, startZ + (i * 2f)));
    }

    // ---------------- Helpers ----------------
    private void SpawnCoin(Vector3 pos)
    {
        if (!coinPrefab) return;
        Quaternion rot = Quaternion.Euler(90f, 0f, 0f); // upright
        ObjectPoolManager.Instance.SpawnFromPool(coinPrefab.name, pos, rot, coinsRoot);
    }
}
