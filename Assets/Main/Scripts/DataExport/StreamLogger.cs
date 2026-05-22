using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ストリーム（毎フレーム）のロギングを管理するクラス。
/// DataRegistry と LogItemCache と連携。
/// </summary>
public sealed class StreamLogger
{
    public int RowCount => streamCache?.Rows.Count ?? 0;

    private readonly DataRegistry dataRegistry;
    private readonly LogItemCache streamCache;

    private bool isStreaming; //「Stream記録中か」を管理する
    private bool isPaused; //「Stream記録がポーズ中か」を管理する

    // ヘッダ列名 -> getter（time/frameは除く）
    private readonly Dictionary<string, Func<object>> getterByHeader = new();

    // ベースラベルごとの採番カウンタ（#1省略／#2以降付与）※試行ごとにリセット
    private readonly Dictionary<string, int> baseCounts = new();

    // 「この登録(LogItem)に割り当てた列名」を覚える（解除時に正確に外す）
    private readonly Dictionary<LogItem, string> activeHeaderByItem = new();

    public StreamLogger(DataRegistry registry, LogItemCache cache)
    {
        dataRegistry = registry;
        streamCache = cache;
    }

    // ===== 公開API (DataLogger から参照) =====

    /// <summary>
    /// 指定したインデックス以降の行データをすべて削除。
    /// </summary>
    public void TruncateFrom(int start)
    {
        streamCache?.TruncateFrom(start);
    }

    /// <summary>
    /// DataRegistry のイベント購読を開始。
    /// </summary>
    public void Subscribe(DataRegistry reg)
    {
        if (reg == null)
        {
            return;
        }
        reg.StreamMemberRegistered += OnRegistered;
        reg.StreamMemberUnregistered += OnUnregistered;
    }

    /// <summary>
    /// DataRegistry のイベント購読を停止。
    /// </summary>
    public void Unsubscribe(DataRegistry reg)
    {
        if (reg == null)
        {
            return;
        }
        reg.StreamMemberRegistered -= OnRegistered;
        reg.StreamMemberUnregistered -= OnUnregistered;
    }

    /// <summary>
    /// Stream記録を開始。
    /// </summary>
    public void StartStream()
    {
        if (isStreaming || isPaused)
        {
            return;
        }

        // ヘッダがなければ準備（フォールバック）
        PrepareHeader();

        isStreaming = true;
        Debug.Log("[StreamLogger] Stream記録を開始しました。");
    }

    /// <summary>
    /// Stream記録を終了。
    /// </summary>
    public void StopStream()
    {
        if (!isStreaming)
        {
            return;
        }

        isStreaming = false;
        Debug.Log("[StreamLogger] Stream記録を終了しました。");
    }

    /// <summary>
    /// Stream記録を一時停止。
    /// </summary>
    public void PauseStream()
    {
        if (!isStreaming || isPaused)
        {
            return;
        }

        isPaused = true;
        Debug.Log("[StreamLogger] Stream記録をポーズしました。");
    }

    /// <summary>
    /// Stream記録を再開。
    /// </summary>
    public void ResumeStream()
    {
        if (!isStreaming || !isPaused)
        {
            return;
        }

        // ヘッダがなければ準備（フォールバック）
        PrepareHeader();

        isPaused = false;
        Debug.Log("[StreamLogger] Stream記録を再開しました。");
    }

    /// <summary>
    /// 新しい試行のために内部状態をリセット。
    /// 採番カウンタとバインドを初期化し、現時点の登録済み項目を新しい列として追加。
    /// </summary>
    public void ResetForNewTrial()
    {
        if (dataRegistry == null)
        {
            return;
        }

        // 採番カウンタと、現在アクティブな getter の紐付けをすべてクリア
        baseCounts.Clear();
        activeHeaderByItem.Clear();
        getterByHeader.Clear();

        streamCache.SetHeader(new List<string> { "time", "frame" });

        // 現時点で DataRegistry に存在するすべてのメンバーを、新しい列として再割り当てする
        foreach (var it in dataRegistry.StreamItems)
        {
            if (it == null || string.IsNullOrEmpty(it.label) || it.getter == null)
            {
                continue;
            }
            AttachNewColumn(it); // AddColumnは "time", "frame" があっても重複追加しない
        }
    }

    /// <summary>
    /// Streamデータを1フレーム分記録（毎LateUpdateで呼ばれる想定）。
    /// </summary>
    /// <param name="time">記録時間 (Clock.ElapsedSeconds)</param>
    /// <param name="frameCount">記録フレーム (Clock.ElapsedFrames)</param>
    public void TickStream(double time, int frameCount)
    {
        if (!isStreaming || isPaused)
        {
            return;
        }

        var header = streamCache.Header;
        if (header == null || header.Count < 2)
        {
            // 通常は DataLogger 側で ResetForNewTrial 済みだが、
            // 直接 StartStream された場合のフォールバック
            PrepareHeader();
            header = streamCache.Header;
        }

        var cells = new List<object>(header?.Count ?? 2);
        cells.Add(time);
        cells.Add(frameCount);

        // ヘッダ順に従って値を埋める（time/frameを除く）
        if (header != null)
        {
            for (int i = 2; i < header.Count; i++)
            {
                string col = header[i];
                if (getterByHeader.TryGetValue(col, out var g))
                {
                    cells.Add(SafeGet(g));
                }
                else
                {
                    cells.Add(null); // 解除済みや未バインドは null
                }
            }
        }

        streamCache.AddRow(cells);
    }

    // ===== DataRegistry イベント処理 (private) =====

    private void OnRegistered(LogItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.label) || item.getter == null)
        {
            return;
        }
        // 常に新しい列として追加
        AttachNewColumn(item);
    }

    private void OnUnregistered(LogItem item)
    {
        if (item == null)
        {
            return;
        }
        // getter だけ外し、列自体は保持（過去行の整合のため）
        DetachColumn(item);
    }

    // ===== 内部：列の追加/解除・採番 =====

    /// <summary>
    /// （互換用）キャッシュにヘッダが存在しない場合のみ、
    /// time, frame を含むヘッダ生成と現在項目の列追加を行う。
    /// </summary>
    private void PrepareHeader()
    {
        if (streamCache.Header != null && streamCache.Header.Count > 0)
        {
            return;
        }
        // 初回は ResetForNewTrial と同じ処理でヘッダが（AddColumn側で）生成される
        ResetForNewTrial();
    }

    private void AttachNewColumn(LogItem item)
    {
        string baseLabel = item.label;

        // 採番（#1は省略）
        if (!baseCounts.TryGetValue(baseLabel, out int n))
        {
            n = 0;
        }
        n++;
        baseCounts[baseLabel] = n;
        string headerLabel = (n == 1) ? baseLabel : $"{baseLabel}#{n}";

        // LogItemCache.AddColumn は、ヘッダ未作成なら time,frame 付きヘッダを自動生成し、
        // 既存列がある場合は右端に追加（既存行は null パディング）する。
        streamCache.AddColumn(headerLabel);

        // getter をこの列に紐付け
        getterByHeader[headerLabel] = item.getter;

        // この「登録」がどの列に紐づいたか覚える（解除時に正しく外すため）
        activeHeaderByItem[item] = headerLabel;
    }

    private void DetachColumn(LogItem item)
    {
        // LogItem インスタンスをキーに、紐づく列名を取得
        if (activeHeaderByItem.TryGetValue(item, out var headerLabel))
        {
            getterByHeader.Remove(headerLabel); // 値の供給だけ止める
            activeHeaderByItem.Remove(item); // 辞書からこの LogItem を削除
        }
        // 列は残す：過去のキャプチャ行の整合性維持のため
    }

    private static object SafeGet(Func<object> getter)
    {
        try
        {
            return getter?.Invoke();
        }
        catch (Exception ex)
        {
            // オブジェクトが破棄された場合など
            Debug.LogWarning($"[StreamLogger] getter の実行に失敗しました: {ex.Message}");
            return null;
        }
    }
}