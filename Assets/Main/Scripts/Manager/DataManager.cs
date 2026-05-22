using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public readonly Dictionary<string, PlayerData> playerData = new Dictionary<string, PlayerData>();
    public readonly Dictionary<string, ScoreData> scoreData = new Dictionary<string, ScoreData>();
    public readonly Dictionary<string, GradeCategoryData> gradeCategoryData = new Dictionary<string, GradeCategoryData>();
    public readonly Dictionary<string, GradeData> gradeData = new Dictionary<string, GradeData>();
    public readonly Dictionary<string, TimerData> timerData = new Dictionary<string, TimerData>();

    private readonly Dictionary<string, string> playerDataFileMap = new Dictionary<string, string>();
    private readonly Dictionary<string, string> scoreDataFileMap = new Dictionary<string, string>();
    private readonly Dictionary<string, string> gradeCategoryDataFileMap = new Dictionary<string, string>();
    private readonly Dictionary<string, string> gradeDataFileMap = new Dictionary<string, string>();
    private readonly Dictionary<string, string> timerDataFileMap = new Dictionary<string, string>();

    private readonly string playerDataFolder = "Data/PlayerData";
    private readonly string scoreDataFolder = "Data/ScoreData";
    private readonly string gradeCategoryDataFolder = "Data/GradeCategoryData";
    private readonly string gradeDataFolder = "Data/GradeData";
    private readonly string timerDataFolder = "Data/TimerData";

    private readonly string playerDataResourcesFolder = "Data/PlayerData";
    private readonly string scoreDataResourcesFolder = "Data/ScoreData";
    private readonly string gradeCategoryDataResourcesFolder = "Data/GradeCategoryData";
    private readonly string gradeDataResourcesFolder = "Data/GradeData";
    private readonly string timerDataResourcesFolder = "Data/TimerData";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitData()
    {
        ApplyData(playerData, playerDataFileMap, playerDataFolder, playerDataResourcesFolder);
        ApplyData(scoreData, scoreDataFileMap, scoreDataFolder, scoreDataResourcesFolder);
        ApplyData(gradeCategoryData, gradeCategoryDataFileMap, gradeCategoryDataFolder, gradeCategoryDataResourcesFolder);
        ApplyData(gradeData, gradeDataFileMap, gradeDataFolder, gradeDataResourcesFolder);
        ApplyData(timerData, timerDataFileMap, timerDataFolder, timerDataResourcesFolder);
    }

    public void ExportData()
    {
        ExportDataToPersistentFolder(playerData, playerDataFileMap, playerDataFolder);
        ExportDataToPersistentFolder(scoreData, scoreDataFileMap, scoreDataFolder);
        ExportDataToPersistentFolder(gradeCategoryData, gradeCategoryDataFileMap, gradeCategoryDataFolder);
        ExportDataToPersistentFolder(gradeData, gradeDataFileMap, gradeDataFolder);
        ExportDataToPersistentFolder(timerData, timerDataFileMap, timerDataFolder);
    }

    private void ApplyData<T>(Dictionary<string, T> targetData, Dictionary<string, string> fileMap, string streamingFolderPath, string resourcesFolderPath) where T : class, new()
    {
        bool loadedFromStreamingAssets = TryApplyDataFromStreamingAssets(targetData, fileMap, streamingFolderPath);

        if (!loadedFromStreamingAssets)
        {
            Debug.LogWarning($"StreamingAssets の読み込みに失敗したため、Resources から読み込みます: {streamingFolderPath}");
            ApplyDataFromResources(targetData, fileMap, resourcesFolderPath);
        }
    }

    private bool TryApplyDataFromStreamingAssets<T>(Dictionary<string, T> targetData, Dictionary<string, string> fileMap, string folderPath) where T : class, new()
    {
        try
        {
            string folderFullPath = Path.Combine(Application.streamingAssetsPath, folderPath);
            string indexPath = Path.Combine(folderFullPath, "index.json");

            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"Index file not found at path: {indexPath}");
                return false;
            }

            string indexJson = File.ReadAllText(indexPath);
            List<string> files = JsonConvert.DeserializeObject<List<string>>(indexJson);

            if (files == null)
            {
                Debug.LogWarning($"Failed to parse index.json at path: {indexPath}");
                return false;
            }

            // 一時バッファにだけ読み込む
            Dictionary<string, T> tempTargetData = new Dictionary<string, T>();
            Dictionary<string, string> tempFileMap = new Dictionary<string, string>();

            foreach (var fileName in files)
            {
                string filePath = Path.Combine(folderFullPath, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"File not found: {filePath}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                var tempData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);

                if (tempData == null)
                {
                    Debug.LogWarning($"Failed to parse json file: {filePath}");
                    return false;
                }

                foreach (var (key, value) in tempData)
                {
                    if (tempTargetData.TryGetValue(key, out T existingObject))
                    {
                        using JsonReader reader = value.CreateReader();
                        JsonSerializer.CreateDefault().Populate(reader, existingObject);
                    }
                    else
                    {
                        T newObject = value.ToObject<T>();
                        tempTargetData[key] = newObject;
                    }

                    tempFileMap[key] = fileName;
                }
            }

            // 全部成功してから本体へ反映
            foreach (var (key, value) in tempTargetData)
            {
                if (targetData.TryGetValue(key, out T existingObject))
                {
                    string mergedJson = JsonConvert.SerializeObject(value);
                    JsonConvert.PopulateObject(mergedJson, existingObject);
                }
                else
                {
                    targetData[key] = value;
                }
            }

            foreach (var (key, value) in tempFileMap)
            {
                fileMap[key] = value;
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"StreamingAssets load failed: {folderPath}\n{e}");
            return false;
        }
    }

    private void ApplyDataFromResources<T>(Dictionary<string, T> targetData, Dictionary<string, string> fileMap, string folderPath) where T : class, new()
    {
        TextAsset indexAsset = Resources.Load<TextAsset>($"{folderPath}/index");

        if (indexAsset == null)
        {
            Debug.LogError($"Index file not found in Resources at path: {folderPath}/index");
            return;
        }

        List<string> files = JsonConvert.DeserializeObject<List<string>>(indexAsset.text);

        if (files == null)
        {
            Debug.LogError($"Failed to parse Resources index at path: {folderPath}/index");
            return;
        }

        foreach (var fileName in files)
        {
            string resourcePath = $"{folderPath}/{Path.GetFileNameWithoutExtension(fileName)}".Replace("\\", "/");
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset == null)
            {
                Debug.LogError($"File not found in Resources: {resourcePath}");
                return;
            }

            string json = jsonAsset.text;
            var tempData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);

            if (tempData == null)
            {
                Debug.LogError($"Failed to parse Resources json: {resourcePath}");
                return;
            }

            foreach (var (key, value) in tempData)
            {
                if (targetData.TryGetValue(key, out T existingObject))
                {
                    using JsonReader reader = value.CreateReader();
                    JsonSerializer.CreateDefault().Populate(reader, existingObject);
                }
                else
                {
                    T newObject = value.ToObject<T>();
                    targetData[key] = newObject;
                }

                fileMap[key] = fileName;
            }
        }
    }

    private void ExportDataToPersistentFolder<T>(Dictionary<string, T> targetData, Dictionary<string, string> fileMap, string folderPath)
    {
        string folderFullPath = Path.Combine(Application.persistentDataPath, folderPath);

        if (!Directory.Exists(folderFullPath)) { Directory.CreateDirectory(folderFullPath); }

        var fileGroups = targetData
            .Where(kvp => fileMap.ContainsKey(kvp.Key))
            .GroupBy(kvp => fileMap[kvp.Key]);

        List<string> indexFileList = new List<string>();

        foreach (var group in fileGroups)
        {
            string fileName = group.Key;
            indexFileList.Add(fileName);

            Dictionary<string, T> exportDict = new Dictionary<string, T>();
            foreach (var kvp in group)
            {
                exportDict.Add(kvp.Key, kvp.Value);
            }

            string json = JsonConvert.SerializeObject(exportDict, Formatting.Indented, new StringEnumConverter());
            string writePath = Path.Combine(folderFullPath, fileName);
            File.WriteAllText(writePath, json);
        }

        if (indexFileList.Count > 0)
        {
            string indexJson = JsonConvert.SerializeObject(indexFileList, Formatting.Indented);
            string indexPath = Path.Combine(folderFullPath, "index.json");
            File.WriteAllText(indexPath, indexJson);
        }
    }
}