using System;

/// <summary>
/// クリア時など「その瞬間の値」を1行として記録したいメンバーに貼る属性。
/// 例: [SnapshotData] / [SnapshotData("TotalScore", Order = 10)]
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SnapshotDataAttribute : Attribute
{
    // CSV見出し（nullだとメンバー名）
    public string Label { get; }

    // CSV列の並び順を制御したい場合に使う任意の番号（小さいほど左側）
    public int Order { get; set; } = 0;

    public SnapshotDataAttribute(string label = null)
    {
        Label = label;
    }
}