using System.Collections.Generic;
using UnityEngine;

public class SegmentLoopGenerator : MonoBehaviour
{
    // 🔔 Event fired whenever a segment is spawned
    public static System.Action<GameObject> OnSegmentSpawned;

    [Header("Segment Pools")]
    public GameObject[] robotSegments;
    public GameObject[] egyptSegments;

    [Header("Portal Spawn")]
    [Tooltip("Portal prefab with PortalTrigger + trigger collider + (optional) kinematic RB")]
    public GameObject portalPrefab;
    [Tooltip("Minimum Z distance between portals (safety gate)")]
    public float minPortalSpacing = 200f;
    [Tooltip("Layers that would block portal placement (props/obstacles). For testing, set to 'Nothing'.")]
    public LayerMask portalBlockMask;
    [Tooltip("Overlap box half-extents for block check")]
    public Vector3 portalCheckSize = new Vector3(1.2f, 2f, 1.2f);

    [Header("Sockets & Fallbacks")]
    [SerializeField] private bool useFallbackIfNoSocket = true;
    [SerializeField] private bool emergencyFallbackEnabled = true;
    [SerializeField] private float emergencyAhead = 25f;
    [SerializeField] private float groundY = 0.2f;
    [SerializeField] private bool emergencyAlignToPlayerX = true;

    [Header("Placement Tweaks")]
    [SerializeField] private Vector3 portalEulerOffset = new Vector3(-90f, 0f, 0f);
    [SerializeField] private float socketYOffset = 0.25f;

    [Header("Immediate Placement")]
    [SerializeField] private int immediateExtraSegments = 2;

    [Header("Player / Timeline")]
    public Transform player;
    public int maxSegments = 6;
    [SerializeField] private float recycleBehindPlayerDistance = 60f;

    [Header("Egypt Landing (optional)")]
    public Transform egyptLandingPoint;

    [Header("World Roots")]
    [SerializeField] private Transform segmentsRoot;
    [SerializeField] private Transform portalsRoot;

    [Header("Portal Grounding")]
    [SerializeField] private float portalBaseYOffset = 1.93f;
    [SerializeField] private LayerMask groundSnapMask = ~0;
    [SerializeField] private float groundSnapRayHeight = 6f;
    [SerializeField] private float groundSnapRayDistance = 18f;

    [Header("Egypt Alignment")]
    [SerializeField] private bool autoAlignEgyptSegments = true;
    [SerializeField] private float egyptLaneCenterX = 0f;
    [SerializeField] private float maxEgyptCenterCorrection = 8f;

    // ----- internals -----
    private float zSpawn = 0f;
    private const float segmentLength = 50f;
    private readonly List<GameObject> activeSegments = new List<GameObject>();

    private bool inEgyptTimeline = false;
    private float lastPortalDistance = -99999f;

    private bool portalQueued = false;
    private bool portalSpawned = false;
    private bool subscribed = false;
    private float queuedTime = -1f;

    private bool egyptRunwaySpawned = false;
    private bool rootsRegistered = false;

    // -------------- Lifecycle --------------
    private void OnEnable()
    {
        TrySubscribeToOrbEvent();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        EnsureRoots();
        RegisterRootsWithScrollController();

        // Boot runway
        for (int i = 0; i < maxSegments; i++)
        {
            if (i < 2 && robotSegments != null && robotSegments.Length > 0)
            {
                SpawnSegment(robotSegments[0]); // safe tiles first
            }
            else
            {
                GameObject[] pool = GetCurrentPool();
                if (pool != null && pool.Length > 0)
                {
                    SpawnSegment(pool[Random.Range(0, pool.Length)]);
                }
            }
        }

        if (!subscribed && OrbManager.Instance != null)
            TrySubscribeToOrbEvent();
    }

    private void Update()
    {
        if (GameState.IsPaused) return;
        if (!rootsRegistered) RegisterRootsWithScrollController();
        if (player == null || activeSegments.Count == 0) return;

        // recycle head segment
        if (activeSegments[0].transform.position.z < player.position.z - recycleBehindPlayerDistance)
        {
            var pool = GetCurrentPool();
            if (pool != null && pool.Length > 0)
            {
                var prefab = pool[Random.Range(0, pool.Length)];
                SpawnSegment(prefab);
            }

            Destroy(activeSegments[0]);
            activeSegments.RemoveAt(0);
        }

        // Emergency fallback portal placement
        if (emergencyFallbackEnabled && portalQueued && !portalSpawned && queuedTime > 0f && Time.time - queuedTime > 1.0f)
        {
            TrySpawnPortalAheadOfPlayer();
        }
    }

    // -------------- Orb Event --------------
    private void TrySubscribeToOrbEvent()
    {
        if (subscribed) return;
        if (OrbManager.Instance == null) return;

        OrbManager.Instance.OnTargetReached += QueuePortal;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (subscribed && OrbManager.Instance != null)
        {
            OrbManager.Instance.OnTargetReached -= QueuePortal;
            subscribed = false;
        }
    }

    private void QueuePortal()
    {
        portalQueued = true;
        queuedTime = Time.time;

        if (!portalSpawned)
        {
            TryPlacePortalImmediately();
        }
    }

    // -------------- Spawning --------------
    private void SpawnSegment(GameObject prefab)
    {
        if (prefab == null) return;

        EnsureRoots();
        GameObject segment = Instantiate(prefab, segmentsRoot);
        segment.transform.localPosition = new Vector3(0f, 0f, zSpawn);
        segment.transform.localRotation = Quaternion.identity;

        if (inEgyptTimeline && autoAlignEgyptSegments)
            AlignEgyptSegment(segment);

        activeSegments.Add(segment);

        // 🔔 Notify listeners (ObstacleSpawner etc.)
        OnSegmentSpawned?.Invoke(segment);

        if (!inEgyptTimeline && portalQueued && !portalSpawned && portalPrefab != null)
        {
            TrySpawnPortalOn(segment);
        }

        zSpawn += segmentLength;
    }

    private GameObject[] GetCurrentPool()
    {
        return inEgyptTimeline ? egyptSegments : robotSegments;
    }

    public void SwitchToEgyptTimeline()
    {
        if (!inEgyptTimeline)
        {
            inEgyptTimeline = true;
            Debug.Log("🟠 [Gen] Switched to Egypt timeline");
        }
    }

    public void ResetZ(float newStartZ)
    {
        if (newStartZ > zSpawn) zSpawn = newStartZ;
    }

    public void ForceSpawnEgyptSegments(int count)
    {
        if (!inEgyptTimeline) return;
        if (egyptRunwaySpawned) return;

        GameObject[] pool = GetCurrentPool();
        if (pool == null || pool.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = pool[Random.Range(0, pool.Length)];
            SpawnSegment(prefab);
        }

        egyptRunwaySpawned = true;
        Debug.Log($"📦 [Gen] Forced spawn of {count} Egypt segments at z = {zSpawn}");
    }

    public float GetCurrentZSpawn() => zSpawn;

    // -------------- Portal Placement --------------
    private bool TrySpawnPortalOn(GameObject segment)
    {
        if (portalSpawned) return false;

        float currentDistance = GetRunDistance();
        if (currentDistance - lastPortalDistance < minPortalSpacing)
        {
            return false;
        }

        Transform socket = FindDeepChild(segment.transform, "PortalSocket");

        Vector3 worldPos;
        Quaternion worldRot;

        if (socket == null)
        {
            if (!useFallbackIfNoSocket) return false;

            worldPos = segment.transform.position + new Vector3(0f, groundY, 0.25f * segmentLength);
            worldRot = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }
        else
        {
            worldPos = socket.position;
            worldPos.y += socketYOffset;
            worldRot = socket.rotation;
        }

        worldPos = ResolvePortalGroundedPosition(worldPos);
        if (Blocked(worldPos)) return false;

        Quaternion finalRot = worldRot * Quaternion.Euler(portalEulerOffset);
        SpawnPortal(worldPos, finalRot);
        return true;
    }

    private bool TryPlacePortalImmediately()
    {
        if (activeSegments.Count > 0)
        {
            var lastSeg = activeSegments[activeSegments.Count - 1];
            if (TrySpawnPortalOn(lastSeg)) return true;
        }

        var pool = GetCurrentPool();
        if (pool == null || pool.Length == 0) return false;

        for (int i = 0; i < immediateExtraSegments && !portalSpawned; i++)
        {
            var prefab = pool[Random.Range(0, pool.Length)];
            SpawnSegment(prefab);
            if (portalSpawned) return true;
        }

        return portalSpawned;
    }

    private void TrySpawnPortalAheadOfPlayer()
    {
        if (!emergencyFallbackEnabled) return;
        if (portalSpawned || portalPrefab == null || player == null) return;

        float z = player.position.z + emergencyAhead;
        float x = emergencyAlignToPlayerX ? player.position.x : 0f;

        Vector3 pos = new Vector3(x, groundY, z);
        pos = ResolvePortalGroundedPosition(pos);
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(portalEulerOffset);

        SpawnPortal(pos, rot);
    }

    private bool Blocked(Vector3 worldPos)
    {
        return Physics.CheckBox(
            worldPos,
            portalCheckSize * 0.5f,
            Quaternion.identity,
            portalBlockMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void SpawnPortal(Vector3 pos, Quaternion rot)
    {
        EnsureRoots();
        var go = Instantiate(portalPrefab, pos, rot, portalsRoot);

        var trig = go.GetComponent<PortalTrigger>();
        if (trig != null && egyptLandingPoint != null)
        {
            trig.SetDestination(egyptLandingPoint);
        }

        lastPortalDistance = GetRunDistance();
        portalSpawned = true;
        portalQueued = false;
        queuedTime = -1f;
        PortalEvents.OnPortalSpawned?.Invoke(pos);

        Debug.Log($"🌀 [Gen] Portal spawned at Z {pos.z}");
    }

    private void AlignEgyptSegment(GameObject segment)
    {
        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        float correction = egyptLaneCenterX - b.center.x;
        correction = Mathf.Clamp(correction, -maxEgyptCenterCorrection, maxEgyptCenterCorrection);
        if (Mathf.Abs(correction) < 0.01f) return;

        segment.transform.position += Vector3.right * correction;
    }

    private Vector3 ResolvePortalGroundedPosition(Vector3 worldPos)
    {
        Vector3 rayOrigin = worldPos + Vector3.up * groundSnapRayHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSnapRayHeight + groundSnapRayDistance, groundSnapMask, QueryTriggerInteraction.Ignore))
        {
            worldPos.y = hit.point.y + portalBaseYOffset;
            return worldPos;
        }

        worldPos.y += portalBaseYOffset;
        return worldPos;
    }

    private float GetRunDistance()
    {
        if (WorldScrollController.Instance != null)
            return WorldScrollController.Instance.DistanceTravelled;

        return player != null ? player.position.z : 0f;
    }

    private void EnsureRoots()
    {
        if (segmentsRoot == null)
        {
            GameObject go = new GameObject("SegmentsRoot");
            segmentsRoot = go.transform;
        }

        if (portalsRoot == null)
        {
            GameObject go = new GameObject("PortalsRoot");
            portalsRoot = go.transform;
        }
    }

    private void RegisterRootsWithScrollController()
    {
        if (WorldScrollController.Instance == null) return;
        WorldScrollController.Instance.RegisterScrollRoot(segmentsRoot);
        WorldScrollController.Instance.RegisterScrollRoot(portalsRoot);
        rootsRegistered = true;
    }

    // --- helpers ---
    private Transform FindDeepChild(Transform parent, string name)
    {
        var q = new Queue<Transform>();
        q.Enqueue(parent);

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) q.Enqueue(t.GetChild(i));
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-2f, 0f, zSpawn), new Vector3(2f, 0f, zSpawn));
        UnityEditor.Handles.Label(new Vector3(0f, 1f, zSpawn), $"zSpawn = {zSpawn:F1}");
    }
#endif
}
