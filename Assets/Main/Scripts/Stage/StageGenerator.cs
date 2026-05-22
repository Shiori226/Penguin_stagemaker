using System.Collections.Generic;
using StageMaker;
using UnityEngine;

public enum StageType
{
    Practice = 0,
    FirstStage = 1,
    SecondStage = 2,
    ThirdStage = 3,
    Custom = 4,
}

public class StageGenerator : MonoBehaviour
{
    [SerializeField]
    private static StageType stageType = StageType.Practice;

    /// <summary>
    /// stageType == Custom のときに読み込む CustomStageData の id
    /// </summary>
    private static string selectedCustomStageId = string.Empty;

    /// <summary>
    /// 直近に生成した CustomStageData。CSV エクスポート時にステージ情報を書き出すために使う。
    /// </summary>
    private static CustomStageData lastBuiltCustomStage;

    [SerializeField]
    private List<GameObject> stagePrefabs;

    // ===== プレイ用のシーンか本番用か設定用かを判別するための変数 =====
    [SerializeField]
    private bool isSettingMode = false;
    public bool IsSettingMode => isSettingMode;

    private static bool isLatestSceneSettingMode; // 最後にプレイしたモードが本番用か設定用かを判別するための変数

    /// <summary>
    /// ステージ制作画面の PLAY ボタンから起動した試遊モードかどうか。
    /// 試遊中はゴール後にリザルト画面を表示せず、ステージ制作画面に直帰する。
    /// </summary>
    private static bool isTestPlay = false;
    public static bool IsTestPlay => isTestPlay;
    public static void SetTestPlay(bool value) { isTestPlay = value; }

    /// <summary>
    /// 試遊から戻ったときに再開する編集中ステージの ID。
    /// StageMakerSceneController が読み取り後にクリアする。
    /// </summary>
    private static string returnToEditStageId = string.Empty;
    public static string ReturnToEditStageId => returnToEditStageId;
    public static void SetReturnToEditStageId(string id) { returnToEditStageId = id ?? string.Empty; }

    private void Awake()
    {
        GenerateStage((int)stageType);
        isLatestSceneSettingMode = isSettingMode;
        // ※カスタムステージでも Penguin には手を加えない。
        //   PlatformStart は固定で (0, 0, 0) に置かれており、Penguin の元位置 (0, 0.5, 0)
        //   の真下にあるので、デフォルトステージと同じく自然落下で着地する。
    }

    private void GenerateStage(int index)
    {
        Debug.Log("Generating stage with index: " + index);

        if ((StageType)index == StageType.Custom)
        {
            BuildCustomStage();
            return;
        }

        if (stagePrefabs == null || stagePrefabs.Count == 0)
        {
            Debug.LogWarning("[StageGenerator] stagePrefabs is empty.");
            return;
        }

        // リストの範囲内にインデックスを正規化
        int normalizedIndex = ((index % stagePrefabs.Count) + stagePrefabs.Count) % stagePrefabs.Count;
        Instantiate(stagePrefabs[normalizedIndex], Vector3.zero, Quaternion.identity);
    }

    private void BuildCustomStage()
    {
        var data = CustomStageRepository.Load(selectedCustomStageId);
        if (data == null)
        {
            Debug.LogError($"[StageGenerator] Custom stage '{selectedCustomStageId}' not found. Falling back to Practice.");
            stageType = StageType.Practice;
            GenerateStage((int)StageType.Practice);
            return;
        }

        CustomStageBuilder.Build(data);
        lastBuiltCustomStage = data;
    }

    public static CustomStageData GetLastBuiltCustomStage()
    {
        return lastBuiltCustomStage;
    }

    public static void SetStageType(StageType newStageType)
    {
        stageType = newStageType;
    }

    public static StageType GetStageType()
    {
        return stageType;
    }

    public static void SetSelectedCustomStageId(string stageId)
    {
        selectedCustomStageId = stageId ?? string.Empty;
    }

    public static string GetSelectedCustomStageId()
    {
        return selectedCustomStageId;
    }

    public static bool GetIsLatestSceneSettingMode()
    {
        return isLatestSceneSettingMode;
    }
}
