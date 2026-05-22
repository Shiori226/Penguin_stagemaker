using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultDrawDirector : MonoBehaviour
{
    [Header("Result Message")]
    [SerializeField] private ResultMessageDrawer resultMessageDrawer;

    [Header("Detail Scores")]
    [SerializeField] private Transform detailScoreParent;
    [SerializeField] private GameObject detailScorePrefab;
    [SerializeField] private Sprite defaultIcon;

    [Header("Total Score")]
    [SerializeField] private TotalScoreDrawer totalScoreDrawer;

    [Header("Rank")]
    [SerializeField] private GradeDrawer gradeDrawer;

    private Coroutine sequence;
    private bool isDrawing;
    private bool isSkipping = false;

    private struct ScoreDrawContext
    {
        public DetailScoreDrawer Drawer;
        public string Label;
        public Sprite Icon;
        public int Count;
        public float Score;
    }

    private void Start()
    {
        // 最初は非表示にする（演出時に表示する）
        if (totalScoreDrawer) { totalScoreDrawer.gameObject.SetActive(false); }
        if (gradeDrawer) { gradeDrawer.gameObject.SetActive(false); }

        // スコア情報を持つクラスが存在しない場合は、リザルト画面を描画しない
        if (!ScoreManager.Instance) { return; }

        Debug.Log("ResultDrawDirector: Start drawing result screen.");

        bool isCleared = ScoreManager.Instance.isStageCleared;
        if (resultMessageDrawer) { resultMessageDrawer.DrawResultMessage(isCleared); }

        Play();
    }

    private void Update()
    {
        // 描画中で、まだスキップしていない場合
        if (isDrawing && !isSkipping)
        {
            // "Submit"ボタン（Enter, Space, Aボタンなど）押下時
            if (InputDataManager.Instance.inputData.submit)
            {
                // 描画演出をスキップする
                Skip();
            }
        }
    }

    public void Play()
    {
        // 多重起動防止
        if (isDrawing) { return; }
        if (sequence != null) { Stop(); }

        isDrawing = true;
        isSkipping = false;

        sequence = StartCoroutine(DrawScores());
    }

    public void Stop()
    {
        if (sequence != null) { StopCoroutine(sequence); }
        sequence = null;
        isDrawing = false;
        isSkipping = false;
    }

    private void Skip()
    {
        isSkipping = true;

        // 実行中のアニメーションコルーチンを停止
        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }

        // 途中まで生成された項目をすべて削除（重複防止、後に完成版を再生成する）
        foreach (Transform child in detailScoreParent)
        {
            Destroy(child.gameObject);
        }

        DrawScoresInstant();

        isDrawing = false;
        isSkipping = false;
    }

    private IEnumerator DrawScores()
    {
        if (!CheckScoreManager()) { yield break; }

        foreach (var detailScore in DataManager.Instance.scoreData)
        {
            if (!detailScore.Value.shouldShowInResult) { continue; }

            if (TryCreateDetailDrawer(detailScore, out var ctx))
            {
                // アニメーション実行
                yield return ctx.Drawer.DrawDetailScore(ctx.Label, ctx.Icon, ctx.Count, ctx.Score);
            }
        }

        // トータルスコア
        if (totalScoreDrawer)
        {
            totalScoreDrawer.gameObject.SetActive(true);
            yield return totalScoreDrawer.DrawTotalScore(ScoreManager.Instance.GetTotalScore());
        }

        // ランク
        if (gradeDrawer)
        {
            gradeDrawer.gameObject.SetActive(true);

            float gradeScore = GetGradeScore();

            string gradeLabel = GetGrade(gradeScore, ScoreManager.Instance.isStageCleared);
            Color32 gradeColor = DataManager.Instance.gradeData[gradeLabel].displayColor;

            yield return gradeDrawer.DrawGrade(gradeLabel, gradeColor);
        }

        isDrawing = false;
        sequence = null;
    }

    private void DrawScoresInstant()
    {
        if (!CheckScoreManager()) { return; }

        foreach (var detailScore in DataManager.Instance.scoreData)
        {
            if (!detailScore.Value.shouldShowInResult) { continue; }

            if (TryCreateDetailDrawer(detailScore, out ScoreDrawContext ctx))
            {
                // 即時実行
                ctx.Drawer.SetInstant(ctx.Label, ctx.Icon, ctx.Count, ctx.Score);
            }
        }

        // トータルスコア
        if (totalScoreDrawer)
        {
            totalScoreDrawer.gameObject.SetActive(true);
            totalScoreDrawer.SetInstant(ScoreManager.Instance.GetTotalScore());
        }

        // グレード
        if (gradeDrawer)
        {
            gradeDrawer.gameObject.SetActive(true);

            float  gradeScore = GetGradeScore();

            string gradeLabel = GetGrade(gradeScore, ScoreManager.Instance.isStageCleared);
            Color32 gradeColor = DataManager.Instance.gradeData[gradeLabel].displayColor;

            gradeDrawer.SetInstant(gradeLabel, gradeColor);
        }
    }

    private bool CheckScoreManager()
    {
        if (!ScoreManager.Instance)
        {
            Debug.LogWarning("ResultDrawDirector: ScoreManager.Instance is null.");

            isDrawing = false;
            sequence = null;
            return false;
        }
        return true;
    }

    private bool TryCreateDetailDrawer(KeyValuePair<string, ScoreData> dataPair, out ScoreDrawContext context)
    {
        context = new ScoreDrawContext();

        // Prefab生成
        GameObject obj = Instantiate(detailScorePrefab, detailScoreParent);
        if (!obj.TryGetComponent(out DetailScoreDrawer drawer))
        {
            Destroy(obj);
            return false;
        }

        // データ計算
        string label = dataPair.Value.resultLabel;
        Sprite icon = Resources.Load<Sprite>("Icons/" + label) ?? defaultIcon;
        int count = ScoreManager.Instance.GetCount(dataPair.Key);
        float score = count * dataPair.Value.baseScore;

        // 構造体に詰めて返す
        context.Drawer = drawer;
        context.Label = label;
        context.Icon = icon;
        context.Count = count;
        context.Score = score;

        return true;
    }

    private float GetGradeScore()
    {
        List<GradeCalculater.GradeFactor> factors = new List<GradeCalculater.GradeFactor>();

        DataManager dataManager = DataManager.Instance;
        ScoreManager scoreManager = ScoreManager.Instance;
        GradeManager gradeManager = GradeManager.Instance;

        // 各スコア項目の重みを、「各スコア項目が属する評価カテゴリ」と「基礎スコア」を基に割り振る
        gradeManager.DistributeWeights();

        foreach (var pair in dataManager.scoreData)
        {
            string key = pair.Key;
            ScoreData data = pair.Value;

            // このスコア項目が、どの評価カテゴリに属しているかを取得
            GradeCategory gradeCategory= data.gradeCategory;
            string categoryName = gradeCategory.ToString();

            // このスコア項目が属しているカテゴリの情報を取得
            GradeCategoryData categoryData = dataManager.gradeCategoryData[categoryName];

            // 計算形式が設定されていない場合、グレード評価計算をスキップ
            if (categoryData.evaluationType == GradeEvaluationType.NotEvaluate) { continue; }

            // リストに追加
            factors.Add(new GradeCalculater.GradeFactor
            {
                currentCount = scoreManager.GetCount(key),
                maxCount = gradeManager.GetCappedMaxCount(key),
                weight = gradeManager.GetWeight(key),
                type = categoryData.evaluationType
            });
        }

        // グレード評価計算の結果（数値）を取得
        float gradeScore = GradeCalculater.CalculateGrade(factors);
        Debug.Log("Grade Score: " + gradeScore);

        return gradeScore;
    }

    private string GetGrade(float score, bool isCleared)
    {
        string label = "";
        float capUnclearedThreshold = 50f;

        bool found = false;
        float best = default;

        // 未クリアの場合はスコアを丸め、クリアできた場合はそのままのスコアで計算する
        float cappedScore = isCleared ? score : Mathf.Min(score, capUnclearedThreshold);

        foreach (var scoreGrade in DataManager.Instance.gradeData)
        {
            if (scoreGrade.Value.gradeThreshold.CompareTo(cappedScore) <= 0)
            {
                if (!found || scoreGrade.Value.gradeThreshold.CompareTo(best) > 0)
                {
                    best = scoreGrade.Value.gradeThreshold;
                    label = scoreGrade.Key;
                    found = true;
                }
            }
        }

        return label;
    }
}
