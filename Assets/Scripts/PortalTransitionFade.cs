using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen fade for portal timeline transitions (uses unscaled time).
/// </summary>
public class PortalTransitionFade : MonoBehaviour
{
    public static PortalTransitionFade Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.35f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureFadeImage();
        SetAlpha(0f);
    }

    public static PortalTransitionFade GetOrCreate()
    {
        if (Instance != null) return Instance;

        PortalTransitionFade existing = FindFirstObjectByType<PortalTransitionFade>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        var tagged = GameObject.FindWithTag("FadeUI");
        if (tagged != null)
        {
            Instance = tagged.GetComponent<PortalTransitionFade>();
            if (Instance == null)
                Instance = tagged.AddComponent<PortalTransitionFade>();
            return Instance;
        }

        GameObject go = new GameObject("PortalTransitionFade");
        ApplyRuntimeHideFlags(go);
        Instance = go.AddComponent<PortalTransitionFade>();
        return Instance;
    }

    public IEnumerator FadeToBlack(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        EnsureFadeImage();
        yield return FadeRoutine(0f, 1f, duration);
    }

    public IEnumerator FadeFromBlack(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        EnsureFadeImage();
        yield return FadeRoutine(1f, 0f, duration);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
        fadeImage.raycastTarget = a > 0.01f;
    }

    private void EnsureFadeImage()
    {
        if (fadeImage != null) return;

        fadeImage = GetComponent<Image>();
        if (fadeImage == null)
            fadeImage = gameObject.AddComponent<Image>();

        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        if (GetComponent<Canvas>() == null)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void ApplyRuntimeHideFlags(GameObject go)
    {
#if UNITY_EDITOR
        go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
#else
        go.hideFlags = HideFlags.DontSave;
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
