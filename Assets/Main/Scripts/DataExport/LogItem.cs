using System;
using UnityEngine;

/// <summary>
/// 記録対象となるメンバー（フィールドまたはプロパティ）の情報を保持するデータクラス。
/// </summary>
[Serializable]
public class LogItem
{
    // 所属コンポーネント
    public Component target;

    // CSV見出し（nullならメンバー名）
    public string label;

    // 型名（表示用）
    public string typeName;

    // 並び順（小さいほど左）
    public int order;

    // 安定ソート用キー（型フル名.メンバー名）
    public string stableKey;

    // 値取得デリゲート（収集時に一度だけ作成）
    [NonSerialized] // getterはシリアライズ対象外
    public Func<object> getter;
}