using UnityEngine;
using UnityEngine.UI;

public class SpeedBarUI : MonoBehaviour
{
    public Image fillImage;    // assign Fill
    public GameObject barRoot; // assign SpeedBar (this object)
    [Range(0f, 1f)] public float lowThreshold = 0.2f;
    public float flashSpeed = 6f;
    public CanvasGroup cg;     // optional

    float duration, timeRemaining;
    bool isActive;

    void Start()
    {
        if (barRoot) barRoot.SetActive(false);
        if (cg) cg.alpha = 0f;
        if (fillImage) fillImage.fillAmount = 0f;
    }

    public void StartTimer(float seconds)
    {
        duration = Mathf.Max(0.01f, seconds);
        timeRemaining = duration;
        isActive = true;
        if (fillImage) fillImage.fillAmount = 1f;
        if (barRoot) barRoot.SetActive(true);
        if (cg) cg.alpha = 1f;
    }

    public void StopTimer()
    {
        isActive = false;
        if (barRoot) barRoot.SetActive(false);
        if (cg) cg.alpha = 0f;
        if (fillImage) fillImage.fillAmount = 0f;
    }

    void Update()
    {
        if (!isActive) return;

        timeRemaining -= Time.deltaTime;
        float t = Mathf.Clamp01(timeRemaining / duration);
        if (fillImage) fillImage.fillAmount = t;

        if (cg) cg.alpha = (t <= lowThreshold)
            ? 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.time * flashSpeed))
            : 1f;

        if (timeRemaining <= 0f) StopTimer();
    }
}
