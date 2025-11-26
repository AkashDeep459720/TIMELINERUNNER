using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text coinSummaryText;

    public void TriggerGameOver()
    {
        Debug.Log("TriggerGameOver called ✅");

        // EXACTLY mirror the Retry 'No Thanks' display style: just the number.
        int runCoins = MasterInfo.coinCount;
        if (coinSummaryText != null)
            coinSummaryText.text = "" + runCoins + "";

        // Bank and freeze
        MasterInfo.BankRunCoins();
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Desertrun");
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
