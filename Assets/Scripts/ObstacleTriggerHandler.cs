using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(Collider))]
public class ObstacleTriggerHandler : MonoBehaviour
{
    [Header("Auto-Resolve Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string likelyAnimatorChildName = "PlayerModel";

    [Header("Optional Refs")]
    [SerializeField] private AudioSource collisionFX;
    [SerializeField] private GameObject mainCam;
    [SerializeField] private RetryDialogManager retryDialog;

    [Header("Animator Override Layer")]
    public int overrideLayerIndex = 1;
    public string stumbleStateName = "Stumble Backwards";

    [Header("Base Layer Control")]
    public string idleStateName = "Idle";
    public bool useIdleState = true;

    [Header("Camera")]
    public string cameraCollisionState = "CollisionCam";

    [Header("Timing")]
    public float minHoldRealtime = 0.6f;
    public float stumbleTransitionTime = 0.1f;

    [Header("Power-Ups")]
    [SerializeField] private PlayerShield playerShield; // 🛡️ Shield support

    [Header("Animation Smoothing")]
    [SerializeField] private float animatorFreezeDelay = 0.2f; // 🔧 NEW: Delay before freezing animator

    private GameObject thePlayer;
    private Animator playerAnim;
    private PlayerMovement playerMove;
    private Collider myTrigger;
    private bool triggered;

    private float originalAnimatorSpeed;
    private float originalLayerWeight;
    private string lastBaseLayerState;

    private void Awake()
    {
        myTrigger = GetComponent<Collider>();
        if (!myTrigger.isTrigger) myTrigger.isTrigger = true;

#if UNITY_2023_1_OR_NEWER
        if (!retryDialog) retryDialog = Object.FindFirstObjectByType<RetryDialogManager>();
#else
        if (!retryDialog) retryDialog = Object.FindObjectOfType<RetryDialogManager>();
#endif

        TryResolvePlayerRefs();

        // Auto-find PlayerShield if not assigned
        if (!playerShield && thePlayer)
            playerShield = thePlayer.GetComponent<PlayerShield>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!root || !root.CompareTag(playerTag))
        {
            TryResolvePlayerRefs();
            if (!thePlayer || !root || root.gameObject != thePlayer) return;
        }
        else
        {
            thePlayer = root.gameObject;
            TryResolveAnimatorAndMover();
        }

        // 🛡️ Shield check: ignore obstacle if active
        if (playerShield && playerShield.IsActive)
        {
            Debug.Log("🛡️ Shield active → obstacle ignored!");
            myTrigger.enabled = false;
            // Optional: Destroy(gameObject); // uncomment if you want the obstacle gone entirely
            return;
        }

        // Normal hit → trigger stumble/death flow
        triggered = true;
        myTrigger.enabled = false;
        StartCoroutine(HitFlow());
    }

    private IEnumerator HitFlow()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        if (playerMove) playerMove.isFrozen = true;

        if (collisionFX) collisionFX.Play();
        if (mainCam)
        {
            var camAnim = mainCam.GetComponent<Animator>();
            if (camAnim && !string.IsNullOrEmpty(cameraCollisionState))
                camAnim.Play(cameraCollisionState, 0, 0f);
        }

        // 🔧 FIX: Store original animator state before any modifications
        if (playerAnim)
        {
            originalAnimatorSpeed = playerAnim.speed;
            if (overrideLayerIndex < playerAnim.layerCount)
                originalLayerWeight = playerAnim.GetLayerWeight(overrideLayerIndex);

            AnimatorStateInfo baseLayerState = playerAnim.GetCurrentAnimatorStateInfo(0);
            lastBaseLayerState = GetStateNameFromHash(baseLayerState.fullPathHash);
        }

        // 🔧 FIX: Smooth transition to stumble animation
        if (playerAnim)
        {
            if (useIdleState && !string.IsNullOrEmpty(idleStateName))
                playerAnim.CrossFadeInFixedTime(idleStateName, stumbleTransitionTime, 0, 0f);

            if (overrideLayerIndex < playerAnim.layerCount)
            {
                playerAnim.SetLayerWeight(overrideLayerIndex, 1f);
                playerAnim.CrossFadeInFixedTime(stumbleStateName, stumbleTransitionTime, overrideLayerIndex, 0f);
            }
        }

        // 🔧 FIX: Wait for stumble animation to play before freezing
        yield return new WaitForSecondsRealtime(Mathf.Max(minHoldRealtime - animatorFreezeDelay, 0.1f));

        // 🔧 FIX: Gradually slow down animator for smoother freeze
        if (playerAnim)
        {
            float freezeTime = animatorFreezeDelay;
            float startSpeed = playerAnim.speed;
            float elapsed = 0f;

            while (elapsed < freezeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / freezeTime;
                playerAnim.speed = Mathf.Lerp(startSpeed, 0f, progress);
                yield return null;
            }

            playerAnim.speed = 0f;
        }

        // Wait any remaining time
        yield return new WaitForSecondsRealtime(animatorFreezeDelay);

        if (retryDialog)
        {
            retryDialog.SetCurrentObstacle(this);
            retryDialog.ShowRetryDialog();
        }
        else
        {
            Debug.LogWarning("⚠️ No RetryDialogManager found.");
        }
    }

    private string GetStateNameFromHash(int hash)
    {
        if (playerAnim == null) return "Unknown";
        string[] commonStates = { "Run", "Running", "Jump", "Slide", "Idle", "Stumble Backwards" };
        foreach (string stateName in commonStates)
        {
            if (Animator.StringToHash(stateName) == hash ||
                Animator.StringToHash("Base Layer." + stateName) == hash ||
                Animator.StringToHash("StumbleLayer." + stateName) == hash)
                return stateName;
        }
        return $"State_{hash}";
    }

    public void RestoreAnimatorState()
    {
        if (playerAnim == null) return;

        // 🔧 FIX: Smooth restoration of animator state
        StartCoroutine(SmoothRestoreAnimator());
    }

    // 🔧 NEW: Smooth animator restoration to prevent glitches
    private IEnumerator SmoothRestoreAnimator()
    {
        if (playerAnim == null) yield break;

        // Gradually restore animator speed
        float restoreTime = 0.1f;
        float elapsed = 0f;
        float startSpeed = playerAnim.speed;

        while (elapsed < restoreTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / restoreTime;
            playerAnim.speed = Mathf.Lerp(startSpeed, originalAnimatorSpeed, progress);
            yield return null;
        }

        // Ensure final values are set
        playerAnim.speed = originalAnimatorSpeed;

        if (overrideLayerIndex < playerAnim.layerCount)
            playerAnim.SetLayerWeight(overrideLayerIndex, originalLayerWeight);

        // Force animator update to apply changes immediately
        playerAnim.Update(0f);
    }

    private void TryResolvePlayerRefs()
    {
        if (!thePlayer)
        {
            var tagged = GameObject.FindWithTag(playerTag);
            if (tagged) thePlayer = tagged.transform.root.gameObject;
        }
        TryResolveAnimatorAndMover();
    }

    private void TryResolveAnimatorAndMover()
    {
        if (!thePlayer) return;

        if (!playerAnim)
        {
            if (!string.IsNullOrEmpty(likelyAnimatorChildName))
            {
                var child = thePlayer.transform.Find(likelyAnimatorChildName);
                if (child) playerAnim = child.GetComponentInChildren<Animator>(true);
            }
            if (!playerAnim) playerAnim = thePlayer.GetComponentInChildren<Animator>(true);
        }

        if (!playerMove) playerMove = thePlayer.GetComponent<PlayerMovement>();
    }
}