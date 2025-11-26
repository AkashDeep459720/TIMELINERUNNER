using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RetryDialogManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject retryPanel;      // 660x700 panel
    [SerializeField] private GameObject gameOverPanel;   // Fullscreen takeover
    [SerializeField] private Image fadePanel;            // Fullscreen black overlay
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button tokenButton;
    [SerializeField] private Button adButton;
    [SerializeField] private TMP_Text adButtonText;
    [SerializeField] private Button noThanksButton;

    [Header("Game Over UI")]
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text runCoinsText;
    [SerializeField] private TMP_Text walletCoinsText;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private int maxAdsPerRun = 3;

    // Internal
    private PlayerMovement move;
    private Animator anim;
    private ObstacleTriggerHandler currentObstacle;
    private int retryCount = 0;
    private int adsUsedThisRun = 0;
    private bool runOver = false;
    private bool coinsAlreadyBanked = false; // Prevent double banking

    // 🔧 NEW: Store death position for consistent revival
    private Vector3 deathPosition;

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        move = Object.FindFirstObjectByType<PlayerMovement>();
#else
        move = Object.FindObjectOfType<PlayerMovement>();
#endif
        if (move) anim = move.GetComponentInChildren<Animator>();

        if (tokenButton) tokenButton.onClick.AddListener(OnUseToken);
        if (adButton) adButton.onClick.AddListener(OnWatchAd);
        if (noThanksButton) noThanksButton.onClick.AddListener(OnNoThanks);
        if (tryAgainButton) tryAgainButton.onClick.AddListener(OnTryAgain);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenu);

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (retryPanel) retryPanel.SetActive(false);

        if (fadePanel) fadePanel.color = new Color(0, 0, 0, 0);
    }

    private void Start()
    {
        // Reset counters at start of new run
        ResetRunCounters();
    }

    public void ResetRunCounters()
    {
        retryCount = 0;
        adsUsedThisRun = 0;
        runOver = false;
        coinsAlreadyBanked = false;

        // Reset MasterInfo run state
        MasterInfo.retryUsedThisRun = false;

        Debug.Log("🔄 Run counters reset");
    }

    public void ShowRetryDialog()
    {
        if (runOver) return;

        // 🔧 FIX: Store current position when death occurs for consistent revival
        if (move != null)
        {
            deathPosition = move.transform.position;
            Debug.Log($"💀 Death position stored: {deathPosition}");
        }

        Time.timeScale = 0f;
        retryPanel.SetActive(true);
        UpdateRetryUI();

        if (move) move.isFrozen = true;

        Debug.Log("📋 Retry dialog shown");
    }

    private void UpdateRetryUI()
    {
        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount)); // 1,2,4,8...
        if (costText) costText.text = "Cost: x" + cost;

        // Token button logic - disabled if insufficient tokens, but always visible
        if (tokenButton)
        {
            tokenButton.interactable = MasterInfo.retryTokens >= cost;
        }

        // Ad button logic - always visible, but disabled when limit reached
        if (adButton)
        {
            bool canUseAd = adsUsedThisRun < maxAdsPerRun;
            adButton.interactable = canUseAd;

            if (adButtonText)
            {
                if (canUseAd)
                    adButtonText.text = $"Watch Ad ({maxAdsPerRun - adsUsedThisRun} left)";
                else
                    adButtonText.text = "No Ads Left";
            }
        }
    }

    private void OnUseToken()
    {
        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount));

        // Double check tokens (should match button interactable state)
        if (MasterInfo.retryTokens >= cost)
        {
            MasterInfo.retryTokens -= cost;
            PlayerPrefs.SetInt("RetryTokens", MasterInfo.retryTokens);
            PlayerPrefs.Save();

            retryCount++; // Increment retry count for next death
            ResumeRun();

            Debug.Log($"💰 Used {cost} tokens. Remaining: {MasterInfo.retryTokens}");
        }
        else
        {
            Debug.Log("❌ Not enough tokens.");
            // Refresh UI in case of sync issues
            UpdateRetryUI();
        }
    }

    private void OnWatchAd()
    {
        if (adsUsedThisRun >= maxAdsPerRun)
        {
            Debug.Log("❌ No ad retries left");
            return;
        }

        Debug.Log($"📺 Simulating rewarded ad... ({adsUsedThisRun + 1}/{maxAdsPerRun})");

        adsUsedThisRun++;
        retryCount++; // Increment retry count for next death
        ResumeRun();
    }

    private void OnNoThanks()
    {
        Debug.Log("🚪 No thanks → transitioning to Game Over");
        retryPanel.SetActive(false);
        StartCoroutine(FadeToGameOver());
    }

    private void OnTryAgain()
    {
        Debug.Log("🔄 Try Again pressed");

        // Reset for new run
        ResetRunCounters();

        // Hide game over panel
        gameOverPanel.SetActive(false);

        // Reset fade panel
        if (fadePanel) fadePanel.color = new Color(0, 0, 0, 0);

        Time.timeScale = 1f;

        // Restart the game/level
        // This would typically reload the scene or reset game state
        // You might want to call your game manager's restart method here
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        Debug.Log("🏠 Returning to Main Menu");

        Time.timeScale = 1f;

        // Load main menu scene
        // Replace "MainMenu" with your actual main menu scene name
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator FadeToGameOver()
    {
        runOver = true;

        // Fade to black
        if (fadePanel)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);
                fadePanel.color = new Color(0, 0, 0, a);
                yield return null;
            }
        }

        // ✅ Bank coins ONLY ONCE at final game over
        if (!coinsAlreadyBanked)
        {
            MasterInfo.BankRunCoins();
            coinsAlreadyBanked = true;
            Debug.Log("💰 Coins banked at Game Over");
        }

        // Update Game Over UI
        UpdateGameOverUI();

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void UpdateGameOverUI()
    {
        // Distance display using DistanceTracker
        if (distanceText && DistanceTracker.Instance)
            distanceText.text = $"Distance: {FormatNumber(DistanceTracker.Instance.currentDistance)}m";

        // Run coins collected using MasterInfo
        if (runCoinsText)
            runCoinsText.text = $"Coins Collected: {FormatNumber(MasterInfo.coinCount)}";

        // Animated wallet total
        if (walletCoinsText)
            StartCoroutine(AnimateWallet(MasterInfo.totalCoins - MasterInfo.coinCount, MasterInfo.totalCoins));
    }

    private IEnumerator AnimateWallet(int from, int to)
    {
        float elapsed = 0f;
        float animationDuration = 1f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / animationDuration;
            int val = Mathf.RoundToInt(Mathf.Lerp(from, to, progress));
            walletCoinsText.text = $"Wallet: {FormatNumber(val)}";
            yield return null;
        }

        // Ensure final value is exact
        walletCoinsText.text = $"Wallet: {FormatNumber(to)}";
    }

    private void ResumeRun()
    {
        retryPanel.SetActive(false);
        Time.timeScale = 1f;

        // 🔧 FIX: Smooth animator restoration
        StartCoroutine(SmoothResumeSequence());

        Debug.Log("▶️ Resuming run...");
    }

    // 🔧 NEW: Smooth resume sequence to prevent animation glitches
    private IEnumerator SmoothResumeSequence()
    {
        // First restore obstacle animator if needed
        if (currentObstacle != null)
        {
            currentObstacle.RestoreAnimatorState();
        }
        else if (anim && anim.speed == 0f)
        {
            anim.speed = 1f;
        }

        // Wait one frame for animator to update
        yield return null;

        // 🔧 FIX: Use stored death position for consistent revival location
        Vector3 revivalPosition = deathPosition;

        // Ensure we have a valid revival position
        if (move != null)
        {
            if (revivalPosition == Vector3.zero)
            {
                revivalPosition = move.transform.position;
                Debug.LogWarning("⚠️ Using current position as death position was not stored");
            }

            // Revive player at the exact death location (no extra offsets)
            move.ReviveAt(revivalPosition);
        }

        currentObstacle = null;
    }

    public void SetCurrentObstacle(ObstacleTriggerHandler obstacle)
    {
        currentObstacle = obstacle;
    }

    // Called when player quits/pauses mid-run
    public void OnRunAbandoned()
    {
        Debug.Log("🚪 Run abandoned - coins NOT banked");
        // Don't bank coins when quitting mid-run
        // Reset MasterInfo.coinCount to 0 to discard collected coins
        MasterInfo.coinCount = 0;
        ResetRunCounters();
    }

    // Utility method to check if player can retry
    public bool CanRetry()
    {
        if (runOver) return false;

        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount));
        bool hasTokens = MasterInfo.retryTokens >= cost;
        bool hasAds = adsUsedThisRun < maxAdsPerRun;

        return hasTokens || hasAds;
    }

    // Get current retry cost for UI display elsewhere
    public int GetCurrentRetryCost()
    {
        return Mathf.RoundToInt(Mathf.Pow(2, retryCount));
    }

    private string FormatNumber(int num)
    {
        return string.Format("{0:n0}", num); // adds commas (e.g., 1,250)
    }

    // Optional: Force show game over (for testing or special cases)
    public void ForceGameOver()
    {
        if (!runOver)
        {
            OnNoThanks();
        }
    }
}