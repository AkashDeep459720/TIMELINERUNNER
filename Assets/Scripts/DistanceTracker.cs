using UnityEngine;
using TMPro;

public class DistanceTracker : MonoBehaviour
{
    public static DistanceTracker Instance;

    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private Transform player;

    public int currentDistance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (player == null) return;

        // measure distance as Z position of player
        currentDistance = Mathf.RoundToInt(player.position.z);

        // update UI text
        if (distanceText)
            distanceText.text = $"{FormatNumber(currentDistance)}m";
    }

    private string FormatNumber(int num)
    {
        return string.Format("{0:n0}", num); // adds commas
    }
}
