using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace OceanGame
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;

    [InitializeOnLoad]
    public static class PlayFromStartScene
    {
        static PlayFromStartScene()
        {
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Game/Scenes/MainMenuScene.unity");
            EditorSceneManager.playModeStartScene = startScene;
        }
    }
#endif
}