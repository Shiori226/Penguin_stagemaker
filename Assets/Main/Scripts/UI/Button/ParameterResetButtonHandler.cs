using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ParameterResetButtonHandler : MonoBehaviour
{
    private Button resetButton;
    private ParameterEditorDrawer drawer;

    private void Awake()
    {
        resetButton = GetComponent<Button>();
        resetButton.onClick.AddListener(OnParameterResetButtonClicked);
    }

    private void Start()
    {
        drawer = FindObjectOfType<ParameterEditorDrawer>();
    }

    private void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnParameterResetButtonClicked);
        }
    }

    private void OnParameterResetButtonClicked()
    {
        Debug.Log("Parameter reset button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickNormal);
        DataManager.Instance.InitData();

        // ===== パラメータ編集UIの再描画処理 =====
        if (drawer != null) { drawer.DrawEditor(); }
        else { Debug.LogError("ParameterEditorDrawer が見つかりません。UIを再描画できません。"); }
    }
}