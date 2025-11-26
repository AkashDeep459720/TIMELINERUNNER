using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle toggleMusic;
    [SerializeField] private Toggle toggleSFX;

    void Start()
    {
        // Set toggle states based on saved preferences
        toggleMusic.isOn = AudioManager.Instance.musicOn;
        toggleSFX.isOn = AudioManager.Instance.sfxOn;

        // Hook up toggle listeners
        toggleMusic.onValueChanged.AddListener(AudioManager.Instance.ToggleMusic);
        toggleSFX.onValueChanged.AddListener(AudioManager.Instance.ToggleSFX);
    }
}
