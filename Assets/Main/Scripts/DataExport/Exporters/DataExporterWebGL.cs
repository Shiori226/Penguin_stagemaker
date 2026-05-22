#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using StageMaker;
using UnityEngine;

/// <summary>
/// WebGL�p�̃G�N�X�|�[�g�����B
/// �R���[�`�����g�p���ă��C���X���b�h�Ŏ������������s���A�u���E�U�̃t���[�Y���y������B
/// </summary>
public static class DataExporterWebGL
{
    // jslib�v���O�C�����̊֐���`
    [DllImport("__Internal")]
    private static extern void DownloadZip(string zipName, string paths, byte[] contents, int[] sizes, int count);

    public static void Export(
        MonoBehaviour runner,
        LogItemCache streamCache, LogItemCache snapshotCache,
        IReadOnlyList<(int s0, int s1, int p0, int p1)> trials,
        string baseDirName, Action onComplete)
    {
        runner.StartCoroutine(ExportRoutine(streamCache, snapshotCache, trials, baseDirName, onComplete));
    }

    private static IEnumerator ExportRoutine(
        LogItemCache streamCache, LogItemCache snapshotCache,
        IReadOnlyList<(int s0, int s1, int p0, int p1)> trials,
        string baseDirName, Action onComplete)
    {
        var files = new List<DataExporter.FileInMemory>();
        var allSnapshotRows = new List<object[]>();
        
        var rawSnapshotHeader = new List<string> { "trial_index" };
        if (snapshotCache.Header != null) { rawSnapshotHeader.AddRange(snapshotCache.Header); }

        // ���s���Ƃ̃��[�v
        for (int i = 0; i < trials.Count; i++)
        {
            var (s0, s1, p0, p1) = trials[i];
            int trialId = i + 1;

            // Stream�f�[�^����
            if (s1 > s0)
            {
                // ���X�g����͓����I�ɍs���i���׌y���̂��߂����͈ꊇ�j
                var (header, rows) = CsvGenerator.SliceCache(streamCache, s0, s1);
                if (rows.Count > 0)
                {
                    // �ł��d��CSV�����񐶐��̂݃R���[�`���ŕ��U����
                    byte[] content = null;
                    yield return CreateCSVContentRoutine(header, rows, result => content = result);
                    
                    string path = $"{baseDirName}/stream_trial{trialId}.csv";
                    files.Add(new DataExporter.FileInMemory { Path = path, Content = content });
                }
            }

            // Snapshot�f�[�^�~��
            if (p1 > p0)
            {
                CsvGenerator.AccumulateSnapshotRows(snapshotCache, p0, p1, trialId, allSnapshotRows);
            }

            // ���s���Ƃ�1�t���[���x�e
            yield return null;
        }

        // Snapshot�����o��
        if (allSnapshotRows.Count > 0)
        {
            var (finalHeader, finalRows) = CsvGenerator.FilterActiveColumns(rawSnapshotHeader, allSnapshotRows);
            
            byte[] content = null;
            yield return CreateCSVContentRoutine(finalHeader, finalRows, result => content = result);

            string path = $"{baseDirName}/snapshot.csv";
            files.Add(new DataExporter.FileInMemory { Path = path, Content = content });
        }

        // ステージ情報 (stage.json) を併出力
        try
        {
            byte[] stageJson = StageInfoExporter.BuildJsonBytes();
            files.Add(new DataExporter.FileInMemory
            {
                Path = $"{baseDirName}/{StageInfoExporter.FileName}",
                Content = stageJson,
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DataExporterWebGL] stage.json export failed: {ex.Message}");
        }

        // JS呼び出しでZipダウンロード
        ProcessFiles(files, $"{baseDirName}.zip");
        
        onComplete?.Invoke();
    }

    /// <summary>
    /// LogCsvGenerator.CreateCSVContent �̃R���[�`���ŁB
    /// ��ʂ̍s����������ہA����I�� yield return null ������Ńt���[�Y��h���B
    /// </summary>
    private static IEnumerator CreateCSVContentRoutine(List<string> header, List<object[]> rows, Action<byte[]> onResult)
    {
        var sb = new StringBuilder();

        // �w�b�_
        var headerCells = header.Select(h => CsvUtility.EscapeCSV(h)).ToList();
        sb.AppendLine(CsvUtility.JoinRow(headerCells));

        // �f�[�^
        int count = 0;
        var rowBuffer = new List<string>(header.Count);

        foreach (var row in rows)
        {
            rowBuffer.Clear();
            int rowLen = row.Length;
            for (int i = 0; i < rowLen && i < header.Count; i++)
            {
                rowBuffer.Add(CsvUtility.ToEscapedCell(row[i]));
            }
            for (int i = rowLen; i < header.Count; i++)
            {
                rowBuffer.Add(string.Empty);
            }
            sb.AppendLine(CsvUtility.JoinRow(rowBuffer));

            count++;
            // 500�s���Ƃ�1�t���[���x�e�i���ɍ��킹�Ē����j
            if (count % 500 == 0)
            {
                yield return null;
            }
        }

        onResult?.Invoke(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray());
    }

    private static void ProcessFiles(List<DataExporter.FileInMemory> files, string zipFileName)
    {
        if (files.Count == 0) { return; }

        var paths = files.Select(f => f.Path.Replace(Path.DirectorySeparatorChar, '/')).ToList();
        var contents = new List<byte>();
        var sizes = new List<int>();

        foreach (var f in files)
        {
            contents.AddRange(f.Content);
            sizes.Add(f.Content.Length);
        }

        DownloadZip(zipFileName, string.Join("|", paths), contents.ToArray(), sizes.ToArray(), files.Count);
    }
}
#endif