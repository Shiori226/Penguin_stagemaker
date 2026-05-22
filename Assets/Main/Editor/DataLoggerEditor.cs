#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(DataLogger))]
public sealed class DataLoggerEditor : Editor
{
    private DataLogger logger;

    // 表示用キャッシュ
    private List<LogItem> viewStream = new();
    private List<LogItem> viewSnapshot = new();

    // 並べ替えUI
    private ReorderableList streamList;
    private ReorderableList snapshotList;

    private SerializedProperty streamOverridesProp;
    private SerializedProperty snapshotOverridesProp;

    private const int RowHeight = 20;

    private void OnEnable()
    {
        logger = (DataLogger)target;

        streamOverridesProp = serializedObject.FindProperty("streamOrderOverrides");
        snapshotOverridesProp = serializedObject.FindProperty("snapshotOrderOverrides");

        DoRefreshAndRebuild();
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ===== Stream =====
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Stream Items (Drag to Reorder)", EditorStyles.boldLabel);
        streamList.DoLayoutList();

        // ===== Snapshot =====
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Snapshot Items (Drag to Reorder)", EditorStyles.boldLabel);
        snapshotList.DoLayoutList();

        // ===== Refresh ボタン =====
        EditorGUILayout.Space(8);
        if (GUILayout.Button(new GUIContent("Refresh", "シーン内を再スキャンして一覧を更新"), GUILayout.Height(22)))
        {
            DoRefreshAndRebuild();
        }

        EditorGUILayout.Space(8);

        serializedObject.ApplyModifiedProperties();
    }

    // ===== ビュー構築 =====

    private void BuildViews()
    {
        viewStream.Clear();
        viewSnapshot.Clear();

        if (logger.StreamItems != null) { viewStream.AddRange(logger.StreamItems); }
        if (logger.SnapshotItems != null) { viewSnapshot.AddRange(logger.SnapshotItems); }
    }

    private void BuildReorderableLists()
    {
        // Stream
        streamList = new ReorderableList(viewStream, typeof(LogItem), draggable: true, displayHeader: false, displayAddButton: false, displayRemoveButton: false);
        streamList.elementHeight = RowHeight;
        streamList.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index < 0 || index >= viewStream.Count) { return; }
            DrawRow(rect, viewStream[index]);
        };
        streamList.onReorderCallback = (list) => ApplyNewOrderToOverrides(viewStream, streamOverridesProp);

        // Snapshot
        snapshotList = new ReorderableList(viewSnapshot, typeof(LogItem), draggable: true, displayHeader: false, displayAddButton: false, displayRemoveButton: false);
        snapshotList.elementHeight = RowHeight;
        snapshotList.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index < 0 || index >= viewSnapshot.Count) { return; }
            DrawRow(rect, viewSnapshot[index]);
        };
        snapshotList.onReorderCallback = (list) => ApplyNewOrderToOverrides(viewSnapshot, snapshotOverridesProp);
    }

    private static void DrawRow(Rect rect, LogItem item)
    {
        float x = rect.x + 4;
        float w = rect.width - 8;
        string label = item?.label ?? "(null)";
        string type = item?.typeName ?? "-";
        string key = item?.stableKey ?? "-";
        EditorGUI.LabelField(new Rect(x, rect.y, w, rect.height),
            new GUIContent($"{label}    <{type}>    ({key})"));
    }

    // 並び替え結果 → Overrides へ反映 → Refresh
    private void ApplyNewOrderToOverrides(List<LogItem> view, SerializedProperty overridesProp)
    {
        Dictionary<string, int> mapIndex = new Dictionary<string, int>();
        for (int i = 0; i < view.Count; i++)
        {
            string key = view[i]?.stableKey;
            if (string.IsNullOrEmpty(key)) continue;
            mapIndex[key] = i;
        }

        for (int i = 0; i < overridesProp.arraySize; i++)
        {
            SerializedProperty elem = overridesProp.GetArrayElementAtIndex(i);
            SerializedProperty keyProp = elem.FindPropertyRelative("stableKey");
            SerializedProperty ordProp = elem.FindPropertyRelative("order");
            string key = keyProp.stringValue;

            if (mapIndex.TryGetValue(key, out int index))
            {
                ordProp.intValue = index;
                mapIndex.Remove(key);
            }
        }

        foreach (var kv in mapIndex)
        {
            int newIndex = overridesProp.arraySize;
            overridesProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty elem = overridesProp.GetArrayElementAtIndex(newIndex);
            elem.FindPropertyRelative("stableKey").stringValue = kv.Key;
            elem.FindPropertyRelative("order").intValue = kv.Value;
        }

        serializedObject.ApplyModifiedProperties();

        DoRefreshAndRebuild();
    }

    private void DoRefreshAndRebuild()
    {
        logger.RefreshRegistry();
        BuildViews();
        BuildReorderableLists();
        Repaint();
    }
}
#endif
