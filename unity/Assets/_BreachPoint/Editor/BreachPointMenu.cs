using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BreachPoint.EditorTools
{
    public static class BreachPointMenu
    {
        const string ScenePath = "Assets/_BreachPoint/Scenes/ControllerCheck.unity";

        /// <summary>Opens (creating on first use) a scene that only runs the controller check.</summary>
        [MenuItem("Breach Point/Controller Check Scene")]
        static void OpenControllerCheck()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.024f, 0.04f, 0.075f);
            }
            new GameObject("ControllerCheck").AddComponent<ControllerCheck>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}
