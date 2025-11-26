using UnityEngine;

public class TimedOrbSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject orbPrefab;

    [Header("Timing")]
    [Tooltip("How often to spawn an orb (seconds).")]
    public float spawnIntervalSeconds = 60f;
    public bool spawnImmediatelyOnStart = false;

    [Header("Stop Conditions")]
    [Tooltip("Automatically stop spawning when OrbManager completes.")]
    public bool stopWhenOrbsCompleted = true;

    [Header("Placement / Track")]
    public float laneWidth = 2.2f;
    public float minAheadZ = 25f;
    public float maxAheadZ = 45f;
    public float rayStartHeight = 3.0f;
    public float rayDownDistance = 10.0f;
    public LayerMask groundMask = ~0;

    [Header("Safety / Obstacle Check")]
    public bool avoidObstacles = true;
    public LayerMask obstacleMask = 0;
    public float clearRadius = 0.35f;

    [Header("Rotation / Orientation")]
    public bool alignToGroundNormal = false;
    public Vector3 rotationEulerOffset = new Vector3(-90f, 0f, 0f);
    public float liftY = 0.02f;

    [Header("Cleanup")]
    public float despawnBehindDistance = 60f;

    private float timer;

    private void OnEnable()
    {
        // Subscribe to completion so we can stop instantly the moment the 5th orb is picked.
        if (stopWhenOrbsCompleted && OrbManager.Instance != null)
            OrbManager.Instance.OnTargetReached += HandleOrbsCompleted;
    }

    private void OnDisable()
    {
        if (OrbManager.Instance != null)
            OrbManager.Instance.OnTargetReached -= HandleOrbsCompleted;
    }

    private void Start()
    {
        if (spawnImmediatelyOnStart) timer = spawnIntervalSeconds;

        // If the run already completed (e.g., scene reload), stop right away.
        if (stopWhenOrbsCompleted && OrbManager.Instance != null && OrbManager.Instance.Completed)
        {
            Debug.Log("[TimedOrbSpawner] Stopping: orb goal already completed.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!player || !orbPrefab) return;

        // Belt & suspenders: also guard in Update in case subscription was missed.
        if (stopWhenOrbsCompleted && OrbManager.Instance != null && OrbManager.Instance.Completed)
        {
            if (enabled)
            {
                Debug.Log("[TimedOrbSpawner] Stopping in Update: orb goal completed.");
                enabled = false;
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnIntervalSeconds)
        {
            timer = 0f;
            TrySpawnOrbAhead();
        }
    }

    private void HandleOrbsCompleted()
    {
        if (!stopWhenOrbsCompleted) return;
        Debug.Log("[TimedOrbSpawner] OnTargetReached received → disabling spawner now.");
        enabled = false;
    }

    private void TrySpawnOrbAhead()
    {
        int[] lanes = new int[] { -1, 0, +1 };
        Shuffle(lanes);

        float ahead = Random.Range(minAheadZ, maxAheadZ);

        foreach (int laneIndex in lanes)
        {
            float x = laneIndex * laneWidth;
            Vector3 rayStart = new Vector3(x, player.position.y + rayStartHeight, player.position.z + ahead);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayStartHeight + rayDownDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 spawnPos = hit.point + Vector3.up * liftY;

                if (avoidObstacles && obstacleMask != 0)
                {
                    if (Physics.CheckSphere(spawnPos + Vector3.up * 0.5f, clearRadius, obstacleMask, QueryTriggerInteraction.Ignore))
                        continue;
                }

                Quaternion baseRot = alignToGroundNormal
                    ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                    : Quaternion.identity;

                Quaternion finalRot = baseRot * Quaternion.Euler(rotationEulerOffset);

                GameObject orb = Instantiate(orbPrefab, spawnPos, finalRot);
                var sd = orb.AddComponent<SelfDestructBehindPlayer>();
                sd.player = player;
                sd.distance = despawnBehindDistance;
                return;
            }
        }
    }

    private void Shuffle(int[] a)
    {
        for (int i = 0; i < a.Length; i++)
        {
            int j = Random.Range(i, a.Length);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    private class SelfDestructBehindPlayer : MonoBehaviour
    {
        public Transform player;
        public float distance = 60f;

        private void Update()
        {
            if (!player) return;
            if (player.position.z - transform.position.z > distance)
                Destroy(gameObject);
        }
    }
}
