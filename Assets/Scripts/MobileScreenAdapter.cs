using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MobileScreenAdapter : MonoBehaviour
{
    [Header("Camera Fit")]
    [SerializeField] private float referenceAspect = 16f / 9f;
    [SerializeField] private float referenceVerticalFov = 62f;
    [SerializeField] private float minVerticalFov = 50f;
    [SerializeField] private float maxVerticalFov = 78f;

    private static MobileScreenAdapter instance;
    private Rect lastSafeArea;
    private int lastWidth;
    private int lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAdapter()
    {
        if (instance != null) return;

        GameObject go = new GameObject("MobileScreenAdapter");
        instance = go.AddComponent<MobileScreenAdapter>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyAll();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea)
            ApplyAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAll();
    }

    private void ApplyAll()
    {
        lastSafeArea = Screen.safeArea;
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        ApplyCameraFit();
        ApplyCanvasSafeArea();
    }

    private void ApplyCameraFit()
    {
        Camera cam = Camera.main;
        if (cam == null || cam.orthographic) return;

        float targetHFov = Camera.VerticalToHorizontalFieldOfView(referenceVerticalFov, referenceAspect);
        float adjustedVertical = Camera.HorizontalToVerticalFieldOfView(targetHFov, cam.aspect);
        cam.fieldOfView = Mathf.Clamp(adjustedVertical, minVerticalFov, maxVerticalFov);
    }

    private void ApplyCanvasSafeArea()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        Vector2 minAnchor = safeArea.position;
        Vector2 maxAnchor = safeArea.position + safeArea.size;
        minAnchor.x /= screenSize.x;
        minAnchor.y /= screenSize.y;
        maxAnchor.x /= screenSize.x;
        maxAnchor.y /= screenSize.y;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;
            ApplySafeAreaToCanvas(canvas, minAnchor, maxAnchor);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.matchWidthOrHeight = Screen.width >= Screen.height ? 0.5f : 1f;
            }
        }
    }

    private void ApplySafeAreaToCanvas(Canvas canvas, Vector2 minAnchor, Vector2 maxAnchor)
    {
        RectTransform safeTarget = null;

        Transform named = canvas.transform.Find("SafeArea");
        if (named != null)
            safeTarget = named as RectTransform;

        if (safeTarget == null && canvas.transform.childCount == 1)
            safeTarget = canvas.transform.GetChild(0) as RectTransform;

        if (safeTarget != null)
        {
            ApplySafeAreaAnchors(safeTarget, minAnchor, maxAnchor);
            return;
        }

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            RectTransform child = canvas.transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            bool isFullStretch =
                Approximately(child.anchorMin, Vector2.zero) &&
                Approximately(child.anchorMax, Vector2.one);

            if (isFullStretch)
                ApplySafeAreaAnchors(child, minAnchor, maxAnchor);
        }
    }

    private static void ApplySafeAreaAnchors(RectTransform target, Vector2 minAnchor, Vector2 maxAnchor)
    {
        target.anchorMin = minAnchor;
        target.anchorMax = maxAnchor;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.001f && Mathf.Abs(a.y - b.y) < 0.001f;
    }
}
