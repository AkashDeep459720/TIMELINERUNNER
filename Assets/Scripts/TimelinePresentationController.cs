using UnityEngine;
using TMPro;

/// <summary>
/// Swaps skybox, music, and optional HUD when entering the Egypt timeline.
/// </summary>
public class TimelinePresentationController : MonoBehaviour
{
    public static TimelinePresentationController Instance { get; private set; }

    [Header("Egypt Presentation")]
    [SerializeField] private Material egyptSkybox;
    [SerializeField] private AudioClip egyptMusicClip;
    [SerializeField] private TMP_Text timelineLabelText;
    [SerializeField] private string egyptLabel = "Ancient Egypt";

    [Header("Robot Presentation (optional restore)")]
    [SerializeField] private Material robotSkybox;

    private bool inEgyptPresentation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OnEnterEgypt()
    {
        if (inEgyptPresentation) return;
        inEgyptPresentation = true;

        if (egyptSkybox != null)
        {
            RenderSettings.skybox = egyptSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (egyptMusicClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusicClip(egyptMusicClip);

        if (timelineLabelText != null)
            timelineLabelText.text = egyptLabel;
    }

    public void OnEnterRobot()
    {
        inEgyptPresentation = false;

        if (robotSkybox != null)
        {
            RenderSettings.skybox = robotSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (timelineLabelText != null)
            timelineLabelText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
