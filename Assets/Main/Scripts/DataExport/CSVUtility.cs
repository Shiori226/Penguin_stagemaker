using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// CSV出力における値の整形とエスケープ処理に特化したユーティリティ。
/// - 値→セル文字列の整形（CultureInfo.InvariantCulture）
/// - カンマ/改行/ダブルクォートのエスケープ
/// - ベクトル/クォータニオン/Color の簡易シリアライズ
/// </summary>
public static class CsvUtility
{
    /// <summary>
    /// 1行分のセルをカンマ区切りで結合して返す（各セルは EscapeCSV 済みを想定）。
    /// </summary>
    public static string JoinRow(IReadOnlyList<string> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(",", cells);
    }

    /// <summary>
    /// 値を CSV セルの文字列に変換する（InvariantCulture）。
    /// ベクトル/クオータニオン/Color は ';' 区切りで1セルにまとめる。
    /// null は空セル。
    /// </summary>
    public static string ToCell(object value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        switch (value)
        {
            case Vector2 v2:
                return $"{F(v2.x)};{F(v2.y)}";
            case Vector3 v3:
                return $"{F(v3.x)};{F(v3.y)};{F(v3.z)}";
            case Vector4 v4:
                return $"{F(v4.x)};{F(v4.y)};{F(v4.z)};{F(v4.w)}";
            case Quaternion q:
                return $"{F(q.x)};{F(q.y)};{F(q.z)};{F(q.w)}";
            case Color c:
                return $"{F(c.r)};{F(c.g)};{F(c.b)};{F(c.a)}";
            case bool b:
                return b ? "true" : "false";
            case IFormattable f:
                return f.ToString(null, CultureInfo.InvariantCulture);
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// CSVセルのエスケープ。カンマ/改行/ダブルクォートを含む場合は "..." で括り、内部の " は "" にする。
    /// </summary>
    public static string EscapeCSV(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        bool needQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        if (!needQuote)
        {
            return s;
        }

        return $"\"{s.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// 値のセル文字列への変換とエスケープを同時に行います。
    /// </summary>
    public static string ToEscapedCell(object value)
    {
        return EscapeCSV(ToCell(value));
    }

    // ===== 内部ヘルパー =====

    /// <summary>
    /// InvariantCulture で float を文字列化するショートカット。
    /// </summary>
    private static string F(float x)
    {
        return x.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// InvariantCulture で double を文字列化するショートカット。
    /// </summary>
    private static string F(double x)
    {
        return x.ToString(CultureInfo.InvariantCulture);
    }
}