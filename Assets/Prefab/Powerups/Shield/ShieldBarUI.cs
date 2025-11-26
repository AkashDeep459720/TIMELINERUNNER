using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShieldBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject barRoot; // background or parent

    private void Awake()
    {
        if (fillImage != null)
            fillImage.fillAmount = 0f;

        if (barRoot != null)
            barRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void StartTimer(float duration)
    {
        if (barRoot != null)
            barRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        StartCoroutine(TimerRoutine(duration));
    }

    private IEnumerator TimerRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = 1f - (elapsed / duration);

            yield return null;
        }

        if (barRoot != null)
            barRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
