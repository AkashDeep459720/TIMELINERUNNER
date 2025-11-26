using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DailyLoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RewardSlotUI[] rewardSlots;
    [SerializeField] private Button claimButton;

    [Header("Reward Icons")]
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite magnetIcon;
    [SerializeField] private Sprite speedIcon;
    [SerializeField] private Sprite jackpotIcon;

    [Header("Cheat Menu")]
    [SerializeField] private GameObject cheatMenuPanel; // drag your CheatMenuPanel here

    private int currentDayIndex;
    private bool claimedToday;

    private const string LastLoginKey = "LastLoginDate";
    private const string StreakDayKey = "DailyRewardDay";
    private const string ClaimedTodayKey = "ClaimedToday";

    private void Start()
    {
        if (claimButton) claimButton.onClick.AddListener(ClaimReward);

        CheckLoginState();
        UpdateUI();

        // Auto-open panel if new reward is available
        if (!claimedToday)
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);

        if (cheatMenuPanel) cheatMenuPanel.SetActive(false); // hidden by default
    }

    private void CheckLoginState()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        string lastLogin = PlayerPrefs.GetString(LastLoginKey, "");

        currentDayIndex = PlayerPrefs.GetInt(StreakDayKey, 0);
        claimedToday = PlayerPrefs.GetInt(ClaimedTodayKey, 0) == 1;

        if (lastLogin != today)
        {
            claimedToday = false;
            currentDayIndex = (currentDayIndex + 1) % 7;

            PlayerPrefs.SetString(LastLoginKey, today);
            PlayerPrefs.SetInt(StreakDayKey, currentDayIndex);
            PlayerPrefs.SetInt(ClaimedTodayKey, 0);
            PlayerPrefs.Save();
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            Sprite icon = GetRewardIcon(i);
            rewardSlots[i].Setup(i + 1, icon);

            if (i < currentDayIndex) rewardSlots[i].SetState(RewardSlotState.Past);
            else if (i == currentDayIndex) rewardSlots[i].SetState(RewardSlotState.Today);
            else rewardSlots[i].SetState(RewardSlotState.Future);
        }

        if (claimButton)
        {
            claimButton.interactable = !claimedToday;
            claimButton.GetComponentInChildren<TMP_Text>().text = claimedToday ? "Claimed" : "Claim";
        }
    }

    public void ClaimReward()
    {
        if (claimedToday) return;

        GrantReward(currentDayIndex);

        claimedToday = true;
        PlayerPrefs.SetInt(ClaimedTodayKey, 1);
        PlayerPrefs.Save();

        UpdateUI();

        // Optional: auto-close after claiming
        gameObject.SetActive(false);
    }

    private Sprite GetRewardIcon(int dayIndex)
    {
        switch (dayIndex)
        {
            case 0: return coinIcon;
            case 1: return shieldIcon;
            case 2: return coinIcon;
            case 3: return magnetIcon;
            case 4: return coinIcon;
            case 5: return speedIcon;
            case 6: return jackpotIcon;
            default: return null;
        }
    }

    private void GrantReward(int dayIndex)
    {
        switch (dayIndex)
        {
            case 0: MasterInfo.coinCount += 100; break;
            case 1: Debug.Log("Shield granted"); break;
            case 2: MasterInfo.coinCount += 200; break;
            case 3: Debug.Log("Magnet granted"); break;
            case 4: MasterInfo.coinCount += 300; break;
            case 5: Debug.Log("Speed boost granted"); break;
            case 6: MasterInfo.coinCount += UnityEngine.Random.Range(500, 1000); break;
        }
    }

    // =======================
    // Cheat Menu
    // =======================
    public void NextDayCheat()
    {
        PlayerPrefs.SetString(LastLoginKey, "");
        PlayerPrefs.Save();

        CheckLoginState();
        UpdateUI();
        gameObject.SetActive(true);

        Debug.Log("Cheat: Advanced to next day");
    }

    public void ResetStreakCheat()
    {
        PlayerPrefs.DeleteKey(LastLoginKey);
        PlayerPrefs.SetInt(StreakDayKey, 0);
        PlayerPrefs.SetInt(ClaimedTodayKey, 0);
        PlayerPrefs.Save();

        CheckLoginState();
        UpdateUI();
        gameObject.SetActive(true);

        Debug.Log("Cheat: Streak reset to Day 1");
    }

    public void ToggleCheatMenu(bool show)
    {
        if (cheatMenuPanel != null)
            cheatMenuPanel.SetActive(show);
    }
}
