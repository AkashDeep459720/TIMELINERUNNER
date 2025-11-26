using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a short popup ("ORBS: x/y") every time OrbManager reports a change,
/// then fades out. Uses unscaled time and never disables the GameObject,
/// so subscriptions remain intact.
/// </summary>
[DisallowMultipleComponent]
public class OrbCounterUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;            // assign your TMP text
    [SerializeField] private CanvasGroup canvasGroup;  // add CanvasGroup on SAME object

    [Header("Text")]
    [SerializeField] private string format = "ORBS: {0}/{1}";

    [Header("Timing (unscaled time)")]
    [SerializeField] private float holdSeconds = 1.0f;
    [SerializeField] private float fadeIn = 0.12f;
    [SerializeField] private float fadeOut = 0.25f;

    [Header("Pop")]
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popReturnSpeed = 10f;

    private Coroutine routine;
    private Vector3 baseScale;
    private bool subscribed;

    private void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = transform.localScale;

        // Start hidden; keep object active so OnEnable/OnDisable manage subscription cleanly
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        StartCoroutine(EnsureSubscribe());
    }

    private void OnDisable()
    {
        if (subscribed && OrbManager.Instance != null)
        {
            OrbManager.Instance.OnOrbChanged -= HandleOrbChanged;
            subscribed = false;
        }
    }

    private IEnumerator EnsureSubscribe()
    {
        // handle load-order (OrbManager may spawn a few frames later)
        int safety = 0;
        while (OrbManager.Instance == null && safety < 120) { safety++; yield return null; }

        if (OrbManager.Instance != null)
        {
            OrbManager.Instance.OnOrbChanged += HandleOrbChanged;
            subscribed = true;

            // Initialize text (stay hidden until first pickup)
            if (text) text.text = string.Format(format, OrbManager.Instance.CurrentOrbs, OrbManager.Instance.RequiredOrbs);
            canvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogWarning("[OrbCounterUI] OrbManager not found in scene.");
        }
    }

    private void HandleOrbChanged(int current, int required)
    {
        if (text) text.text = string.Format(format, current, required);

        // Force-show immediately and restart the timer each pickup
        canvasGroup.alpha = 1f;
        transform.localScale = baseScale * popScale;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowThenHideRealtime());
    }

    private IEnumerator ShowThenHideRealtime()
    {
        // Fade-in settle is already forced; just pop back to base scale smoothly
        float tIn = 0f;
        while (tIn < fadeIn)
        {
            tIn += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, popReturnSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        transform.localScale = baseScale;

        // Hold (unscaled)
        yield return new WaitForSecondsRealtime(holdSeconds);

        // Fade out (unscaled)
        float tOut = 0f;
        while (tOut < fadeOut)
        {
            tOut += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(tOut / fadeOut);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // IMPORTANT: do NOT SetActive(false); keep object active so it stays subscribed
    }
}
