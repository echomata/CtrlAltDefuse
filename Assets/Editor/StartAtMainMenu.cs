using UnityEditor;
using UnityEditor.SceneManagement;

namespace CtrlAltDefuse
{
    [InitializeOnLoad]
    static class StartAtMainMenu
    {
        static StartAtMainMenu()
        {
            EditorApplication.delayCall += SetStartScene;
        }

        static void SetStartScene()
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            if (menu) EditorSceneManager.playModeStartScene = menu;
        }
    }
}