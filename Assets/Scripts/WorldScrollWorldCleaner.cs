using UnityEngine;

/// <summary>
/// Clears spawned props (obstacles, coins, powerups, orbs) before a timeline swap.
/// </summary>
public static class WorldScrollWorldCleaner
{
    private static readonly string[] PropRootNames =
    {
        "ObstaclesRoot",
        "CoinsRoot",
        "PowerupsRoot",
        "OrbsRoot"
    };

    public static void ClearAllPropRoots()
    {
        foreach (string rootName in PropRootNames)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) continue;
            ClearTransformChildren(root.transform);
        }
    }

    private static void ClearTransformChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null) continue;

            GameObject go = child.gameObject;
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Despawn(go);
            else
                go.SetActive(false);
        }
    }
}
