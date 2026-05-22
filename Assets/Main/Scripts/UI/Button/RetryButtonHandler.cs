using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RetryButtonHandler : MonoBehaviour
{
    private Button retryButton;
    private StageType selectStageType;

    private void Awake()
    {
        selectStageType = StageGenerator.GetStageType();
        retryButton = GetComponent<Button>();
        retryButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("Retry button clicked");

        StageGenerator.SetStageType(selectStageType);
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (StageGenerator.GetIsLatestSceneSettingMode()) { SceneLoadUtility.LoadScene("Setting"); }
        else { SceneLoadUtility.LoadScene("InGame"); }
    }
}
