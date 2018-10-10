// this is an auto light baker if you have set lightbaking manual and uses the scenes from the build settings
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AutoLightBaker : EditorWindow
{
    bool isBaking = false;
    int finishedScenes = 0;
    int currentScene = 0;
    bool isBusy = false;
    bool isBakingCurrent = false;

    [MenuItem("Autocat/Autolightbaker")]
    static void Init()
    {
        AutoLightBaker window = (AutoLightBaker)EditorWindow.GetWindow(typeof(AutoLightBaker));
        window.Show();
    }

    void OnGUI()
    {

      if (GUILayout.Button("Bake Lights for Build")) {
        RunAutoLightBaker();
      }

    }

    void RunAutoLightBaker() {
      // Debug.Log("Starting bake process...");
      int totalScenes = SceneManager.sceneCountInBuildSettings;
      isBaking = true;
      Scene tmpScene;
      EditorSceneManager.sceneSaved += SceneSavedCallback;
      Lightmapping.completed += SceneFinishedLightbaking;
      Debug.Log("Found " + totalScenes + " to get baked.");

      while(isBaking) {

        if(!isBusy) {
          isBusy = true;

          // Debug.Log("Preparing to bake " + SceneUtility.GetScenePathByBuildIndex(currentScene));
          tmpScene = EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(currentScene), OpenSceneMode.Single);
          isBakingCurrent = true;
          while(isBakingCurrent) {
            Lightmapping.Bake();
          }

          EditorSceneManager.SaveScene(tmpScene, "", false);
        }

        if(finishedScenes == totalScenes) {
          FinishedBaking();
        }

      }

    }

    void FinishedBaking() {
      isBaking = false;
      Lightmapping.completed -= SceneFinishedLightbaking;
      EditorSceneManager.sceneSaved -= SceneSavedCallback;
      Debug.Log("Finished.");
    }

    void SceneFinishedLightbaking() {
      isBakingCurrent = false;
      // Debug.Log("Completed Lightmap baking for " + SceneUtility.GetScenePathByBuildIndex(currentScene));
    }

    void SceneSavedCallback(Scene scene) {
      // Debug.Log("Saved " + SceneUtility.GetScenePathByBuildIndex(scene.buildIndex));
      finishedScenes += 1;
      isBusy = false;
      currentScene += 1;
    }

}
