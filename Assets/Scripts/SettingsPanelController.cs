using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    public GameObject settingsPanel;

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}
