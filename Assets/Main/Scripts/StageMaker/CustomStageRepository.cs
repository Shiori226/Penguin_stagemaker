using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CustomStageData を JSON ファイルとして persistentDataPath に保存・読み込みする。
    /// Export/Import は同じ JSON フォーマットを使う (CSV ログの stage.json とも同形式)。
    /// </summary>
    public static class CustomStageRepository
    {
        public const string FolderName = "CustomStages";
        public const string ExportFolderName = "StageMaker/Exports";
        public const string ImportFolderName = "StageMaker/Imports";
        public const string FileExtension = ".json";

        public static string GetFolderPath()
        {
            string path = Path.Combine(Application.persistentDataPath, FolderName);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        public static string GetExportFolderPath()
        {
            string path = Path.Combine(Application.persistentDataPath, ExportFolderName);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        public static string GetImportFolderPath()
        {
            string path = Path.Combine(Application.persistentDataPath, ImportFolderName);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        public static string GetFilePath(string stageId)
        {
            return Path.Combine(GetFolderPath(), stageId + FileExtension);
        }

        public static List<CustomStageData> LoadAll()
        {
            var result = new List<CustomStageData>();
            string folder = GetFolderPath();
            if (!Directory.Exists(folder)) { return result; }

            foreach (var file in Directory.GetFiles(folder, "*" + FileExtension))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = CustomStageData.FromJson(json);
                    if (data != null && !string.IsNullOrEmpty(data.id))
                    {
                        result.Add(data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CustomStageRepository] Failed to read {file}: {ex.Message}");
                }
            }

            result.Sort((a, b) => string.Compare(b.updatedAt, a.updatedAt, StringComparison.Ordinal));
            return result;
        }

        public static CustomStageData Load(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) { return null; }
            string path = GetFilePath(stageId);
            if (!File.Exists(path)) { return null; }
            try
            {
                return CustomStageData.FromJson(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomStageRepository] Load failed: {ex.Message}");
                return null;
            }
        }

        public static void Save(CustomStageData data)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                Debug.LogWarning("[CustomStageRepository] Cannot save data without id");
                return;
            }

            data.updatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string json = data.ToJson(prettyPrint: true);
            string path = GetFilePath(data.id);
            File.WriteAllText(path, json);
            Debug.Log($"[CustomStageRepository] Saved: {path}");
        }

        public static void Delete(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) { return; }
            string path = GetFilePath(stageId);
            if (File.Exists(path)) { File.Delete(path); }
        }

        // ========== Export / Import ==========

        /// <summary>
        /// 指定ステージを Exports フォルダに JSON として書き出す。
        /// 戻り値は出力先パス (失敗時は null)。
        /// </summary>
        public static string Export(CustomStageData data)
        {
            if (data == null) { return null; }
            string folder = GetExportFolderPath();
            string baseName = SanitizeFileName(data.displayName);
            if (string.IsNullOrEmpty(baseName)) { baseName = "stage"; }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{baseName}_{timestamp}{FileExtension}";
            string path = Path.Combine(folder, fileName);
            try
            {
                File.WriteAllText(path, data.ToJson(prettyPrint: true));
                Debug.Log($"[CustomStageRepository] Exported: {path}");
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomStageRepository] Export failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Imports フォルダ内の全 JSON を読み込んで通常の保存場所にコピーする。
        /// 戻り値は取り込めたステージ数。
        /// </summary>
        public static int ImportFromFolder()
        {
            string folder = GetImportFolderPath();
            if (!Directory.Exists(folder)) { return 0; }
            int imported = 0;
            foreach (var file in Directory.GetFiles(folder, "*" + FileExtension))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = CustomStageData.FromJson(json);
                    if (data == null || string.IsNullOrEmpty(data.id))
                    {
                        // ID が無い場合は新規発行する (CSV ログから取り込んだケース等)
                        if (data == null) { continue; }
                        data.id = Guid.NewGuid().ToString("N");
                    }
                    // 既存と衝突する場合は新しい id を割り当てる
                    if (File.Exists(GetFilePath(data.id)))
                    {
                        data.id = Guid.NewGuid().ToString("N");
                        data.displayName = (data.displayName ?? "Imported") + " (Imported)";
                    }
                    Save(data);
                    imported++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CustomStageRepository] Import skip {file}: {ex.Message}");
                }
            }
            return imported;
        }

        /// <summary>
        /// 指定パスの JSON を 1 件だけ読み込んで保存する (将来的なファイル選択ダイアログ用)。
        /// </summary>
        public static bool ImportFromFile(string filePath)
        {
            try
            {
                return ImportFromJson(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomStageRepository] ImportFromFile failed: {ex.Message}");
                return false;
            }
        }

        public static bool ImportFromJson(string json)
        {
            try
            {
                var data = CustomStageData.FromJson(json);
                if (data == null) { return false; }
                if (string.IsNullOrEmpty(data.id)) { data.id = Guid.NewGuid().ToString("N"); }
                if (File.Exists(GetFilePath(data.id)))
                {
                    data.id = Guid.NewGuid().ToString("N");
                    data.displayName = (data.displayName ?? "Imported") + " (Imported)";
                }
                Save(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomStageRepository] ImportFromJson failed: {ex.Message}");
                return false;
            }
        }

        public static void OpenFolderInOS(string path)
        {
            try
            {
                if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                Application.OpenURL("file://" + path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CustomStageRepository] OpenFolderInOS failed: {ex.Message}");
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }
            // 使えない文字を _ に置換
            return Regex.Replace(name, "[\\\\/:*?\"<>|]", "_");
        }
    }
}
