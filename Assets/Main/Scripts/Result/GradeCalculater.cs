using System.Collections.Generic;
using UnityEngine;

// 評価カテゴリの定義
public enum GradeCategory
{
    ClearTimeBonus,
    Fish,
    CourseOut,
    Others,
}

// 評価方式の定義
public enum GradeEvaluationType
{
    Positive, // 加点方式（多いほど良い）
    Negative, // 減点方式（少ないほど良い）
    NotEvaluate, // 評価に含めない
}

public static class GradeCalculater
{
    public struct GradeFactor
    {
        public float currentCount; // 現在の値
        public float maxCount; // 値の上限
        public float weight; // 重み
        public GradeEvaluationType type; // 評価タイプ
    }

    public static float CalculateGrade(List<GradeFactor> factors)
    {
        if (factors == null || factors.Count == 0) { return 0f; }

        float totalWeightedScore = 0f;
        float totalWeight = 0f;

        foreach (GradeFactor factor in factors)
        {
            if (factor.type == GradeEvaluationType.NotEvaluate || factor.maxCount <= 0f || factor.weight <= 0f)
            {
                // このスコア項目は「存在しないもの」として扱い、評価計算には含めない
                continue;
            }

            float rate = 0f;

            switch (factor.type)
            {
                case GradeEvaluationType.Positive:
                    // 達成率 = 現在値 / 最大値
                    rate = Mathf.Clamp01(factor.currentCount / factor.maxCount);
                    break;
                case GradeEvaluationType.Negative:
                    // 達成率 = 1.0 - (現在値 / 許容限界) ※許容限界を超えたら0点とする
                    rate = 1.0f - Mathf.Clamp01(factor.currentCount / factor.maxCount);
                    break;
            }

            totalWeightedScore += rate * factor.weight;
            totalWeight += factor.weight;
        }

        // 重みの合計で割って正規化 (0.0 ~ 1.0)
        float normalizedResult = (totalWeight > 0f) ? (totalWeightedScore / totalWeight) : 0f;

        // 100点満点に変換して返す
        return Mathf.RoundToInt(normalizedResult * 100.0f);
    }
}
