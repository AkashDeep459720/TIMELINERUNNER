using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RetryDialogManager : MonoBehaviour
{
    public struct RunResultsSnapshot
    {
        public int finalDistanceMeters;
        public int runCoinsCollected;
        public int walletBeforeBank;
    }

    [Header("Refs")]
    [SerializeField] private GameObject retryPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image fadePanel;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button tokenButton;
    [SerializeField] private Button adButton;
    [SerializeField] private TMP_Text adButtonText;
    [SerializeField] private Button noThanksButton;

    [Header("Game Over UI")]
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text runCoinsText;
    [SerializeField] private TMP_Text walletCoinsText;
    [SerializeField] private TMP_Text tapHintText;
    [SerializeField] private Button tapToBankButton;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private int maxAdsPerRun = 3;
    [SerializeField] private float coinTransferDuration = 1f;

    private PlayerMovement move;
    private Animator anim;
    private ObstacleTriggerHandler currentObstacle;
    private int retryCount = 0;
    private int adsUsedThisRun = 0;
    private bool runOver = false;
    private bool coinsAlreadyBanked = false;
    private bool coinTransferInProgress = false;
    private RunResultsSnapshot runSnapshot;
    private Vector3 deathPosition;

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        move = Object.FindFirstObjectByType<PlayerMovement>();
#else
        move = Object.FindObjectOfType<PlayerMovement>();
#endif
        if (move) anim = move.GetComponentInChildren<Animator>();

        ResolveGameOverRefs();

        if (tokenButton) tokenButton.onClick.AddListener(OnUseToken);
        if (adButton) adButton.onClick.AddListener(OnWatchAd);
        if (noThanksButton) noThanksButton.onClick.AddListener(OnNoThanks);
        if (tryAgainButton) tryAgainButton.onClick.AddListener(OnTryAgain);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenu);
        if (tapToBankButton) tapToBankButton.onClick.AddListener(OnTapToBankCoins);

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (retryPanel) retryPanel.SetActive(false);

        if (fadePanel) fadePanel.color = new Color(0, 0, 0, 0);
    }

    private void ResolveGameOverRefs()
    {
        if (gameOverPanel == null) return;

        TMP_Text[] texts = gameOverPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in texts)
        {
            string n = t.gameObject.name;
            if (n.Contains("Distance")) distanceText = t;
            else if (n.Contains("RunCoins")) runCoinsText = t;
            else if (n.Contains("Wallet")) walletCoinsText = t;
            else if (n.Contains("TapHint")) tapHintText = t;
        }

        if (tapToBankButton == null)
        {
            foreach (Button b in gameOverPanel.GetComponentsInChildren<Button>(true))
            {
                if (b.gameObject.name.Contains("TapToBank"))
                {
                    tapToBankButton = b;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        ResetRunCounters();
    }

    public void ResetRunCounters()
    {
        retryCount = 0;
        adsUsedThisRun = 0;
        runOver = false;
        coinsAlreadyBanked = false;
        coinTransferInProgress = false;
        MasterInfo.retryUsedThisRun = false;
        SetGameOverHudPaused(false);

        Debug.Log("🔄 Run counters reset");
    }

    public void ShowRetryDialog()
    {
        if (runOver) return;

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
        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount));
        if (costText) costText.text = "Cost: x" + cost;

        if (tokenButton)
            tokenButton.interactable = MasterInfo.retryTokens >= cost;

        if (adButton)
        {
            bool canUseAd = adsUsedThisRun < maxAdsPerRun;
            adButton.interactable = canUseAd;

            if (adButtonText)
            {
                adButtonText.text = canUseAd
                    ? $"Watch Ad ({maxAdsPerRun - adsUsedThisRun} left)"
                    : "No Ads Left";
            }
        }
    }

    private void OnUseToken()
    {
        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount));

        if (MasterInfo.retryTokens >= cost)
        {
            MasterInfo.retryTokens -= cost;
            PlayerPrefs.SetInt("RetryTokens", MasterInfo.retryTokens);
            PlayerPrefs.Save();

            retryCount++;
            ResumeRun();

            Debug.Log($"💰 Used {cost} tokens. Remaining: {MasterInfo.retryTokens}");
        }
        else
        {
            Debug.Log("❌ Not enough tokens.");
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
        retryCount++;
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

        if (!coinsAlreadyBanked && runSnapshot.runCoinsCollected > 0)
            MasterInfo.BankRunCoins(runSnapshot.runCoinsCollected);

        ResetRunCounters();
        gameOverPanel.SetActive(false);

        if (fadePanel) fadePanel.color = new Color(0, 0, 0, 0);

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        Debug.Log("🏠 Returning to Main Menu");

        if (!coinsAlreadyBanked && runSnapshot.runCoinsCollected > 0)
            MasterInfo.BankRunCoins(runSnapshot.runCoinsCollected);

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnTapToBankCoins()
    {
        if (coinsAlreadyBanked || coinTransferInProgress || runSnapshot.runCoinsCollected <= 0)
            return;

        StartCoroutine(TransferCoinsAnimation());
    }

    private void CaptureRunSnapshot()
    {
        runSnapshot = new RunResultsSnapshot
        {
            finalDistanceMeters = DistanceTracker.GetDistanceMeters(),
            runCoinsCollected = MasterInfo.coinCount,
            walletBeforeBank = MasterInfo.totalCoins
        };
    }

    private IEnumerator FadeToGameOver()
    {
        runOver = true;
        CaptureRunSnapshot();

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

        SetGameOverHudPaused(true);
        UpdateGameOverUI();
        ConfigureTapToBankState();

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void SetGameOverHudPaused(bool paused)
    {
        MasterInfo.pauseHudUpdates = paused;
        DistanceTracker.pauseHudUpdates = paused;
    }

    private void UpdateGameOverUI()
    {
        if (distanceText)
            distanceText.text = $"Distance: {FormatNumber(runSnapshot.finalDistanceMeters)}m";

        if (runCoinsText)
            runCoinsText.text = $"Coins Collected: {FormatNumber(runSnapshot.runCoinsCollected)}";

        if (walletCoinsText)
            walletCoinsText.text = $"Wallet: {FormatNumber(runSnapshot.walletBeforeBank)}";
    }

    private void ConfigureTapToBankState()
    {
        bool hasCoins = runSnapshot.runCoinsCollected > 0 && !coinsAlreadyBanked;

        if (tapToBankButton)
            tapToBankButton.interactable = hasCoins;

        if (tapHintText)
        {
            tapHintText.text = hasCoins
                ? "Tap to add coins to wallet"
                : (coinsAlreadyBanked ? "Tap Try Again to restart" : "");
        }

        if (tryAgainButton)
            tryAgainButton.interactable = !hasCoins || coinsAlreadyBanked;
    }

    private IEnumerator TransferCoinsAnimation()
    {
        coinTransferInProgress = true;

        if (tapToBankButton) tapToBankButton.interactable = false;

        int fromRun = runSnapshot.runCoinsCollected;
        int fromWallet = runSnapshot.walletBeforeBank;
        int toWallet = fromWallet + fromRun;
        float elapsed = 0f;

        while (elapsed < coinTransferDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / coinTransferDuration);

            int displayRun = Mathf.RoundToInt(Mathf.Lerp(fromRun, 0, t));
            int displayWallet = Mathf.RoundToInt(Mathf.Lerp(fromWallet, toWallet, t));

            if (runCoinsText)
            {
                runCoinsText.text = $"Coins Collected: {FormatNumber(displayRun)}";
                runCoinsText.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI));
            }

            if (walletCoinsText)
                walletCoinsText.text = $"Wallet: {FormatNumber(displayWallet)}";

            yield return null;
        }

        if (runCoinsText)
        {
            runCoinsText.text = "Coins Collected: 0";
            runCoinsText.transform.localScale = Vector3.one;
        }

        if (walletCoinsText)
            walletCoinsText.text = $"Wallet: {FormatNumber(toWallet)}";

        MasterInfo.BankRunCoins(fromRun);
        coinsAlreadyBanked = true;
        coinTransferInProgress = false;

        if (tapHintText)
            tapHintText.text = "Tap Try Again to restart";

        if (tryAgainButton)
            tryAgainButton.interactable = true;

        Debug.Log("💰 Coins transferred to wallet on tap");
    }

    private void ResumeRun()
    {
        retryPanel.SetActive(false);
        Time.timeScale = 1f;
        StartCoroutine(SmoothResumeSequence());
        Debug.Log("▶️ Resuming run...");
    }

    private IEnumerator SmoothResumeSequence()
    {
        if (currentObstacle != null)
            currentObstacle.RestoreAnimatorState();
        else if (anim && anim.speed == 0f)
            anim.speed = 1f;

        yield return null;

        Vector3 revivalPosition = deathPosition;

        if (move != null)
        {
            if (revivalPosition == Vector3.zero)
            {
                revivalPosition = move.transform.position;
                Debug.LogWarning("⚠️ Using current position as death position was not stored");
            }

            move.ReviveAt(revivalPosition);
        }

        currentObstacle = null;
    }

    public void SetCurrentObstacle(ObstacleTriggerHandler obstacle)
    {
        currentObstacle = obstacle;
    }

    public void OnRunAbandoned()
    {
        Debug.Log("🚪 Run abandoned - coins NOT banked");
        MasterInfo.coinCount = 0;
        ResetRunCounters();
    }

    public bool CanRetry()
    {
        if (runOver) return false;

        int cost = Mathf.RoundToInt(Mathf.Pow(2, retryCount));
        bool hasTokens = MasterInfo.retryTokens >= cost;
        bool hasAds = adsUsedThisRun < maxAdsPerRun;

        return hasTokens || hasAds;
    }

    public int GetCurrentRetryCost()
    {
        return Mathf.RoundToInt(Mathf.Pow(2, retryCount));
    }

    private string FormatNumber(int num)
    {
        return string.Format("{0:n0}", num);
    }

    public void ForceGameOver()
    {
        if (!runOver)
            OnNoThanks();
    }
}
