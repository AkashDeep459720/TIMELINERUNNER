using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float playerSpeed = 8f;
    public float slideSpeedMultiplier = 0.8f;
    public float laneDistance = 3f;
    public float laneSwitchSpeed = 14f;

    [Header("Immersion Settings")]
    public float tiltAngle = 15f;
    public float tiltLerpSpeed = 10f;
    private float targetTilt = 0f;
    private float currentTilt = 0f;
    private Coroutine tiltRoutine;

    [Header("Jump/Slide Settings")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.5f;
    public float slideDuration = 1f;

    [Header("Grounding")]
    public bool enableGroundSnap = true;
    public float rayStartOffset = 0.5f;
    public float raycastDown = 3.0f;
    public float footHeightOffset = 0.05f;
    public LayerMask groundMask = ~0;

    [Header("Mobile Ground Fix")]
    [SerializeField] private bool useMobileGroundFix = true;
    [SerializeField] private float mobileGroundBuffer = 0.1f;
    [SerializeField] private float groundSnapStrength = 0.8f;
    [SerializeField] private int groundCheckFrameDelay = 2;
    private int framesSinceLastGroundCheck = 0;

    [Header("Slide Collider")]
    public float slideColliderHeight = 1f;
    public Vector3 slideColliderCenter = new Vector3(0f, 0.5f, 0f);

    [Header("Speed Boost")]
    public float boostMultiplier = 1.5f;
    public float boostDuration = 5f;

    [Header("Difficulty Scaling")]
    public float speedIncreaseAmount = 0.2f;
    public float speedIncreaseInterval = 30f;
    private float nextSpeedIncreaseTime = 0f;

    [Header("Animator Sync")]
    public bool useAnimatorSpeedMultiplier = false;
    public float baseRunSpeed = 6f;
    public string speedParam = "Speed";

    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f;

    [Header("Animator State Names")]
    public string runStateName = "Run";
    public string jumpStateName = "Jump";
    public string slideStateName = "Slide";

    [Header("Interrupt Smoothness")]
    public float landingSmoothTime = 0.08f;

    [Header("Re-trigger Protection")]
    public float slideRetriggerBlock = 0.25f;

    [Header("Revival Settings")]
    [SerializeField] private float revivalGroundCheckDelay = 0.1f;
    [SerializeField] private bool forceGroundSnapOnRevive = true;
    [SerializeField] private float revivalHeightBuffer = 0.2f;
    [SerializeField] private float animatorResetDelay = 0.05f;

    [HideInInspector] public bool isFrozen = false;

    // Internals
    private bool isBoosted = false;
    private float originalSpeed;
    private int currentLane = 0;
    private bool isJumping = false;
    private bool isSliding = false;

    private bool slideRequestedOrActive = false;
    private bool slideQueuedAfterJump = false;
    private bool jumpCancelRequested = false;

    private Vector2 startTouchPos;
    private bool swipeDetected = false;
    private int activeTouchId = -1;

    private Animator animator;
    private CapsuleCollider capsuleCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    private Coroutine jumpRoutine;
    private Coroutine slideRoutine;

    private float lastGroundY;
    private float jumpStartGroundY;
    private float _lastSlideRequestTime = -999f;

    private bool isReviving = false;

    // Mobile-specific ground tracking
    private float confirmedGroundY;
    private bool hasValidGroundY = false;

    // NEW: skip ground checks for a few frames after portal teleport
    private int suppressGroundChecks = 0;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        originalSpeed = playerSpeed;
        Application.targetFrameRate = 60;

        InitializeGroundPosition();

        nextSpeedIncreaseTime = Time.timeSinceLevelLoad + speedIncreaseInterval;
    }

    void InitializeGroundPosition()
    {
        float initialY = transform.position.y;

        if (TryGetGroundY(out float detectedY))
        {
            lastGroundY = detectedY;
            confirmedGroundY = detectedY;
            hasValidGroundY = true;
        }
        else
        {
            Vector3 testPos = transform.position + Vector3.up * 5f;
            transform.position = testPos;

            if (TryGetGroundY(out detectedY))
            {
                lastGroundY = detectedY;
                confirmedGroundY = detectedY;
                hasValidGroundY = true;
            }
            else
            {
                lastGroundY = initialY;
                confirmedGroundY = initialY;
                hasValidGroundY = false;
            }
        }

        transform.position = new Vector3(transform.position.x, lastGroundY, transform.position.z);
        EnterRun(0.0f);
    }

    void Update()
    {
        if (GameState.IsPaused || isFrozen) return;

        framesSinceLastGroundCheck++;
        if (enableGroundSnap && framesSinceLastGroundCheck >= groundCheckFrameDelay)
        {
            if (suppressGroundChecks > 0)
            {
                suppressGroundChecks--; // skip a few frames after teleport
            }
            else
            {
                UpdateGroundPosition();
            }

            framesSinceLastGroundCheck = 0;
        }

        float moveSpeed = CurrentForwardSpeed();
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.World);

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.A) && currentLane > -1) { currentLane--; StartTilt(-1); }
        if (Input.GetKeyDown(KeyCode.D) && currentLane < 1) { currentLane++; StartTilt(1); }
        if (Input.GetKeyDown(KeyCode.Space)) TriggerJump();
        if (Input.GetKeyDown(KeyCode.S)) TriggerSlideOrQueue();
#endif

#if UNITY_ANDROID || UNITY_IOS
        DetectSwipe();
#endif

        float targetX = currentLane * laneDistance;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltLerpSpeed);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);

        if (enableGroundSnap && hasValidGroundY)
        {
            ApplyGroundSnapping();
        }

        SyncAnimator(moveSpeed);

        if (Time.timeSinceLevelLoad >= nextSpeedIncreaseTime)
        {
            playerSpeed += speedIncreaseAmount;
            nextSpeedIncreaseTime = Time.timeSinceLevelLoad + speedIncreaseInterval;
        }
    }

    void UpdateGroundPosition()
    {
        if (TryGetGroundY(out float newGroundY))
        {
            if (!hasValidGroundY || Mathf.Abs(newGroundY - confirmedGroundY) < 2f)
            {
                lastGroundY = newGroundY;
                confirmedGroundY = newGroundY;
                hasValidGroundY = true;
            }
            else
            {
                lastGroundY = Mathf.Lerp(confirmedGroundY, newGroundY, 0.1f);
            }
        }
    }

    void ApplyGroundSnapping()
    {
        Vector3 pos = transform.position;

        if (isSliding)
            pos.y = confirmedGroundY;
        else if (!isJumping && !isReviving)
            pos.y = Mathf.Lerp(pos.y, confirmedGroundY, groundSnapStrength);

        transform.position = pos;
    }

    // ---------- NEW: external resnap for portals ----------
    public void ForceResnapGround(float y)
    {
        lastGroundY = y;
        confirmedGroundY = y;
        hasValidGroundY = true;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        suppressGroundChecks = 10; // skip ground checks for ~10 frames
    }

    // ---------- SWIPE ----------
    void DetectSwipe()
    {
        if (Input.touchCount == 0) { activeTouchId = -1; return; }

        Touch t = default;
        bool found = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var tt = Input.GetTouch(i);
            if (activeTouchId == -1 || tt.fingerId == activeTouchId)
            {
                t = tt;
                activeTouchId = tt.fingerId;
                found = true;
                break;
            }
        }
        if (!found) return;

        switch (t.phase)
        {
            case TouchPhase.Began:
                swipeDetected = false;
                startTouchPos = t.position;
                break;
            case TouchPhase.Moved:
                if (swipeDetected) break;
                Vector2 d = t.position - startTouchPos;
                if (d.magnitude >= minSwipeDistance && Mathf.Abs(d.x) > Mathf.Abs(d.y))
                {
                    swipeDetected = true;
                    if (d.x > 0 && currentLane < 1) { currentLane++; StartTilt(1); }
                    else if (d.x < 0 && currentLane > -1) { currentLane--; StartTilt(-1); }
                }
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!swipeDetected)
                {
                    Vector2 delta = t.position - startTouchPos;
                    if (delta.magnitude >= minSwipeDistance && Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                    {
                        swipeDetected = true;
                        if (delta.y > 0) TriggerJump();
                        else TriggerSlideOrQueue();
                    }
                }
                activeTouchId = -1;
                break;
        }
    }

    // ---------- TILT ----------
    void StartTilt(int direction)
    {
        if (tiltRoutine != null) StopCoroutine(tiltRoutine);
        tiltRoutine = StartCoroutine(TiltCoroutine(direction));
    }

    IEnumerator TiltCoroutine(int direction)
    {
        targetTilt = direction * tiltAngle;
        yield return new WaitForSeconds(0.2f);
        targetTilt = 0f;
        yield return new WaitForSeconds(0.15f);
        currentTilt = 0f;
        transform.rotation = Quaternion.identity;
    }

    // ---------- JUMP ----------
    void TriggerJump()
    {
        if (isSliding) CancelSlideImmediate();
        if (isJumping) return;

        ResetTrigger("SlideTrigger");
        SetTrigger("JumpTrigger");
        CrossFadeIfExists(jumpStateName, 0.05f);

        if (jumpRoutine != null) StopCoroutine(jumpRoutine);
        jumpStartGroundY = confirmedGroundY;
        jumpCancelRequested = false;
        slideQueuedAfterJump = false;

        jumpRoutine = StartCoroutine(JumpArc());
    }

    IEnumerator JumpArc()
    {
        isJumping = true;
        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            if (jumpCancelRequested) break;
            float t = elapsed / Mathf.Max(0.0001f, jumpDuration);
            float progress = Mathf.Sin(t * Mathf.PI);
            float y = jumpStartGroundY + jumpHeight * progress;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (jumpCancelRequested)
        {
            float startY = transform.position.y;
            float t = 0f;
            while (t < landingSmoothTime)
            {
                float a = (landingSmoothTime <= 0f) ? 1f : t / landingSmoothTime;
                float y = Mathf.Lerp(startY, confirmedGroundY, a);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                t += Time.deltaTime;
                yield return null;
            }
        }

        transform.position = new Vector3(transform.position.x, confirmedGroundY, transform.position.z);
        SetTrigger("LandTrigger");
        isJumping = false;

        if (slideQueuedAfterJump)
        {
            slideQueuedAfterJump = false;
            if (!isSliding)
            {
                isSliding = true;
                if (slideRoutine != null) StopCoroutine(slideRoutine);
                slideRoutine = StartCoroutine(SlideCoroutine());
            }
        }
        else
        {
            EnterRun(0.05f);
        }
    }

    // ---------- SLIDE ----------
    void TriggerSlideOrQueue()
    {
        if (Time.time - _lastSlideRequestTime < slideRetriggerBlock) return;
        if (slideRequestedOrActive) return;
        _lastSlideRequestTime = Time.time;

        if (isJumping)
        {
            jumpCancelRequested = true;
            slideQueuedAfterJump = true;
            slideRequestedOrActive = true;
            return;
        }

        slideRequestedOrActive = true;
        if (isSliding) return;

        if (slideRoutine != null) StopCoroutine(slideRoutine);
        isSliding = true;
        slideRoutine = StartCoroutine(SlideCoroutine());
    }

    IEnumerator SlideCoroutine()
    {
        ResetTrigger("JumpTrigger");
        if (HasParam("SlideTrigger")) SetTrigger("SlideTrigger");
        else CrossFadeIfExists(slideStateName, 0.05f);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = slideColliderHeight;
            capsuleCollider.center = slideColliderCenter;
        }

        float t = 0f;
        while (t < slideDuration)
        {
            transform.position = new Vector3(transform.position.x, confirmedGroundY, transform.position.z);
            t += Time.deltaTime;
            yield return null;
        }

        RestoreCollider();
        isSliding = false;
        slideRequestedOrActive = false;
        EnterRun(0.05f);
    }

    void CancelSlideImmediate()
    {
        if (!isSliding && !slideRequestedOrActive) return;
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        RestoreCollider();
        isSliding = false;
        slideRequestedOrActive = false;
        ResetTrigger("SlideTrigger");
        transform.position = new Vector3(transform.position.x, confirmedGroundY, transform.position.z);
        EnterRun(0.05f);
    }

    void RestoreCollider()
    {
        if (capsuleCollider == null) return;
        capsuleCollider.height = originalColliderHeight;
        capsuleCollider.center = originalColliderCenter;
    }

    // ---------- BOOST ----------
    public void ActivateSpeedBoost()
    {
        if (!isBoosted) StartCoroutine(SpeedBoostRoutine());
    }

    IEnumerator SpeedBoostRoutine()
    {
        isBoosted = true;
        float pre = playerSpeed;
        playerSpeed = pre * boostMultiplier;
        yield return new WaitForSeconds(boostDuration);
        playerSpeed = pre;
        isBoosted = false;
    }

    // ---------- HELPERS ----------
    float CurrentForwardSpeed()
    {
        float s = playerSpeed;
        if (isSliding) s *= slideSpeedMultiplier;
        return s;
    }

    void SyncAnimator(float moveSpeed)
    {
        if (animator == null) return;
        if (useAnimatorSpeedMultiplier)
        {
            float mult = Mathf.Max(0.01f, moveSpeed / Mathf.Max(0.01f, baseRunSpeed));
            animator.speed = mult;
        }
        else
        {
            if (HasParam(speedParam)) animator.SetFloat(speedParam, moveSpeed);
        }
    }

    bool TryGetGroundY(out float y)
    {
        Vector3 origin = transform.position + Vector3.up * rayStartOffset;

        if (useMobileGroundFix)
        {
            Vector3[] origins = {
                origin,
                origin + Vector3.forward * 0.2f,
                origin + Vector3.back * 0.2f
            };

            float bestY = float.MinValue;
            bool foundAny = false;

            foreach (Vector3 testOrigin in origins)
            {
                if (Physics.Raycast(testOrigin, Vector3.down, out RaycastHit hit, raycastDown, groundMask, QueryTriggerInteraction.Ignore))
                {
                    float candidateY = hit.point.y + footHeightOffset;
                    if (candidateY > bestY)
                    {
                        bestY = candidateY;
                        foundAny = true;
                    }
                }
            }

            if (foundAny)
            {
                y = bestY;
                return true;
            }
        }
        else
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDown, groundMask, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y + footHeightOffset;
                return true;
            }
        }

        y = transform.position.y;
        return false;
    }

    void EnterRun(float fade = 0.05f)
    {
        if (CrossFadeIfExists(runStateName, fade)) return;
        if (HasParam("RunTrigger")) animator.SetTrigger("RunTrigger");
    }

    bool CrossFadeIfExists(string stateNameOrPath, float fade)
    {
        if (animator == null || string.IsNullOrEmpty(stateNameOrPath)) return false;
        int id = Animator.StringToHash(stateNameOrPath);
        if (animator.HasState(0, id))
        {
            animator.CrossFade(stateNameOrPath, fade, 0, 0f);
            return true;
        }
        return false;
    }

    void SetTrigger(string name) { if (HasParam(name)) animator.SetTrigger(name); }
    void ResetTrigger(string name) { if (HasParam(name)) animator.ResetTrigger(name); }
    bool HasParam(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    // ---------- REVIVE ----------
    public void ReviveAt(Vector3 revivePosition)
    {
        StartCoroutine(ReviveCoroutine(revivePosition));
    }

    private IEnumerator ReviveCoroutine(Vector3 revivePosition)
    {
        isReviving = true;

        if (jumpRoutine != null) { StopCoroutine(jumpRoutine); jumpRoutine = null; }
        if (slideRoutine != null) { StopCoroutine(slideRoutine); slideRoutine = null; }

        isJumping = false;
        isSliding = false;
        slideRequestedOrActive = false;
        slideQueuedAfterJump = false;
        jumpCancelRequested = false;

        RestoreCollider();

        if (tiltRoutine != null)
        {
            StopCoroutine(tiltRoutine);
            tiltRoutine = null;
        }
        targetTilt = 0f;
        currentTilt = 0f;
        transform.rotation = Quaternion.identity;

        if (animator != null)
        {
            if (animator.speed == 0f) animator.speed = 1f;
            for (int i = 1; i < animator.layerCount; i++)
                animator.SetLayerWeight(i, 0f);

            if (HasParam("JumpTrigger")) ResetTrigger("JumpTrigger");
            if (HasParam("SlideTrigger")) ResetTrigger("SlideTrigger");
            if (HasParam("LandTrigger")) ResetTrigger("LandTrigger");

            animator.Update(0f);
        }

        float targetGroundY = revivePosition.y;
        lastGroundY = targetGroundY;
        confirmedGroundY = targetGroundY;
        hasValidGroundY = true;

        Vector3 finalPosition = new Vector3(
            revivePosition.x,
            revivePosition.y,
            revivePosition.z + 2f
        );

        transform.position = finalPosition;

        yield return new WaitForSeconds(revivalGroundCheckDelay);
        if (animator != null)
        {
            yield return new WaitForSeconds(animatorResetDelay);
            EnterRun(0.0f);
            animator.Update(0f);
        }

        isFrozen = false;
        isReviving = false;
    }
}
