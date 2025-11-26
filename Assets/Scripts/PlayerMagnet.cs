using UnityEngine;
using System.Collections;

public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float magnetRadius = 5f;
    public float magnetForce = 15f;

    [Header("Visuals & UI")]
    public GameObject magnetVFX;
    public MagnetBarUI magnetBarUI;

    private bool isMagnetActive = false;

    private void Start()
    {
        if (magnetVFX != null)
            magnetVFX.SetActive(false);
    }

    public void ActivateMagnet(float duration)
    {
        if (!isMagnetActive)
        {
            if (magnetBarUI != null)
                magnetBarUI.StartTimer(duration);

            StartCoroutine(MagnetRoutine(duration));
        }
    }

    private IEnumerator MagnetRoutine(float duration)
    {
        isMagnetActive = true;

        if (magnetVFX != null)
            magnetVFX.SetActive(true);

        float timer = 0f;

        while (timer < duration)
        {
            // Find all active coins by tag instead of relying on Layer
            GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");

            foreach (GameObject coin in coins)
            {
                if (coin == null || !coin.activeInHierarchy) continue;

                float dist = Vector3.Distance(transform.position, coin.transform.position);
                if (dist <= magnetRadius)
                {
                    Vector3 dir = (transform.position - coin.transform.position).normalized;
                    float step = magnetForce * Time.deltaTime;

                    coin.transform.position = Vector3.MoveTowards(coin.transform.position, transform.position, step);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        isMagnetActive = false;

        if (magnetVFX != null)
            magnetVFX.SetActive(false);
    }
}
