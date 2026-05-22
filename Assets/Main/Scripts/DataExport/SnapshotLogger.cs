using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スナップショット（任意のタイミング）のロギングを管理するクラス。
/// DataRegistry と LogItemCache と連携。
/// </summary>
public sealed class SnapshotLogger
{
    public LogItemCache Cache => snapshotCache;
    public int RowCount => snapshotCache?.Rows.Count ?? 0;

    private readonly DataRegistry dataRegistry;
    private readonly LogItemCache snapshotCache;

    // ヘッダ列名 -> getter（time/frameは除く）
    private readonly Dictionary<string, Func<object>> getterByHeader = new();

    // ベースラベルごとの採番カウンタ（#1省略／#2以降付与）※試行ごとにリセット
    private readonly Dictionary<string, int> baseCounts = new();

    // 「この登録(LogItem)に割り当てた列名」を覚える（解除時に正確に外す）
    private readonly Dictionary<LogItem, string> activeHeaderByItem = new();

    public SnapshotLogger(DataRegistry registry, LogItemCache cache)
    {
        dataRegistry = registry;
        snapshotCache = cache;
    }

    // ===== 公開API (DataLogger から参照) =====

    /// <summary>
    /// 指定したインデックス以降の行データをすべて削除。
    /// </summary>
    public void TruncateFrom(int start)
    {
        snapshotCache?.TruncateFrom(start);
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
        reg.SnapshotMemberRegistered += OnRegistered;
        reg.SnapshotMemberUnregistered += OnUnregistered;
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
        reg.SnapshotMemberRegistered -= OnRegistered;
        reg.SnapshotMemberUnregistered -= OnUnregistered;
    }

    /// <summary>
    /// 新しい試行のために内部状態をリセット。
    /// 採番カウンタとバインドを初期化し、現時点の登録済み項目を新しい列として追加。
    /// </summary>
    public void ResetForNewTrial()
    {
        baseCounts.Clear();
        getterByHeader.Clear();
        activeHeaderByItem.Clear();

        snapshotCache.SetHeader(new List<string> { "time", "frame" });

        foreach (var it in dataRegistry.SnapshotItems)
        {
            if (it == null || string.IsNullOrEmpty(it.label) || it.getter == null)
            {
                continue;
            }
            AttachNewColumn(it); // AddColumnは "time", "frame" があっても重複追加しない
        }
    }

    /// <summary>
    /// （互換用）キャッシュにヘッダが存在しない場合のみ、
    /// time, frame を含むヘッダ生成と現在項目の列追加を行う。
    /// </summary>
    public void PrepareHeader()
    {
        if (snapshotCache.Header != null && snapshotCache.Header.Count > 0)
        {
            return;
        }
        // 初回は ResetForNewTrial と同じ処理でヘッダが（AddColumn側で）生成される
        ResetForNewTrial();
    }

    /// <summary>
    /// その時点の全登録メンバーの値を1行分のデータとしてキャッシュに追加。
    /// </summary>
    /// <param name="time">記録時間 (Clock.ElapsedSeconds)</param>
    /// <param name="frameCount">記録フレーム (Clock.ElapsedFrames)</param>
    public void Capture(double time, int frameCount)
    {
        var header = snapshotCache.Header;
        if (header == null || header.Count == 0)
        {
            // 通常は DataLogger 側で ResetForNewTrial 済みだが、
            // 直接 Capture が呼ばれた場合のフォールバック
            PrepareHeader();
            header = snapshotCache.Header;
        }

        var cells = new List<object>(header?.Count ?? 2);
        cells.Add(time);
        cells.Add(frameCount);

        if (header != null)
        {
            // time, frame (index 0, 1) を除く
            for (int i = 2; i < header.Count; i++)
            {
                string col = header[i];
                if (getterByHeader.TryGetValue(col, out var g))
                {
                    cells.Add(SafeGet(g));
                }
                else
                {
                    cells.Add(null); // 未バインド列（解除済みなど）は null
                }
            }
        }

        snapshotCache.AddRow(cells);

        Debug.Log("[SnapshotLogger] Snapshotを記録しました。");
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
        snapshotCache.AddColumn(headerLabel);

        // getter バインド
        getterByHeader[headerLabel] = item.getter;

        // この登録(LogItem)がどの列に紐づいたかを覚える（解除時に正しく外す）
        activeHeaderByItem[item] = headerLabel;
    }

    private void DetachColumn(LogItem item)
    {
        // LogItem インスタンスをキーに、紐づく列名を取得
        if (activeHeaderByItem.TryGetValue(item, out var headerLabel))
        {
            getterByHeader.Remove(headerLabel); // 値供給だけ止める
            activeHeaderByItem.Remove(item); // 辞書からこの LogItem を削除
        }
        // 列自体は過去のキャプチャ行の整合性維持のため残す
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
            Debug.LogWarning($"[SnapshotLogger] getter の実行に失敗しました: {ex.Message}");
            return null;
        }
    }
}