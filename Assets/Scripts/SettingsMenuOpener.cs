using UnityEngine;

public class SettingsMenuOpener : MonoBehaviour
{
    public GameObject settingsPanel;

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
}
