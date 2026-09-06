using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Editor helper: rebuilds the Build Profiles scene list in the exact order
// the game's scripts expect (see GameScenes.cs).
//
// Run it from the menu: Tools > Fractured Echoes > Setup Build Scenes
public static class FracturedEchoesBuildSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/GameCompany.unity",       // 0  GameScenes.Splash
        "Assets/Scenes/Main Menu.unity",         // 1  GameScenes.MainMenu
        "Assets/Scenes/Opening Cut Scene.unity", // 2  GameScenes.CutScene
        "Assets/Scenes/HorrorScene.unity",       // 3  GameScenes.Game
        "Assets/Scenes/Game Victory.unity",      // 4  GameScenes.Victory
    };

    [MenuItem("Tools/Fractured Echoes/Setup Build Scenes")]
    public static void SetupBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        var missing = new List<string>();

        foreach (var path in ScenePaths)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                missing.Add(path);
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        if (missing.Count > 0)
        {
            Debug.LogError("Setup Build Scenes: these scenes were not found:\n" + string.Join("\n", missing));
            return;
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();

        var log = "Build scene list set:\n";
        for (int i = 0; i < scenes.Count; i++)
            log += i + " -> " + scenes[i].path + "\n";

        Debug.Log(log);
    }
}
