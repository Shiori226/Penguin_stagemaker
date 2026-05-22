using System;
using StageMaker;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class StageSelector : MonoBehaviour
{
    private ToggleGroup toggleGroup;

    [SerializeField]
    private GameObject stageTogglePrefab;

    [SerializeField]
    private StartButtonHandler startButton;

    private void Start()
    {
        toggleGroup = GetComponent<ToggleGroup>();

        // 1) 既存のデフォルトステージ用トグルを生成 (Custom は別途下で扱う)
        foreach (StageType stage in Enum.GetValues(typeof(StageType)))
        {
            if (stage == StageType.Custom) { continue; }
            var toggleObject = Instantiate(stageTogglePrefab, transform);
            var toggleController = toggleObject.GetComponent<StageToggleController>();
            toggleController.Initialize(stage, toggleGroup);
        }

        // 2) Stage Maker で作成したカスタムステージを順に追加
        var customStages = CustomStageRepository.LoadAll();
        foreach (var data in customStages)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) { continue; }
            var toggleObject = Instantiate(stageTogglePrefab, transform);
            var toggleController = toggleObject.GetComponent<StageToggleController>();
            toggleController.Initialize(StageType.Custom, toggleGroup, data.id, data.displayName);
        }
    }
}
