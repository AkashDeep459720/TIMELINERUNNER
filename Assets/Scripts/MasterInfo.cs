using UnityEngine;
using TMPro;

public class MasterInfo : MonoBehaviour
{
    public static int coinCount = 0;      // Coins collected during run
    public static int totalCoins = 0;     // Persistent wallet
    public static int retryTokens = 0;    // Retry tokens available

    // Track if a retry has already been used in this run
    public static bool retryUsedThisRun = false;

    [SerializeField] private TMP_Text coinDisplay;
    private int previousCount = -1;

    void Start()
    {
        // Load persistent coins
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        // Load or initialize retry tokens
        if (PlayerPrefs.HasKey("RetryTokens"))
        {
            retryTokens = PlayerPrefs.GetInt("RetryTokens");
        }
        else
        {
            retryTokens = 3; // Starting value
            PlayerPrefs.SetInt("RetryTokens", retryTokens);
            PlayerPrefs.Save();
        }

        // Reset run state
        coinCount = 0;
        previousCount = -1;

        // Ensure per-run flag is cleared when a new run starts
        retryUsedThisRun = false;
    }

    void Update()
    {
        if (coinDisplay != null && coinCount != previousCount)
        {
            coinDisplay.text = coinCount.ToString();
            previousCount = coinCount;
        }
    }

    public static void BankRunCoins()
    {
        totalCoins += coinCount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        coinCount = 0;
        Debug.Log("💰 Coins banked! New wallet total: " + totalCoins);
    }

    public static bool UseRetryToken()
    {
        if (retryTokens > 0)
        {
            retryTokens--;
            PlayerPrefs.SetInt("RetryTokens", retryTokens);
            PlayerPrefs.Save();
            Debug.Log("🪙 Retry token used. Remaining: " + retryTokens);
            return true;
        }

        Debug.LogWarning("❌ No retry tokens left.");
        return false;
    }
}
