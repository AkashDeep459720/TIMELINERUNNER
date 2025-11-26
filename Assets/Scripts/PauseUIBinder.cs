using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseUIBinder : MonoBehaviour
{
    [Header("Optional explicit refs (auto-found if empty)")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    void Awake()
    {
        EnsureEventSystem();
        Bind();
    }

    void OnEnable()
    {
        // Re-bind in case panel was disabled/enabled or scene just loaded
        Bind();
    }

    public void Bind()
    {
        // Auto-find if not assigned
        if (resumeButton == null) resumeButton = FindButton("ResumeButton");
        if (restartButton == null) restartButton = FindButton("RestartButton");
        if (mainMenuButton == null) mainMenuButton = FindButton("MainMenuButton");

        var gp = GamePause.Instance;
        if (gp == null) return;

        // Remove old listeners to avoid duplicates
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(gp.UI_Resume);
        }
        if (restartButton)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(gp.UI_Restart);
        }
        if (mainMenuButton)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(gp.UI_MainMenu);
        }
    }

    private Button FindButton(string name)
    {
        var t = transform.Find(name) ?? DeepFind(transform, name);
        return t ? t.GetComponent<Button>() : null;
    }

    private Transform DeepFind(Transform parent, string target)
    {
        foreach (Transform child in parent)
        {
            if (child.name == target) return child;
            var found = DeepFind(child, target);
            if (found) return found;
        }
        return null;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go); // keep it alive across scenes
        }
    }
}
