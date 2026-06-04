using UnityEngine;

[CreateAssetMenu(fileName = "RobotSpawnProfile", menuName = "Configs/Robot Spawn Profile")]
public class RobotSpawnProfile : ScriptableObject
{
    [Header("Distance Ramp")]
    public float distanceForMaxDifficulty = 500f;
    [Range(0f, 1f)] public float maxDifficultyFactor = 1f;
    public bool useDifficultyCurve = false;
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Combo")]
    [Range(0f, 1f)] public float comboChanceStart = 0.1f;
    [Range(0f, 1f)] public float comboChanceEnd = 0.7f;
    public bool useComboChanceCurve = false;
    public AnimationCurve comboChanceCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 0.7f);
    public float minSpacingInsideCombo = 12f;
    public bool emptySegmentAfterCombo = true;

    [Header("Spacing (looser at low difficulty, tighter at high)")]
    public float minGroupSpacing = 10f;
    public float maxGroupSpacing = 18f;
    public bool useSpacingCurve = false;
    public AnimationCurve spacingCurve = AnimationCurve.Linear(0f, 18f, 1f, 10f);

    [Header("Breathing Room")]
    public int emptySegmentInterval = 5;
    public float graceDistance = 80f;
    [Range(0f, 1f)] public float graceMaxDifficulty = 0.25f;
    public int graceSegmentsAfterRunStart = 2;

    [Header("Weighted Prefab Pick")]
    public bool useWeightedPick = true;
    public float weightJumpSlide = 1f;
    public float weightLaneBlocker = 1f;
    public float weightFullLane = 0.5f;
    [Range(0f, 1f)] public float fullLaneMaxChance = 0.1f;

    public float EvaluateDifficultyFactor(float distanceTravelled, bool useScaling)
    {
        if (!useScaling) return 0f;

        float raw = distanceForMaxDifficulty > 0f
            ? distanceTravelled / distanceForMaxDifficulty
            : 0f;

        if (distanceTravelled < graceDistance)
            raw = Mathf.Min(raw, graceMaxDifficulty);

        if (useDifficultyCurve && difficultyCurve != null)
            raw = difficultyCurve.Evaluate(Mathf.Clamp01(raw));

        return Mathf.Clamp(raw, 0f, maxDifficultyFactor);
    }

    public float EvaluateComboChance(float difficultyFactor)
    {
        if (useComboChanceCurve && comboChanceCurve != null)
            return Mathf.Clamp01(comboChanceCurve.Evaluate(difficultyFactor));

        return Mathf.Lerp(comboChanceStart, comboChanceEnd, difficultyFactor);
    }

    public float EvaluateGroupSpacing(float difficultyFactor)
    {
        if (useSpacingCurve && spacingCurve != null)
            return spacingCurve.Evaluate(difficultyFactor);

        return Mathf.Lerp(maxGroupSpacing, minGroupSpacing, difficultyFactor);
    }
}
