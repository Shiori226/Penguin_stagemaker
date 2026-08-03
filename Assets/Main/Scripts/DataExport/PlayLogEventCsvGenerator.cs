using System.Collections.Generic;
using System.Text;

/// <summary>
/// プレイイベントを固定列のCSVへ変換する純粋なロジック。
/// イベント種別によって値を持たない列は空欄になる。
/// </summary>
public static class PlayLogEventCsvGenerator
{
    private static readonly List<string> Header = new()
    {
        "trial_index",
        "time",
        "frame",
        "sequence",
        "event_type",
        "actor_id",
        "target_id",
        "position_x",
        "position_y",
        "position_z",
        "velocity_before_x",
        "velocity_before_y",
        "velocity_before_z",
        "velocity_after_x",
        "velocity_after_y",
        "velocity_after_z",
        "normal_x",
        "normal_y",
        "normal_z",
        "input_name",
        "input_phase",
        "input_value",
        "item_name",
        "reason",
        "value",
    };

    public static byte[] CreateCSVContent(
        IReadOnlyList<PlayLogEvent> events,
        int start,
        int end,
        int trialIndex)
    {
        var rows = new List<object[]>();
        if (events != null)
        {
            start = Clamp(start, 0, events.Count);
            end = Clamp(end, start, events.Count);

            for (int i = start; i < end; i++)
            {
                rows.Add(ToRow(events[i], trialIndex));
            }
        }

        return CreateCSVContent(Header, rows);
    }

    private static object[] ToRow(PlayLogEvent item, int trialIndex)
    {
        var position = item.position;
        var velocityBefore = item.velocityBefore;
        var velocityAfter = item.velocityAfter;
        var normal = item.normal;

        return new object[]
        {
            trialIndex,
            item.time,
            item.frame,
            item.sequence,
            item.eventType,
            item.actorId,
            item.targetId,
            position?.x,
            position?.y,
            position?.z,
            velocityBefore?.x,
            velocityBefore?.y,
            velocityBefore?.z,
            velocityAfter?.x,
            velocityAfter?.y,
            velocityAfter?.z,
            normal?.x,
            normal?.y,
            normal?.z,
            item.inputName,
            item.inputPhase,
            item.inputValue,
            item.itemName,
            item.reason,
            item.value,
        };
    }

    private static byte[] CreateCSVContent(IReadOnlyList<string> header, IEnumerable<object[]> rows)
    {
        var sb = new StringBuilder();
        var headerCells = new List<string>(header.Count);
        foreach (string item in header)
        {
            headerCells.Add(CsvUtility.EscapeCSV(item));
        }
        sb.AppendLine(CsvUtility.JoinRow(headerCells));

        var rowCells = new List<string>(header.Count);
        foreach (object[] row in rows)
        {
            rowCells.Clear();
            for (int i = 0; i < header.Count; i++)
            {
                rowCells.Add(CsvUtility.ToEscapedCell(i < row.Length ? row[i] : null));
            }
            sb.AppendLine(CsvUtility.JoinRow(rowCells));
        }

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        System.Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        System.Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) { return min; }
        if (value > max) { return max; }
        return value;
    }
}
