using UnityEngine;

/// <summary>
/// Creates the game automatically when any scene starts, so the scene
/// does not need any setup. Just open a scene and press Play.
/// If a GameManager is already in the scene, nothing extra is created.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGame()
    {
#if UNITY_2023_1_OR_NEWER
        bool alreadyExists = Object.FindAnyObjectByType<GameManager>() != null;
#else
        bool alreadyExists = Object.FindObjectOfType<GameManager>() != null;
#endif
        if (alreadyExists) return;

        new GameObject("Game").AddComponent<GameManager>();
    }
}
