using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ログエクスポートの公開APIを提供するエントリポイント。
/// 実際の書き出し処理はプラットフォーム（PC/WebGL）別のクラスが担当。
/// </summary>
public static class DataExporter
{
    // ファイル名と、ファイルの中身(byte[])を保持するための構造体
    public struct FileInMemory
    {
        public string Path; // WebGLではファイル名、PCでは保存先相対パスとして使用
        public byte[] Content;
    }

    /// <summary>
    /// ログのエクスポートを開始する。
    /// 実行環境に応じて、Task(PC, Android, iOS, Mac, Linux などの標準プラットフォーム) または Coroutine(WebGL) に処理を委譲する。
    /// </summary>
    public static void Export(
        MonoBehaviour runner,
        LogItemCache streamCache, LogItemCache snapshotCache,
        IReadOnlyList<(int s0, int s1, int p0, int p1)> trials,
        System.Action onComplete = null)
    {
        string timestamp = GetTimestamp();
        string baseDirName = $"log_{timestamp}";

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: メインスレッドでの時分割処理 (Coroutine) に委譲
        DataExporterWebGL.Export(runner, streamCache, snapshotCache, trials, baseDirName, onComplete);
#else
        // PC, Android, iOS, Mac, Linux などの標準プラットフォーム: 別スレッドでの処理 (Task) に委譲
        DataExporterDefault.Export(streamCache, snapshotCache, trials, baseDirName, onComplete);
#endif
    }

    private static string GetTimestamp()
    {
        return System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }
}