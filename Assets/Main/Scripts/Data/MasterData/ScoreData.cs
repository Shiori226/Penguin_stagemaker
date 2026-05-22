using System;
using UnityEngine;

[Serializable]
public class ScoreData
{
    [Header("スコア設定")]
    public float baseScore = 100.0f;
    public Color32 displayColor = new Color32(255, 30, 30, 255);

    [Header("リザルト表示設定")]
    public bool shouldShowInResult = true;
    public string resultLabel = "Score Item";

    [Header("グレード評価で計算される時の設定")]
    public GradeCategory gradeCategory = GradeCategory.Others; // このスコア項目がどの評価カテゴリに属するか
    [Range(0, int.MaxValue)]
    public int gradeCap = int.MaxValue; // グレード評価計算への反映上限
}
