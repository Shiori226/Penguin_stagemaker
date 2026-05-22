using System.Collections.Generic;
using UnityEngine;

public class GradeManager : MonoBehaviour
{
    public static GradeManager Instance;

    // 各スコア項目の最大カウント数（理論上、カウントされる最大値）
    private Dictionary<string, int> scoreMaxCounts = new Dictionary<string, int>();
    // 各スコア項目の重み付け
    private Dictionary<string, float> scoreGradeWeights = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        scoreMaxCounts.Clear();
        scoreGradeWeights.Clear();
    }

    public void SetStageInfo()
    {
        foreach (var pair in DataManager.Instance.scoreData)
        {
            // デフォルトは、int型が扱える最大値を代入
            scoreMaxCounts[pair.Key] = int.MaxValue;
        }

        // 他のパラメータや、ゲーム内のデータから値が算出されるものは、以下で上書きする（"DataManager.Instance.scoreData"には反映されない）
        scoreMaxCounts["ClearTimeBonus"] = (int)DataManager.Instance.timerData["TimerKeeper"].timeLimit;
        scoreMaxCounts["FishNormal"] = GameObject.FindGameObjectsWithTag("Fish").Length;
        scoreMaxCounts["FishGold"] = GameObject.FindGameObjectsWithTag("FishGold").Length;
    }

    public void DistributeWeights()
    {
        scoreGradeWeights.Clear();

        // ===== スコア項目をカテゴリごとにグループ化する =====

        Dictionary<string, List<string>> categoryGroups = new Dictionary<string, List<string>>();

        foreach (var pair in DataManager.Instance.scoreData)
        {
            string scoreKey = pair.Key;
            string categoryName = pair.Value.gradeCategory.ToString();

            // カテゴリが未設定の場合、重み付けは分配されない
            if (string.IsNullOrEmpty(categoryName)) { continue; }

            if (!categoryGroups.ContainsKey(categoryName)) { categoryGroups[categoryName] = new List<string>(); }
            categoryGroups[categoryName].Add(scoreKey);
        }

        // ===== カテゴリごとに、そのカテゴリに属する各スコア項目への重み付けを分配 =====

        foreach (var group in categoryGroups)
        {
            string categoryName = group.Key;
            List<string> scoreKeys = group.Value;

            // カテゴリ自体の設定データ（重み付けなど）を取得
            if (!DataManager.Instance.gradeCategoryData.TryGetValue(categoryName, out var categoryData))
            {
                Debug.LogWarning($"GradeManager: Category Data not found for '{categoryName}'. Skipping.");
                continue;
            }

            float categoryTotalWeight = categoryData.gradeWeight;

            // このカテゴリ内の「スコアポテンシャル総量」を計算（ポテンシャル = MaxCount * BaseScore）
            float totalPotentialScore = 0f;
            foreach (var key in scoreKeys)
            {
                // 既に設定されている「最大カウント数」と「評価反映回数」を比べて、小さい方を適用する
                int maxCount = GetCappedMaxCount(key);
                float baseScore = DataManager.Instance.scoreData[key].baseScore;

                totalPotentialScore += Mathf.Abs(maxCount * baseScore);
            }

            // 各項目への重み配分
            foreach (string key in scoreKeys)
            {
                int maxCount = GetCappedMaxCount(key);
                float baseScore = DataManager.Instance.scoreData[key].baseScore;
                float myPotential = Mathf.Abs(maxCount * baseScore);

                float assignedWeight = 0f;

                if (totalPotentialScore > 0f)
                {
                    // (自分のポテンシャル / 全体のポテンシャル) * カテゴリの総重み付け
                    assignedWeight = (myPotential / totalPotentialScore) * categoryTotalWeight;
                }

                scoreGradeWeights[key] = assignedWeight;

                Debug.Log($"[Grade] Key:{key}, Max:{maxCount}, Weight:{assignedWeight:F4} (Category:{categoryName})");
            }
        }

        // ===== 正規化処理 =====

        float currentTotalWeight = 0f;
        foreach (var weight in scoreGradeWeights.Values) { currentTotalWeight += weight; }

        // 合計が0（全項目無効など）でなく、かつ 1.0 とズレがある場合のみ補正を実行
        if (currentTotalWeight > 0f && Mathf.Abs(currentTotalWeight - 1.0f) > 0.0001f)
        {
            float scaleFactor = 1.0f / currentTotalWeight;

            // 辞書の値を直接書き換えるためにキーのリストを作成
            List<string> keys = new List<string>(scoreGradeWeights.Keys);
            foreach (var key in keys) { scoreGradeWeights[key] *= scaleFactor; }

            Debug.Log($"[GradeManager] Weights Normalized. Scale: {scaleFactor:F4} (Original Total: {currentTotalWeight:F4})");
        }
        else if (currentTotalWeight <= 0f)
        {
            Debug.LogWarning("[GradeManager] Total weight is 0. Check ScoreData or Stage Settings.");
        }
    }

    public int GetMaxCount(string scoreKey)
    {
        if (scoreMaxCounts.TryGetValue(scoreKey, out int maxCount)) { return maxCount; }
        else { return 0; }
    }

    public int GetCappedMaxCount(string scoreKey)
    {
        if (DataManager.Instance.scoreData.TryGetValue(scoreKey, out ScoreData scoreData))
        {
            int scoreGradeCap = scoreData.gradeCap;

            // 既に設定されている「最大カウント数」と「評価反映回数」を比べて、小さい方を適用する
            return Mathf.Min(GetMaxCount(scoreKey), scoreGradeCap);
        }
        else { return 0; }
    }

    public float GetWeight(string scoreKey)
    {
        if (scoreGradeWeights.TryGetValue(scoreKey, out float weight)) { return weight; }
        else { return 0f; }
    }
}
