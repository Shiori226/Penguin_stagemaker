using System;
using System.Collections.Generic;
using StageMaker;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class StageSelector : MonoBehaviour
{
    /// <summary>
    /// タイトルのステージ選択に表示しない(選択不可にする)ステージ。
    /// </summary>
    private static readonly HashSet<StageType> HiddenStages = new HashSet<StageType>
    {
        StageType.FirstStage,
        StageType.SecondStage,
        StageType.ThirdStage,
    };

    private ToggleGroup toggleGroup;

    [SerializeField]
    private GameObject stageTogglePrefab;

    [SerializeField]
    private StartButtonHandler startButton;

    private void Start()
    {
        toggleGroup = GetComponent<ToggleGroup>();

        // 非表示ステージが選択されたままだと Start ボタンで起動できてしまうため
        // Practice に戻しておく
        if (HiddenStages.Contains(StageGenerator.GetStageType()))
        {
            StageGenerator.SetStageType(StageType.Practice);
        }

        // 1) 既存のデフォルトステージ用トグルを生成 (Custom は別途下で扱う)
        foreach (StageType stage in Enum.GetValues(typeof(StageType)))
        {
            if (stage == StageType.Custom || HiddenStages.Contains(stage)) { continue; }
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
