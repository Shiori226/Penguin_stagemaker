using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HomeButtonHandler : MonoBehaviour
{
    private Button homeButton;

    private void Awake()
    {
        homeButton = GetComponent<Button>();
        homeButton.onClick.AddListener(OnHomeButtonClicked);
    }

    private void OnEnable()
    {
        // 試遊モード中はラベルをステージ制作に戻る表示に差し替える
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
            label.text = StageGenerator.IsTestPlay ? "Back to Editor" : "Return to Home";
    }

    private void OnHomeButtonClicked()
    {
        Debug.Log("Home button clicked");
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (StageGenerator.IsTestPlay)
        {
            StageGenerator.SetTestPlay(false);
            SceneLoadUtility.LoadScene("StageMaker");
        }
        else
        {
            SceneLoadUtility.LoadScene("Title");
        }
    }
}
