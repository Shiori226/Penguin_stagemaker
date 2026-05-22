using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageToggleController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    private Toggle toggle;
    private StageType stageType;

    /// <summary>
    /// stageType == Custom のときに使用するカスタムステージ ID。
    /// 空文字列の場合はデフォルトステージのトグルとして扱う。
    /// </summary>
    private string customStageId = string.Empty;

    public void Initialize(StageType type, ToggleGroup toggleGroup)
    {
        Initialize(type, toggleGroup, customStageId: string.Empty, displayName: type.ToString());
    }

    public void Initialize(StageType type, ToggleGroup toggleGroup, string customStageId, string displayName)
    {
        stageType = type;
        this.customStageId = customStageId ?? string.Empty;

        toggle = GetComponent<Toggle>();
        toggle.group = toggleGroup;

        // 直近選択していた組み合わせを再選択する
        if (type == StageGenerator.GetStageType())
        {
            bool sameId = (type != StageType.Custom) || (this.customStageId == StageGenerator.GetSelectedCustomStageId());
            if (sameId) { toggle.isOn = true; }
        }
        toggle.onValueChanged.AddListener(OnToggleChanged);

        SetLabel(displayName);
    }

    private void SetLabel(string newLabel)
    {
        if (label == null) { return; }
        // CamelCase 由来の名前なら単語間にスペースを入れる
        string formatted = StringCaseUtility.ToSpacedWords(newLabel);
        label.SetText(formatted);
    }

    public void OnToggleChanged(bool isOn)
    {
        if (!isOn) { return; }
        StageGenerator.SetStageType(stageType);
        if (stageType == StageType.Custom)
        {
            StageGenerator.SetSelectedCustomStageId(customStageId);
        }
        else
        {
            StageGenerator.SetSelectedCustomStageId(string.Empty);
        }
    }
}
