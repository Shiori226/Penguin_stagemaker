using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    // 各スコア項目のカウント数
    private Dictionary<string, int> scoreEventCounts = new Dictionary<string, int>();

    // クリアフラグ
    public bool isStageCleared { get; set; } = false;

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
        scoreEventCounts.Clear();
    }

    public void AddCount(string scoreEventKey)
    {
        if(scoreEventCounts.ContainsKey(scoreEventKey))
        {
            scoreEventCounts[scoreEventKey]++;
        }
        else
        {
            scoreEventCounts[scoreEventKey] = 1;
        }
    }

    public void SetCount(string scoreEventKey, int count)
    {
        scoreEventCounts[scoreEventKey] = count;
    }

    public int GetCount(string scoreEventKey)
    {
        if(scoreEventCounts.ContainsKey(scoreEventKey))
        {
            return scoreEventCounts[scoreEventKey];
        }
        else
        {
            Debug.LogWarning($"ScoreManager: GetCount called for non-existing key '{scoreEventKey}'. Returning 0.");
            return 0;
        }
    }

    public int GetScore(string scoreEventKey)
    {
        int count = GetCount(scoreEventKey);
        float baseScore = DataManager.Instance.scoreData[scoreEventKey].baseScore;
        return Mathf.RoundToInt(count * baseScore);
    }

    public int GetTotalScore()
    {
        int total = 0;
        foreach(var key in scoreEventCounts.Keys)
        {
            total += GetScore(key);
        }
        return total;
    }

    public void SetCleared(bool isClear)
    {
        isStageCleared = isClear;
    }
}
