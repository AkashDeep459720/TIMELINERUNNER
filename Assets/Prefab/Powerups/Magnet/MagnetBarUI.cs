using UnityEngine;
using UnityEngine.UI;

public class MagnetBarUI : MonoBehaviour
{
    public Image fillImage;        // drag the FILL image
    public GameObject barRoot;     // drag the whole MagnetBar_BG here

    private float duration;
    private float timeRemaining;
    private bool isActive = false;

    void Start()
    {
        if (barRoot != null)
            barRoot.SetActive(false); // hide by default
    }

    public void StartTimer(float magnetDuration)
    {
        duration = magnetDuration;
        timeRemaining = magnetDuration;
        isActive = true;
        fillImage.fillAmount = 1f;

        if (barRoot != null)
            barRoot.SetActive(true);
    }

    void Update()
    {
        if (!isActive) return;

        timeRemaining -= Time.deltaTime;
        fillImage.fillAmount = timeRemaining / duration;

        if (timeRemaining <= 0f)
        {
            isActive = false;

            if (barRoot != null)
                barRoot.SetActive(false);
        }
    }
}
