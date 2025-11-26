using UnityEngine;

public class ShopTabs : MonoBehaviour
{
    [Header("Tab Panels")]
    public GameObject currencyPanel;
    public GameObject skinsPanel;
    public GameObject powerupsPanel;
    public GameObject specialsPanel;

    private void OnEnable()
    {
        // Always show Currency tab first when ShopPanel opens
        ShowCurrency();
    }

    public void ShowCurrency() { SetActivePanel(currencyPanel); }
    public void ShowSkins() { SetActivePanel(skinsPanel); }
    public void ShowPowerups() { SetActivePanel(powerupsPanel); }
    public void ShowSpecials() { SetActivePanel(specialsPanel); }

    private void SetActivePanel(GameObject panel)
    {
        // Disable all panels first
        if (currencyPanel) currencyPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (powerupsPanel) powerupsPanel.SetActive(false);
        if (specialsPanel) specialsPanel.SetActive(false);

        // Enable the selected one
        if (panel) panel.SetActive(true);
    }
}
