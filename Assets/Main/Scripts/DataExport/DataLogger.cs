using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity シーン内のパラメータロギング全体を管理するシングルトン。
/// 試行(Trial)のライフサイクル、Stream/Snapshot の制御、エクスポートの起点。
/// </summary>
public sealed class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    // 内部モジュール
    private readonly DataRegistry registry = new();
    private readonly RecordingClock clock = new();
    private readonly LogItemCache streamCache = new();
    private readonly LogItemCache snapshotCache = new();

    private StreamLogger streamLogger;
    private SnapshotLogger snapshotLogger;

    [SerializeField] private List<LogItemOrderOverride> streamOrderOverrides = new();
    [SerializeField] private List<LogItemOrderOverride> snapshotOrderOverrides = new();

    // 試行管理用
    private bool isTrialInProgress = false; // 「試行中か」を管理する
    private int streamBookmark = 0; // 現在の試行の Stream データ開始インデックス
    private int snapshotBookmark = 0; // 現在の試行の Snapshot データ開始インデックス

    // 確定した試行のデータ範囲（end は排他的）
    private readonly List<TrialRange> committedTrials = new();

    // デバッグ・インスペクタ表示用
    public IReadOnlyList<LogItem> StreamItems => registry.StreamItems;
    public IReadOnlyList<LogItem> SnapshotItems => registry.SnapshotItems;

    // ===== Unity メッセージ =====

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Logger 準備
        streamLogger = new StreamLogger(registry, streamCache);
        snapshotLogger = new SnapshotLogger(registry, snapshotCache);

        // 各 Logger が DataRegistry のイベントを購読
        streamLogger.Subscribe(registry);
        snapshotLogger.Subscribe(registry);
    }

    private void LateUpdate()
    {
        // 試行中であれば時計を進める
        clock.Tick(Time.unscaledDeltaTime);

        // Stream記録中であればデータを記録する（isStreaming, isPaused のチェックは streamLogger 内部で行う）
        streamLogger?.TickStream(clock.ElapsedSeconds, clock.ElapsedFrames);
    }

    private void OnDisable()
    {
        // アプリケーション終了時や無効化時に安全のためポーズ
        streamLogger?.PauseStream();
        clock.Pause();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // イベント購読を解除
        streamLogger?.Unsubscribe(registry);
        snapshotLogger?.Unsubscribe(registry);
    }

    // ===== API: 試行管理 (Trial) =====

    /// <summary>
    /// 新しい試行を開始。
    /// ※シーン遷移後や実験開始時に呼び出してください。
    /// </summary>
    public void StartTrial()
    {
        if (isTrialInProgress) { return; }

        // 1. レジストリをリフレッシュし、現在のシーンのオブジェクトをスキャン
        RefreshRegistry();

        // 2. 各ロガーをリセットし、新しい試行の準備をさせる
        streamLogger.ResetForNewTrial();
        snapshotLogger.ResetForNewTrial();

        // 3. この試行のデータがキャッシュのどこから始まるかを記録
        streamBookmark = streamLogger.RowCount;
        snapshotBookmark = snapshotLogger.RowCount;

        // 4. 時計をリセットして開始
        clock.Start();

        isTrialInProgress = true;
        Debug.Log("[DataLogger] 試行を開始しました。");
    }

    /// <summary>
    /// 現在の試行を正常に終了し、記録を確定。
    /// ※シーン遷移前や実験終了時に呼び出してください。
    /// </summary>
    public void EndTrial()
    {
        if (!isTrialInProgress) { return; }

        // 進行中のStream記録と時計を停止
        streamLogger.StopStream();
        clock.Stop();

        // この試行で記録された範囲を確定
        int s0 = streamBookmark;
        int s1 = streamLogger.RowCount;
        int p0 = snapshotBookmark;
        int p1 = snapshotLogger.RowCount;

        if (s1 > s0 || p1 > p0) // 何か記録があれば
        {
            committedTrials.Add(new TrialRange { s0 = s0, s1 = s1, p0 = p0, p1 = p1 });
        }

        isTrialInProgress = false;
        Debug.Log($"[DataLogger] 試行を終了しました。({committedTrials.Count} 試行完了)");
    }

    /// <summary>
    /// 現在の試行を一時停止し、時計とStream記録がポーズされる。
    /// </summary>
    public void PauseTrial()
    {
        if (!isTrialInProgress) { return; }

        // Stream と Clock の両方をポーズ
        PauseStream();
        clock.Pause();

        Debug.Log("[DataLogger] 試行をポーズしました。");
    }

    /// <summary>
    /// 一時停止中の試行を再開し、時計とStream記録も再開。
    /// </summary>
    public void ResumeTrial()
    {
        if (!isTrialInProgress) { return; }

        // Stream と Clock の両方を再開
        ResumeStream();
        clock.Resume();

        Debug.Log("[DataLogger] 試行を再開しました。");
    }

    /// <summary>
    /// 現在の試行を破棄し、この試行中に行った記録はすべてキャンセル。
    /// </summary>
    public void AbortTrial()
    {
        if (!isTrialInProgress) { return; }

        streamLogger.StopStream();
        clock.Stop();

        // ブックマーク地点までキャッシュを巻き戻す
        streamLogger.TruncateFrom(streamBookmark);
        snapshotLogger.TruncateFrom(snapshotBookmark);

        isTrialInProgress = false;
        Debug.Log("[DataLogger] 現在の試行を破棄しました。");
    }

    // ===== API: 記録制御 =====

    /// <summary>
    /// Stream（連続）記録を開始。
    /// StartTrial() を呼び出した後である必要あり。
    /// </summary>
    public void StartStream()
    {
        if (!isTrialInProgress)
        {
            Debug.LogWarning("試行が開始されていません。");
            return;
        }

        streamLogger.StartStream();
    }

    /// <summary>
    /// Stream（連続）記録を停止（試行は継続）。
    /// </summary>
    public void StopStream()
    {
        if (!isTrialInProgress)
        {
            Debug.LogWarning("試行が開始されていません。");
            return;
        }

        streamLogger.StopStream();
    }

    /// <summary>
    /// Stream（連続）記録を一時停止（試行は継続）。
    /// </summary>
    public void PauseStream()
    {
        if (!isTrialInProgress)
        {
            Debug.LogWarning("試行が開始されていません。");
            return;
        }

        streamLogger.PauseStream();
    }

    /// <summary>
    /// Stream（連続）記録を再開。
    /// StartTrial() を呼び出した後である必要あり。
    /// </summary>
    public void ResumeStream()
    {
        if (!isTrialInProgress)
        {
            Debug.LogWarning("試行が開始されていません。");
            return;
        }

        streamLogger.ResumeStream();
    }

    /// <summary>
    /// 現在の全 Snapshot メンバーの値を記録。
    /// StartTrial() を呼び出した後である必要あり。
    /// </summary>
    public void CaptureSnapshot()
    {
        if (!isTrialInProgress)
        {
            Debug.LogWarning("試行が開始されていないため、Snapshotを記録できません。");
            return;
        }

        snapshotLogger.PrepareHeader();
        snapshotLogger.Capture(clock.ElapsedSeconds, clock.ElapsedFrames);
    }

    // ===== API: 収集・出力・初期化 =====

    /// <summary>
    /// 現在のシーンをスキャンし、ログ対象メンバーのリストを更新。
    /// インスペクタでの並び順上書きも適用される。
    /// </summary>
    public void RefreshRegistry()
    {
        registry.Refresh();
        ApplyOrderOverrides(registry.StreamItems, streamOrderOverrides);
        ApplyOrderOverrides(registry.SnapshotItems, snapshotOrderOverrides);
        registry.Resort();
    }

    /// <summary>
    /// ログ対象メンバーを動的に登録。
    /// </summary>
    public void RegisterMember(Component target, string memberName)
    {
        registry.RegisterMember(target, memberName);
    }

    /// <summary>
    /// ログ対象メンバーを動的に解除。
    /// </summary>
    public void UnregisterMember(Component target, string memberName)
    {
        registry.UnregisterMember(target, memberName);
    }

    /// <summary>
    /// 記録されたログをCSVファイルとしてエクスポート。
    /// 確定した試行 (committedTrials) があれば出力する。
    /// </summary>
    public void Export()
    {
        if (isTrialInProgress)
        {
            Debug.LogWarning("[DataLogger] 試行が進行中です。エクスポート前に EndTrial() または AbortTrial() を呼んでください。");
            return;
        }

        if (committedTrials.Count > 0)
        {
            var trialsData = committedTrials.ConvertAll(t => (t.s0, t.s1, t.p0, t.p1));

            DataExporter.Export(
                this,
                streamCache,
                snapshotCache,
                trialsData,
                () => Debug.Log("[DataLogger] エクスポート処理が完了しました。")
            );
        }
        else
        {
            Debug.LogWarning("[DataLogger] エクスポート対象となるコミット済みの試行がありません。");
        }
    }

    /// <summary>
    /// 内部キャッシュと試行履歴をすべてクリア。
    /// </summary>
    public void Initialize()
    {
        if (isTrialInProgress)
        {
            AbortTrial();
        }

        streamCache.Clear();
        snapshotCache.Clear();
        committedTrials.Clear();
        streamBookmark = 0;
        snapshotBookmark = 0;
        clock.Reset();
    }

    // ===== 内部処理 =====

    /// <summary>
    /// インスペクタで設定された並び順上書きを適用。
    /// </summary>
    private static void ApplyOrderOverrides(IReadOnlyList<LogItem> items, List<LogItemOrderOverride> overrides)
    {
        if (items == null || overrides == null || overrides.Count == 0) { return; }

        var map = new Dictionary<string, int>(overrides.Count);
        foreach (var o in overrides)
        {
            if (o != null && !string.IsNullOrEmpty(o.stableKey))
            {
                map[o.stableKey] = o.order;
            }
        }

        if (map.Count == 0) { return; }

        foreach (var it in items)
        {
            if (it != null && !string.IsNullOrEmpty(it.stableKey))
            {
                if (map.TryGetValue(it.stableKey, out int ord))
                {
                    it.order = ord;
                }
            }
        }
    }

    /// <summary>
    /// 試行範囲（end は排他的）
    /// </summary>
    private struct TrialRange
    {
        public int s0, s1, p0, p1;
    }
}