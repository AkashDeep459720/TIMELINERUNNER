using UnityEngine;

public class DailyLoginOpener : MonoBehaviour
{
    [SerializeField] private GameObject dailyLoginPanel; // drag your DailyLoginPanel here

    public void OpenDailyLogin()
    {
        if (dailyLoginPanel != null)
            dailyLoginPanel.SetActive(true);
    }

    public void CloseDailyLogin()
    {
        if (dailyLoginPanel != null)
            dailyLoginPanel.SetActive(false);
    }
}
