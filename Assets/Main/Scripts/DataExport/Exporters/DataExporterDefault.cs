#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StageMaker;
using UnityEngine;

/// <summary>
/// �W���I�ȃv���b�g�t�H�[���iPC/Mobile/Console�j�p�̃G�N�X�|�[�g�����B
/// Task.Run ���g�p���ĕʃX���b�h�ŏ������s���A���C���X���b�h�̃t���[�Y���������B
/// </summary>
public static class DataExporterDefault
{
    public static void Export(
        LogItemCache streamCache, LogItemCache snapshotCache,
        IReadOnlyList<(int s0, int s1, int p0, int p1)> trials,
        string baseDirName, Action onComplete)
    {
        // �ۑ���p�X�̎擾�̓��C���X���b�h�ōs���K�v������
        string savePathBase = Application.persistentDataPath;

        // �d��������ʃX���b�h�Ŏ��s
        Task.Run(() =>
        {
            try
            {
                ExportLogic(streamCache, snapshotCache, trials, baseDirName, savePathBase);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataExporterDefault] Error: {ex}");
            }
        }).ContinueWith(t =>
        {
            // ����������A���C���X���b�h�ŃR�[���o�b�N�����s
            onComplete?.Invoke();
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static void ExportLogic(
        LogItemCache streamCache, LogItemCache snapshotCache,
        IReadOnlyList<(int s0, int s1, int p0, int p1)> trials,
        string baseDirName, string savePathBase)
    {
        var files = new List<DataExporter.FileInMemory>();
        var allSnapshotRows = new List<object[]>();

        // Snapshot�w�b�_����
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
                var (header, rows) = CsvGenerator.SliceCache(streamCache, s0, s1);
                if (rows.Count > 0)
                {
                    byte[] content = CsvGenerator.CreateCSVContent(header, rows);
                    string path = $"{baseDirName}/stream_trial{trialId}.csv";
                    files.Add(new DataExporter.FileInMemory { Path = path, Content = content });
                }
            }

            // Snapshot�f�[�^�~��
            if (p1 > p0)
            {
                CsvGenerator.AccumulateSnapshotRows(snapshotCache, p0, p1, trialId, allSnapshotRows);
            }
        }

        // �~�ς���Snapshot���ꊇ�o��
        if (allSnapshotRows.Count > 0)
        {
            var (finalHeader, finalRows) = CsvGenerator.FilterActiveColumns(rawSnapshotHeader, allSnapshotRows);
            byte[] content = CsvGenerator.CreateCSVContent(finalHeader, finalRows);
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
            Debug.LogWarning($"[DataExporterDefault] stage.json export failed: {ex.Message}");
        }

        // ファイル書き出し (IO処理)
        foreach (var file in files)
        {
            string fullPath = Path.Combine(savePathBase, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, file.Content);
        }

        Debug.Log($"[DataExporterDefault] Export complete: {Path.Combine(savePathBase, baseDirName)}");
    }
}
#endif