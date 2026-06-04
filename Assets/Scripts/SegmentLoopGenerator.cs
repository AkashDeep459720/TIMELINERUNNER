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

    [Header("Egypt Runway (portal swap)")]
    [Tooltip("Segments spawned synchronously during portal swap (before fade-in).")]
    [SerializeField] private int egyptRunwaySegmentCount = 7;

    public int EgyptRunwayBurstCount => egyptRunwaySegmentCount;

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
            inEgyptTimeline = true;
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

        int burstCount = Mathf.Max(count, egyptRunwaySegmentCount, maxSegments);
        for (int i = 0; i < burstCount; i++)
        {
            GameObject prefab = pool[Random.Range(0, pool.Length)];
            SpawnSegment(prefab);
        }

        egyptRunwaySpawned = true;
        PortalEvents.OnEgyptTimelineEntered?.Invoke();
    }

    /// <summary>
    /// Masked portal swap: clear robot tiles, align spawn cursor to player, spawn Egypt runway in place.
    /// </summary>
    public void PrepareEgyptRunwayAtPlayer(Transform playerTransform)
    {
        if (playerTransform == null) return;

        EnsureRoots();
        SwitchToEgyptTimeline();

        ClearActiveSegments();

        float playerWorldZ = playerTransform.position.z;
        float rootZ = segmentsRoot.position.z;
        zSpawn = playerWorldZ - rootZ;

        egyptRunwaySpawned = false;

        GameObject[] pool = egyptSegments;
        if (pool == null || pool.Length == 0) return;

        int burstCount = Mathf.Max(egyptRunwaySegmentCount, maxSegments);
        for (int i = 0; i < burstCount; i++)
        {
            GameObject prefab = pool[Random.Range(0, pool.Length)];
            SpawnSegment(prefab);
        }

        egyptRunwaySpawned = true;
        portalSpawned = true;
        portalQueued = false;

        PortalEvents.OnEgyptTimelineEntered?.Invoke();
    }

    private void ClearActiveSegments()
    {
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            if (activeSegments[i] != null)
                Destroy(activeSegments[i]);
        }

        activeSegments.Clear();
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

        Physics.SyncTransforms();
        worldPos = ResolvePortalGroundedPosition(worldPos, segment);
        if (Blocked(worldPos)) return false;

        Quaternion finalRot = worldRot * Quaternion.Euler(portalEulerOffset);
        SpawnPortal(worldPos, finalRot, segment);
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
        Physics.SyncTransforms();
        pos = ResolvePortalGroundedPosition(pos, null);
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(portalEulerOffset);

        SpawnPortal(pos, rot, null);
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

    private void SpawnPortal(Vector3 pos, Quaternion rot, GameObject hostSegment)
    {
        EnsureRoots();
        GameObject portal = Instantiate(portalPrefab, pos, rot, portalsRoot);

        PortalGroundSnap snap = portal.GetComponent<PortalGroundSnap>();
        if (snap == null)
            snap = portal.AddComponent<PortalGroundSnap>();
        snap.Configure(portalBaseYOffset, groundSnapMask, groundSnapRayHeight, groundSnapRayDistance);
        snap.SnapToGroundDeferred();

        lastPortalDistance = GetRunDistance();
        portalSpawned = true;
        portalQueued = false;
        queuedTime = -1f;
        PortalEvents.OnPortalSpawned?.Invoke(pos);
    }

    public bool IsInEgyptTimeline => inEgyptTimeline;

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

    private Vector3 ResolvePortalGroundedPosition(Vector3 worldPos, GameObject hostSegment)
    {
        float deckY = hostSegment != null ? GetSegmentDeckWorldY(hostSegment) : float.NaN;
        float rayGroundY = SampleLowestGroundY(worldPos, hostSegment);

        float baseY;
        if (!float.IsNaN(rayGroundY))
            baseY = rayGroundY;
        else if (!float.IsNaN(deckY))
            baseY = deckY;
        else
            baseY = groundY;

        worldPos.y = baseY + portalBaseYOffset;
        return worldPos;
    }

    private float GetSegmentDeckWorldY(GameObject segment)
    {
        if (segment == null) return float.NaN;

        bool hasBounds = false;
        Bounds combined = default;

        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (!hasBounds)
            {
                combined = renderers[i].bounds;
                hasBounds = true;
            }
            else
                combined.Encapsulate(renderers[i].bounds);
        }

        Collider[] colliders = segment.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || colliders[i].isTrigger) continue;
            if (!hasBounds)
            {
                combined = colliders[i].bounds;
                hasBounds = true;
            }
            else
                combined.Encapsulate(colliders[i].bounds);
        }

        return hasBounds ? combined.min.y : float.NaN;
    }

    private float SampleLowestGroundY(Vector3 worldPos, GameObject hostSegment)
    {
        Vector3 rayOrigin = worldPos + Vector3.up * groundSnapRayHeight;
        float maxDist = groundSnapRayHeight + groundSnapRayDistance;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, maxDist, groundSnapMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return float.NaN;

        Transform hostRoot = hostSegment != null ? hostSegment.transform : null;
        float lowest = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hostRoot != null && hits[i].collider.transform.IsChildOf(hostRoot))
                continue;

            if (hits[i].point.y < lowest)
                lowest = hits[i].point.y;
        }

        return lowest < float.MaxValue ? lowest : float.NaN;
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
