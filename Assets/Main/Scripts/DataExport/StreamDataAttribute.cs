using System;

/// <summary>
/// 連続データ（軌跡・時系列）を記録したいメンバーに貼る属性。
/// 例: [StreamData] / [StreamData("Position", OnlyOnChange = true, VectorEps = 0.0050f)]
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class StreamDataAttribute : Attribute
{
    // CSV見出し（nullだとメンバー名）
    public string Label { get; }

    // 値が変わったフレームだけ出力したいとき true
    public bool OnlyOnChange { get; set; } = false;

    // CSV列の並び順を制御したい場合に使う任意の番号（小さいほど左側）
    public int Order { get; set; } = 0;

    // 小数の変化判定（ノイズ抑制用）
    public float FloatEps { get; set; } = 1e-4f;

    // ベクトル・回転の変化判定（ノイズ抑制用）
    public float VectorEps { get; set; } = 1e-4f;

    public StreamDataAttribute(string label = null)
    {
        Label = label;
    }
}