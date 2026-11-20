using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BreachPoint.EditorTools
{
    /// <summary>
    /// Creates the assets the code-built game needs: base URP materials (in Resources so builds include them)
    /// and Main.unity (one object running GameFlow), registered as the first build scene.
    /// Menu: Breach Point → Setup Project. Batch: -executeMethod BreachPoint.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        public const string MainScene = "Assets/_BreachPoint/Scenes/Main.unity";
        const string MatDir = "Assets/_BreachPoint/Resources/Materials";

        [MenuItem("Breach Point/Setup Project (materials + Main scene)")]
        public static void Run()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(Path.GetDirectoryName(MainScene));
            EnsureMaterial("BP_Lit", "Universal Render Pipeline/Lit");
            EnsureMaterial("BP_Unlit", "Universal Render Pipeline/Unlit");
            AssetDatabase.SaveAssets();

            if (!File.Exists(MainScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("BreachPoint").AddComponent<GameFlow>();
                EditorSceneManager.SaveScene(scene, MainScene);
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != MainScene).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(MainScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.Refresh();
            Debug.Log("[BreachPoint] Project setup complete: " + MainScene);
        }

        static void EnsureMaterial(string name, string shader)
        {
            string path = $"{MatDir}/{name}.mat";
            if (File.Exists(path)) return;
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[BreachPoint] Shader not found: " + shader); return; }
            AssetDatabase.CreateAsset(new Material(sh) { name = name }, path);
        }

        [MenuItem("Breach Point/Play Game (Main scene)")]
        static void PlayMain()
        {
            if (!File.Exists(MainScene)) Run();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainScene);
            EditorApplication.isPlaying = true;
        }
    }
}
