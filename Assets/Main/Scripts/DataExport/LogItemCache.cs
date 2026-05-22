using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ログのヘッダ＋行データをメモリに保持する簡易キャッシュ。
/// Stream/Snapshot の両方で使う。行は object[] として保持。
/// </summary>
public sealed class LogItemCache
{
    // 現在有効なヘッダ（null可）
    public List<string> Header { get; private set; }

    // 行データ（各行はヘッダ列数と同じ長さの object[] を想定）
    public List<object[]> Rows { get; } = new();

    // 上限（0以下で無制限）。メモリ保護用。例: 0=無制限, 100000=10万行で頭から捨てる
    public int MaxRows { get; set; } = 0;

    /// <summary>
    /// ヘッダを設定（または上書き）。
    /// </summary>
    public void SetHeader(List<string> header)
    {
        Header = header != null ? new List<string>(header) : null;
    }

    /// <summary>
    /// 1行分のデータをキャッシュに追加。
    /// 内部で配列にコピーされる。
    /// </summary>
    public void AddRow(List<object> cells)
    {
        if (cells == null)
        {
            return;
        }

        // List<object> を object[] に変換して追加
        Rows.Add(cells.ToArray());

        // 上限を超えたら古い行から削除
        if (MaxRows > 0 && Rows.Count > MaxRows)
        {
            int over = Rows.Count - MaxRows;
            Rows.RemoveRange(0, over);
        }
    }

    /// <summary>
    /// ヘッダ末尾に新しい列を追加し、既存行を null で右側パディングする。
    /// 既に同名の列がある場合は何もしない。
    /// </summary>
    public void AddColumn(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        // ヘッダが未初期化なら、time/frame を持つ既定ヘッダを作る
        if (Header == null)
        {
            Header = new List<string> { "time", "frame" };
        }

        int index = Header.IndexOf(label);
        if (index >= 0)
        {
            return; // すでに存在
        }

        Header.Add(label);

        // 既存行を新しい列数に合わせて右側を null でパディング
        int cols = Header.Count;
        for (int r = 0; r < Rows.Count; r++)
        {
            object[] row = Rows[r];
            if (row.Length < cols)
            {
                var newRow = new object[cols];
                System.Array.Copy(row, newRow, row.Length);
                Rows[r] = newRow; // 新列は null のまま
            }
        }
    }

    /// <summary>
    /// 指定したインデックス以降の行データをすべて削除。
    /// </summary>
    /// <param name="startIndex">このインデックスから末尾までが削除対象</param>
    public void TruncateFrom(int startIndex)
    {
        if (startIndex < 0)
        {
            startIndex = 0;
        }
        if (startIndex >= Rows.Count)
        {
            return;
        }

        Rows.RemoveRange(startIndex, Rows.Count - startIndex);
    }

    /// <summary>
    /// キャッシュされているすべての行データとヘッダをクリア。
    /// </summary>
    public void Clear()
    {
        Rows.Clear();
        Header = null;
    }
}