using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Header("Auto-binds if empty")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private SegmentLoopGenerator segmentGenerator;

    [Header("Landing")]
    [SerializeField] private Transform egyptLandingPoint;
    [SerializeField] private bool snapLandingToLane = true;

    [Header("Animator Triggers (optional)")]
    [SerializeField] private string enterPortalTrigger = "EnterPortal";
    [SerializeField] private string landInEgyptTrigger = "LandInEgypt";

    [Header("Timings")]
    [SerializeField] private float enterPortalDelay = 1.0f;
    [SerializeField] private float afterLandingDelay = 0.4f;

    [Header("Ground Snap (optional)")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float raycastUp = 3.0f;
    [SerializeField] private float raycastDown = 10.0f;
    [SerializeField] private float extraFootOffset = 0.01f;
    [SerializeField] private float fallbackY = 0.0f;
    [SerializeField] private float keepPlayerForwardZOffset = 0f;

    private bool triggered;
    private Collider myTrigger;

    private static bool teleporting = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        myTrigger = GetComponent<Collider>();

        if (!player)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }
        if (!playerMovement && player) playerMovement = player.GetComponent<PlayerMovement>();

        if (!playerAnimator && player)
        {
            var model = player.Find("PlayerModel");
            playerAnimator = model ? model.GetComponent<Animator>() : player.GetComponentInChildren<Animator>();
        }

        if (!segmentGenerator) segmentGenerator = FindFirstObjectByType<SegmentLoopGenerator>();

        if (!egyptLandingPoint)
        {
            var tagged = GameObject.FindWithTag("EgyptLandingPoint");
            if (tagged) egyptLandingPoint = tagged.transform;
            else
            {
                var named = GameObject.Find("EgyptLandingPoint");
                if (named) egyptLandingPoint = named.transform;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || teleporting) return;

        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
        if (!isPlayer) return;

        triggered = true;
        teleporting = true;

        if (myTrigger) myTrigger.enabled = false;

        StartCoroutine(DoPortalSequence());
    }

    private IEnumerator DoPortalSequence()
    {
        // 1) Pause movement & play enter anim
        if (playerMovement) playerMovement.enabled = false;
        if (WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(true);

        if (playerAnimator)
        {
            if (!string.IsNullOrEmpty(landInEgyptTrigger)) playerAnimator.ResetTrigger(landInEgyptTrigger);
            if (!string.IsNullOrEmpty(enterPortalTrigger)) playerAnimator.SetTrigger(enterPortalTrigger);
        }

        if (enterPortalDelay > 0f) yield return new WaitForSeconds(enterPortalDelay);

        // 2) Switch timeline & spawn Egypt runway
        if (segmentGenerator)
        {
            segmentGenerator.SwitchToEgyptTimeline();
            segmentGenerator.ForceSpawnEgyptSegments(2);

            // Wait for physics to fully initialize the new colliders
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
        }

        // 3) Teleport with ground snap
        if (player)
        {
            float landingZ = player.position.z + keepPlayerForwardZOffset;

            Vector3 target = egyptLandingPoint
                ? egyptLandingPoint.position
                : new Vector3(0f, fallbackY, landingZ);

            if (snapLandingToLane && playerMovement != null)
            {
                float laneDistance = Mathf.Max(0.1f, playerMovement.LaneDistanceValue);
                float snappedX = Mathf.Round(target.x / laneDistance) * laneDistance;
                target.x = snappedX;
            }

            // Raycast from WELL ABOVE the target to ensure we hit valid ground
            float yFinal = target.y;
            Vector3 rayStart = new Vector3(target.x, target.y + raycastUp + 5f, target.z);

            // Cast from multiple positions to ensure we hit the floor
            bool foundGround = false;
            Vector3[] testPositions = {
                rayStart,
                rayStart + Vector3.forward * 0.5f,
                rayStart + Vector3.back * 0.5f
            };

            foreach (Vector3 testPos in testPositions)
            {
                if (Physics.Raycast(testPos, Vector3.down, out RaycastHit hit, raycastUp + raycastDown + 10f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    yFinal = hit.point.y + extraFootOffset;
                    foundGround = true;
                    break;
                }
            }

            // Fallback: use landing point's Y if raycast failed
            if (!foundGround)
            {
                yFinal = egyptLandingPoint ? egyptLandingPoint.position.y : fallbackY;
            }

            player.position = new Vector3(target.x, yFinal, target.z);
            Physics.SyncTransforms();

            // Lock PlayerMovement's baseline for a few frames
            if (playerMovement != null && playerMovement.enableGroundSnap)
            {
                playerMovement.ForceResnapGround(yFinal);
            }
        }

        // 4) Landing anim
        if (playerAnimator && !string.IsNullOrEmpty(landInEgyptTrigger))
        {
            playerAnimator.ResetTrigger(enterPortalTrigger);
            playerAnimator.SetTrigger(landInEgyptTrigger);
        }

        if (afterLandingDelay > 0f) yield return new WaitForSeconds(afterLandingDelay);

        if (playerMovement) playerMovement.enabled = true;
        if (WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(false);

        // disable portal and release guard
        gameObject.SetActive(false);
        teleporting = false;
    }

    public void SetDestination(Transform dest) => egyptLandingPoint = dest;

    private void OnDisable()
    {
        if (teleporting && WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(false);
    }
}