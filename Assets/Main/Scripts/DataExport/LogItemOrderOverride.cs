using System;

/// <summary>
/// DataLogger のインスペクタで、特定のログ項目の並び順を
/// デフォルト値から上書きするためのシリアライズ可能なクラス。
/// </summary>
[Serializable]
public sealed class LogItemOrderOverride
{
    // 安定キー（型フル名.メンバー名）
    public string stableKey;

    // 上書きする order 値（小さいほど左／上）
    public int order;
}