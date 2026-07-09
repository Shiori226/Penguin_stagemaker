using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 実行時に動的注入している VolumePanel を、編集モードでシーンに実体化する
/// メニュー。焼き込んだ後は Scene ビュー/Inspector で位置・サイズを
/// グラフィカルに調整して、そのままシーンに保存できる。
/// (TitleManager 側の注入は「既にあれば何もしない」ため二重生成されない)
/// </summary>
public static class VolumePanelBakeMenu
{
    [MenuItem("Tools/SlidingPenguin/音量パネルをシーンに配置")]
    private static void BakeVolumePanel()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("音量パネル",
                "Canvas が見つかりません。Title シーンを開いてから実行してください。", "OK");
            return;
        }

        var existing = canvas.transform.Find("VolumePanel");
        if (existing != null)
        {
            // 既にあるなら選択するだけ (そのまま Scene ビューで調整できる)
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[VolumePanelBakeMenu] VolumePanel は既にシーンにあります。選択しました。");
            return;
        }

        var panel = VolumeSliderBinder.Build(canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Create VolumePanel");
        Selection.activeGameObject = panel;
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log("[VolumePanelBakeMenu] VolumePanel をシーンに配置しました。位置を調整してシーンを保存してください。");
    }
}
