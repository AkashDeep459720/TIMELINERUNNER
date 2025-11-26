using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text coinWalletText;

    void Start()
    {
        // Load saved totalCoins if not already loaded
        MasterInfo.totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        // Update the UI
        if (coinWalletText != null)
        {
            coinWalletText.text = " " + MasterInfo.totalCoins;
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1); // loads the scene at index 1
    }

    public void OpenSettings()
    {
        Debug.Log("Settings menu not implemented yet.");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
