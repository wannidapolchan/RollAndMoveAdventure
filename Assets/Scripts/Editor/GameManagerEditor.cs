using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds two buttons under the GameManager settings in the Inspector:
/// "Build Board in Scene" and "Remove Board From Scene".
/// (Files in an "Editor" folder only run inside the Unity editor, not in the game.)
/// </summary>
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private const string MaterialFolder = "Assets/Resources/Materials";
    private const string MaterialPath = MaterialFolder + "/SpriteUnlit.mat";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(10);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Stop the game to build or change the board.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(
            "Build Board in Scene: creates the board as real objects, so you can see and move them " +
            "without pressing Play. Save the scene after moving things.\n\n" +
            "Building again replaces the board, so moved objects go back to their normal spots. " +
            "Build again after changing ladders, snakes or tiles above.", MessageType.Info);

        var game = (GameManager)target;
        if (GUILayout.Button("Build Board in Scene", GUILayout.Height(32)))
        {
            CreateUnlitMaterialIfNeeded();
            RemoveBoard();
            BoardView board = game.BuildBoardInScene();
            Undo.RegisterCreatedObjectUndo(board.gameObject, "Build Board in Scene");
            if (Camera.main != null) EditorUtility.SetDirty(Camera.main);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        if (GUILayout.Button("Remove Board From Scene"))
        {
            RemoveBoard();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
    }

    private static void RemoveBoard()
    {
        BoardView old = Object.FindAnyObjectByType<BoardView>();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
    }

    /// <summary>
    /// In URP projects, saves an unlit sprite material as an asset, so board
    /// sprites are not black in a scene without 2D lights.
    /// </summary>
    private static void CreateUnlitMaterialIfNeeded()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (unlit == null) return; // Built-in pipeline: the normal sprite material is fine

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Materials");
        }
        AssetDatabase.CreateAsset(new Material(unlit), MaterialPath);
        AssetDatabase.SaveAssets();
        GameAssets.ClearCache();
    }
}
