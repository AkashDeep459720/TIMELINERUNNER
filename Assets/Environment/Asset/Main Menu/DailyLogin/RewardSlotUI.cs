using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum RewardSlotState { Past, Today, Future }

public class RewardSlotUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image iconImage;   // reward icon
    [SerializeField] private Image glowImage;   // glow overlay
    [SerializeField] private TMP_Text dayLabel; // "Day 1"

    [Header("Colors")]
    [SerializeField] private Color pastColor = Color.gray;
    [SerializeField] private Color todayColor = Color.white;
    [SerializeField] private Color futureColor = new Color(1f, 1f, 1f, 0.5f);

    public void Setup(int dayNumber, Sprite rewardIcon)
    {
        if (dayLabel) dayLabel.text = $"Day {dayNumber}";
        if (iconImage && rewardIcon) iconImage.sprite = rewardIcon;
    }

    public void SetState(RewardSlotState state)
    {
        switch (state)
        {
            case RewardSlotState.Past:
                if (iconImage) iconImage.color = pastColor;
                if (glowImage) glowImage.enabled = false;
                break;

            case RewardSlotState.Today:
                if (iconImage) iconImage.color = todayColor;
                if (glowImage) glowImage.enabled = true;
                break;

            case RewardSlotState.Future:
                if (iconImage) iconImage.color = futureColor;
                if (glowImage) glowImage.enabled = false;
                break;
        }
    }
}
