#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Clears orphaned runtime-only objects when exiting Play Mode to avoid Hierarchy TreeView errors.
/// </summary>
[InitializeOnLoad]
static class PlayModeHierarchyCleanup
{
    static readonly string[] RuntimeRootNames =
    {
        "SegmentsRoot",
        "PortalsRoot",
        "PortalTransitionFade",
        "WorldScrollController"
    };

    static PlayModeHierarchyCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
            return;

        foreach (string rootName in RuntimeRootNames)
            DestroyRuntimeRoot(rootName);
    }

    static void DestroyRuntimeRoot(string rootName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null || go.name != rootName) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            Object.DestroyImmediate(go);
        }
    }
}
#endif
