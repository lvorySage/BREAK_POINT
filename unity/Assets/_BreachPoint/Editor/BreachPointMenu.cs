using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BreachPoint.EditorTools
{
    public static class BreachPointMenu
    {
        const string ControllerScene = "Assets/_BreachPoint/Scenes/ControllerCheck.unity";

        /// <summary>Menu actions can't open scenes during play mode: stop first, then continue.</summary>
        public static void RunInEditMode(Action action)
        {
            if (!EditorApplication.isPlaying)
            {
                action();
                return;
            }
            void OnChange(PlayModeStateChange s)
            {
                if (s != PlayModeStateChange.EnteredEditMode) return;
                EditorApplication.playModeStateChanged -= OnChange;
                EditorApplication.delayCall += () => action();
            }
            EditorApplication.playModeStateChanged += OnChange;
            EditorApplication.isPlaying = false;
        }

        /// <summary>Opens (creating on first use) a scene that only runs the controller check, then plays it.</summary>
        [MenuItem("Breach Point/Controller Check Scene", priority = 20)]
        static void OpenControllerCheck() => RunInEditMode(() =>
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ControllerScene))
            {
                EditorSceneManager.OpenScene(ControllerScene);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ControllerScene));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.024f, 0.04f, 0.075f);
                }
                new GameObject("ControllerCheck").AddComponent<ControllerCheck>();
                EditorSceneManager.SaveScene(scene, ControllerScene);
                AssetDatabase.Refresh();
            }
            PlayStartsGame.PlayOtherSceneOnce();
            EditorApplication.isPlaying = true;
        });
    }
}
