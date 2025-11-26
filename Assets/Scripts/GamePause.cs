using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GamePause : MonoBehaviour
{
    public static GamePause Instance { get; private set; }

    [Header("Auto-find (by name, anywhere in scene, even if inactive)")]
    [SerializeField] private string pausePanelName = "PausePanel";
    [SerializeField] private string hudButtonName = "PauseButton";

    private GameObject pausePanel;
    private GameObject hudPauseButton;

    // Optional: custom hints to find buttons by name (case-insensitive "contains")
    [Header("Button name hints (change if your names differ)")]
    [SerializeField] private string resumeHint = "resume";
    [SerializeField] private string restartHint = "restart";
    [SerializeField] private string mainMenuHint = "main";

    private Button btnResume;
    private Button btnRestart;
    private Button btnMainMenu;

    private bool isPaused = false;
    private float cachedFixedDelta;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cachedFixedDelta = Time.fixedDeltaTime;
        EnsureEventSystem();
        ForceUnpause(); // safety
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindUIAndButtons(); // find panel + buttons even if panel is inactive
        ForceUnpause();       // ensure timescale/audio sane on new scene
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        // Make sure we have fresh refs (covers late scene changes)
        RebindUIAndButtons();

        if (pausePanel) pausePanel.SetActive(true);
        if (hudPauseButton) hudPauseButton.SetActive(false);

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        AudioListener.pause = true;
        GameState.IsPaused = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        if (pausePanel) pausePanel.SetActive(false);
        if (hudPauseButton) hudPauseButton.SetActive(true);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = cachedFixedDelta;
        AudioListener.pause = false;
        GameState.IsPaused = false;
    }

    public void ForceUnpause()
    {
        isPaused = false;

        if (pausePanel) pausePanel.SetActive(false);
        if (hudPauseButton) hudPauseButton.SetActive(true);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = cachedFixedDelta;
        AudioListener.pause = false;
        GameState.IsPaused = false;
    }

    // -------- UI Hooks (called by wired buttons) --------
    public void UI_Resume() => Resume();
    public void UI_Restart()
    {
        ForceUnpause();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void UI_MainMenu()
    {
        ForceUnpause();
        SceneManager.LoadScene("MainMenu"); // ensure it’s in Build Settings
    }

    // -------- Rebinding & Utilities --------
    private void RebindUIAndButtons()
    {
        // Find objects by name anywhere in the active scene, INCLUDING inactive
        pausePanel = FindInActiveSceneByName(pausePanelName);
        hudPauseButton = FindInActiveSceneByName(hudButtonName);

        // Make sure Canvas can receive clicks
        EnsureGraphicRaycaster(pausePanel);

        // Find buttons under the pausePanel (inactive included)
        btnResume = FindButtonByHint(pausePanel, resumeHint);
        btnRestart = FindButtonByHint(pausePanel, restartHint);
        btnMainMenu = FindButtonByHint(pausePanel, mainMenuHint);

        // Wire listeners (clear to avoid duplicates after reloads)
        if (btnResume)
        {
            btnResume.onClick.RemoveAllListeners();
            btnResume.onClick.AddListener(UI_Resume);
            btnResume.interactable = true;
        }
        if (btnRestart)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(UI_Restart);
            btnRestart.interactable = true;
        }
        if (btnMainMenu)
        {
            btnMainMenu.onClick.RemoveAllListeners();
            btnMainMenu.onClick.AddListener(UI_MainMenu);
            btnMainMenu.interactable = true;
        }
    }

    private GameObject FindInActiveSceneByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects(); // includes inactive roots
        foreach (var root in roots)
        {
            var t = DeepFind(root.transform, targetName);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private Transform DeepFind(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var result = DeepFind(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    private Button FindButtonByHint(GameObject root, string hint)
    {
        if (!root || string.IsNullOrEmpty(hint)) return null;
        var buttons = root.GetComponentsInChildren<Button>(true); // include inactive
        var lower = hint.ToLower();
        foreach (var b in buttons)
        {
            if (b.name.ToLower().Contains(lower)) return b;
        }
        return null;
    }

    private void EnsureGraphicRaycaster(GameObject panel)
    {
        if (!panel) return;
        var canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var go = new GameObject("EventSystem");
        DontDestroyOnLoad(go);

#if ENABLE_INPUT_SYSTEM
        go.AddComponent<EventSystem>();
        var type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (type != null) go.AddComponent(type);
        else go.AddComponent<StandaloneInputModule>();
#else
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}

public static class GameState { public static bool IsPaused = false; }
