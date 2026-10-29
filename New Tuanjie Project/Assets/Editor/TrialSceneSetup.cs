using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class TrialSceneSetup
{
    [MenuItem("Trial X/Create Trail Pack Scene")]
    public static void CreateScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Desert environment").AddComponent<TrialWorld>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Canyon.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Canyon.unity", true) };
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.productName = "Trial X — The First Five";
        PlayerSettings.companyName = "Trial X";
        AssetDatabase.SaveAssets();
    }
}
