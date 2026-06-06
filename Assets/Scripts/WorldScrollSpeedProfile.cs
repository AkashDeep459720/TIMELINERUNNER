using UnityEngine;

[CreateAssetMenu(fileName = "WorldScrollSpeedProfile", menuName = "Configs/World Scroll Speed Profile")]
public class WorldScrollSpeedProfile : ScriptableObject
{
    [Header("Speed")]
    public float startingScrollSpeed = 8f;
    public float maxScrollSpeed = 35f;

    [Header("Time-Based Ramp")]
    public bool useTimeBasedRamp = true;
    [Min(1f)] public float speedIncreaseIntervalSeconds = 30f;
    [Min(0f)] public float firstRampDelaySeconds = 30f;
    [Min(0f)] public float firstRampBonus = 5f;
    [Min(0f)] public float speedIncreaseAmount = 0.2f;
    public bool pauseRampWhenScrollPaused = true;

    [Header("Optional Ramp Curve")]
    public bool useRampCurve = false;
    public AnimationCurve rampCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    public float EvaluateIncrement(int rampTickIndex, int estimatedMaxTicks)
    {
        if (rampTickIndex == 0)
            return firstRampBonus;

        if (!useRampCurve || rampCurve == null || estimatedMaxTicks <= 0)
            return speedIncreaseAmount;

        float t = Mathf.Clamp01((float)rampTickIndex / estimatedMaxTicks);
        return speedIncreaseAmount * Mathf.Max(0f, rampCurve.Evaluate(t));
    }
}
