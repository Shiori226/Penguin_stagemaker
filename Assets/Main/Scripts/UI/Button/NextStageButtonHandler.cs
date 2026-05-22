using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NextStageButtonHandler : MonoBehaviour
{
    private Button nextStageButton;
    private StageType selectStageType;

    private void Awake()
    {
        // "StageTypeの要素数" = "StageGeneratorが持つステージのPrefabを管理するListの要素数" である前提
        int stageCount = Enum.GetValues(typeof(StageType)).Length;
        int latestStageIndex = (int)StageGenerator.GetStageType();
        int nextStageIndex = (latestStageIndex + 1) % stageCount;
        selectStageType = (StageType)nextStageIndex;

        nextStageButton = GetComponent<Button>();
        nextStageButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("NextStage button clicked");

        StageGenerator.SetStageType(selectStageType);
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (StageGenerator.GetIsLatestSceneSettingMode()) { SceneLoadUtility.LoadScene("Setting"); }
        else { SceneLoadUtility.LoadScene("InGame"); }
    }
}
