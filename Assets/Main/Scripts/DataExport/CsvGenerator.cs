using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// ログデータ（LogItemCache）を加工し、CSV形式のバイト配列を生成する純粋なロジッククラス。
/// 特定のプラットフォームや保存処理に依存せず、データの変換のみを担当する。
/// </summary>
public static class CsvGenerator
{
    /// <summary>
    /// キャッシュから指定範囲 [start, end) の行を抽出し、
    /// その範囲内で「値が null でない列」のみを抽出（スライス）して返す。
    /// </summary>
    public static (List<string> header, List<object[]> rows) SliceCache(LogItemCache cache, int start, int end)
    {
        if (cache == null || cache.Header == null || start < 0 || end <= start || start >= cache.Rows.Count)
        {
            return (new List<string>(), new List<object[]>());
        }

        if (end > cache.Rows.Count) { end = cache.Rows.Count; }

        var slicedRows = cache.Rows.GetRange(start, end - start);
        if (slicedRows.Count == 0)
        {
            return (new List<string>(), new List<object[]>());
        }

        var originalHeader = cache.Header;
        int originalColCount = originalHeader.Count;

        // 1. アクティブ列（値を持つ列）の判定
        var isActiveColumn = new bool[originalColCount];
        if (originalColCount > 0) { isActiveColumn[0] = true; } // time
        if (originalColCount > 1) { isActiveColumn[1] = true; } // frame

        foreach (var row in slicedRows)
        {
            int rowLength = Mathf.Min(row.Length, originalColCount);
            for (int c = 2; c < rowLength; c++)
            {
                if (!isActiveColumn[c] && row[c] != null)
                {
                    isActiveColumn[c] = true;
                }
            }
        }

        // 2. 列マッピング作成
        var finalHeader = new List<string>();
        var oldToNewColMap = new int[originalColCount];
        int newColCount = 0;

        for (int i = 0; i < originalColCount; i++)
        {
            if (isActiveColumn[i])
            {
                finalHeader.Add(originalHeader[i]);
                oldToNewColMap[i] = newColCount;
                newColCount++;
            }
            else
            {
                oldToNewColMap[i] = -1;
            }
        }

        // 3. 行データの再構築
        var finalRows = new List<object[]>(slicedRows.Count);
        foreach (var originalRow in slicedRows)
        {
            var newRow = new object[newColCount];
            int rowLength = Mathf.Min(originalRow.Length, originalColCount);
            for (int i = 0; i < rowLength; i++)
            {
                int newIndex = oldToNewColMap[i];
                if (newIndex != -1)
                {
                    newRow[newIndex] = originalRow[i];
                }
            }
            finalRows.Add(newRow);
        }

        return (finalHeader, finalRows);
    }

    /// <summary>
    /// Snapshotデータを行バッファに蓄積する。先頭列に trialId を付与する。
    /// </summary>
    public static void AccumulateSnapshotRows(
        LogItemCache cache, int start, int end, int trialId,
        List<object[]> destination)
    {
        if (cache == null || cache.Rows.Count == 0) { return; }
        if (end > cache.Rows.Count) { end = cache.Rows.Count; }

        var sourceRows = cache.Rows;
        int sourceColCount = cache.Header?.Count ?? 0;

        for (int r = start; r < end; r++)
        {
            object[] srcRow = sourceRows[r];
            var newRow = new object[1 + sourceColCount];

            newRow[0] = trialId; // 先頭にTrial ID

            int copyLen = Mathf.Min(srcRow.Length, sourceColCount);
            if (copyLen > 0) { System.Array.Copy(srcRow, 0, newRow, 1, copyLen); }

            destination.Add(newRow);
        }
    }

    /// <summary>
    /// 蓄積された全Snapshotデータから、有効な値が入っている列のみを抽出して返す。
    /// </summary>
    public static (List<string>, List<object[]>) FilterActiveColumns(
        List<string> originalHeader, List<object[]> allRows)
    {
        int colCount = originalHeader.Count;
        if (colCount == 0 || allRows.Count == 0) { return (new List<string>(), new List<object[]>()); }

        // trial_index(0), time(1), frame(2) は常に維持
        bool[] isActiveColumn = new bool[colCount];
        if (colCount > 0) isActiveColumn[0] = true;
        if (colCount > 1) isActiveColumn[1] = true;
        if (colCount > 2) isActiveColumn[2] = true;

        foreach (var row in allRows)
        {
            for (int c = 3; c < row.Length; c++)
            {
                if (!isActiveColumn[c] && row[c] != null)
                {
                    isActiveColumn[c] = true;
                }
            }
        }

        var finalHeader = new List<string>();
        var oldToNewMap = new int[colCount];
        int newColCount = 0;

        for (int i = 0; i < colCount; i++)
        {
            if (isActiveColumn[i])
            {
                finalHeader.Add(originalHeader[i]);
                oldToNewMap[i] = newColCount;
                newColCount++;
            }
            else
            {
                oldToNewMap[i] = -1;
            }
        }

        var finalRows = new List<object[]>(allRows.Count);
        foreach (var srcRow in allRows)
        {
            var newRow = new object[newColCount];
            int len = Mathf.Min(srcRow.Length, colCount);
            for (int i = 0; i < len; i++)
            {
                int targetIdx = oldToNewMap[i];
                if (targetIdx != -1) { newRow[targetIdx] = srcRow[i]; }
            }
            finalRows.Add(newRow);
        }

        return (finalHeader, finalRows);
    }

    /// <summary>
    /// ヘッダと行データを受け取り、BOM付きUTF-8のCSVバイト配列を生成する。
    /// </summary>
    public static byte[] CreateCSVContent(IReadOnlyList<string> header, IEnumerable<object[]> rows)
    {
        var sb = new StringBuilder();

        // ヘッダ出力
        var headerCells = new List<string>(header.Count);
        foreach (var h in header) { headerCells.Add(CsvUtility.EscapeCSV(h)); }
        sb.AppendLine(CsvUtility.JoinRow(headerCells));

        // データ出力
        var rowBuffer = new List<string>(header.Count);
        foreach (object[] row in rows)
        {
            rowBuffer.Clear();
            int rowLength = row.Length;

            // 実データの処理
            for (int i = 0; i < rowLength; i++)
            {
                if (i >= header.Count) { break; }
                rowBuffer.Add(CsvUtility.ToEscapedCell(row[i]));
            }

            // 足りない列の埋め合わせ
            for (int i = rowLength; i < header.Count; i++) { rowBuffer.Add(string.Empty); }

            sb.AppendLine(CsvUtility.JoinRow(rowBuffer));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}