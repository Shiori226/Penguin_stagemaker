using System.Collections.Generic;

/// <summary>
/// プレイイベントを試行をまたいで保持するメモリキャッシュ。
/// DataLogger が試行ごとの範囲を管理するため、イベント本体には試行番号を持たせない。
/// </summary>
public sealed class PlayLogEventCache
{
    public List<PlayLogEvent> Events { get; } = new();

    /// <summary>
    /// 0以下なら無制限。長時間計測時のメモリ保護用。
    /// </summary>
    public int MaxEvents { get; set; }

    public int Count => Events.Count;

    public void Add(PlayLogEvent item)
    {
        if (item == null)
        {
            return;
        }

        Events.Add(item);

        if (MaxEvents > 0 && Events.Count > MaxEvents)
        {
            int over = Events.Count - MaxEvents;
            Events.RemoveRange(0, over);
        }
    }

    public void TruncateFrom(int startIndex)
    {
        if (startIndex < 0)
        {
            startIndex = 0;
        }
        if (startIndex >= Events.Count)
        {
            return;
        }

        Events.RemoveRange(startIndex, Events.Count - startIndex);
    }

    public void Clear()
    {
        Events.Clear();
    }
}
