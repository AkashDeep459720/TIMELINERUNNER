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

    [Header("Animator Triggers (optional)")]
    [SerializeField] private string enterPortalTrigger = "EnterPortal";
    [SerializeField] private string landInEgyptTrigger = "LandInEgypt";

    [Header("Timings")]
    [SerializeField] private float enterPortalDelay = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float afterLandingDelay = 0.4f;

    [Header("Fade")]
    [SerializeField] private PortalTransitionFade fadeController;

    private bool triggered;
    private Collider myTrigger;

    private static bool transitionActive;

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
        if (!fadeController) fadeController = PortalTransitionFade.GetOrCreate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || transitionActive) return;

        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
        if (!isPlayer) return;

        triggered = true;
        transitionActive = true;

        if (myTrigger) myTrigger.enabled = false;

        StartCoroutine(DoPortalSequence());
    }

    private IEnumerator DoPortalSequence()
    {
        if (playerMovement) playerMovement.enabled = false;
        if (WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(true);

        if (playerAnimator)
        {
            if (!string.IsNullOrEmpty(landInEgyptTrigger)) playerAnimator.ResetTrigger(landInEgyptTrigger);
            if (!string.IsNullOrEmpty(enterPortalTrigger)) playerAnimator.SetTrigger(enterPortalTrigger);
        }

        if (enterPortalDelay > 0f)
            yield return new WaitForSecondsRealtime(enterPortalDelay);

        PortalTransitionFade fade = fadeController != null ? fadeController : PortalTransitionFade.GetOrCreate();
        if (fade != null)
            yield return fade.FadeToBlack(fadeOutDuration);

        PerformTimelineSwap();

        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        if (TimelinePresentationController.Instance != null)
            TimelinePresentationController.Instance.OnEnterEgypt();

        if (fade != null)
            yield return fade.FadeFromBlack(fadeInDuration);

        if (playerAnimator && !string.IsNullOrEmpty(landInEgyptTrigger))
        {
            playerAnimator.ResetTrigger(enterPortalTrigger);
            playerAnimator.SetTrigger(landInEgyptTrigger);
        }

        if (afterLandingDelay > 0f)
            yield return new WaitForSecondsRealtime(afterLandingDelay);

        if (playerMovement) playerMovement.enabled = true;
        if (WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(false);

        gameObject.SetActive(false);
        transitionActive = false;
    }

    private void PerformTimelineSwap()
    {
        WorldScrollWorldCleaner.ClearAllPropRoots();

        if (segmentGenerator != null && player != null)
            segmentGenerator.PrepareEgyptRunwayAtPlayer(player);
        else if (segmentGenerator != null)
        {
            segmentGenerator.SwitchToEgyptTimeline();
            segmentGenerator.ForceSpawnEgyptSegments(segmentGenerator.EgyptRunwayBurstCount);
        }

        SnapPlayerGroundIfNeeded();
    }

    private void SnapPlayerGroundIfNeeded()
    {
        if (player == null || playerMovement == null || !playerMovement.enableGroundSnap) return;

        Vector3 origin = player.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            playerMovement.ForceResnapGround(hit.point.y + 0.01f);
    }

    private void OnDisable()
    {
        if (transitionActive && WorldScrollController.Instance != null)
            WorldScrollController.Instance.SetScrollPaused(false);
    }
}
