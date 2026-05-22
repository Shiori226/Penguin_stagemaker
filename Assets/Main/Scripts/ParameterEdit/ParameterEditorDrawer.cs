using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ParameterEditorDrawer : MonoBehaviour
{
    [SerializeField]
    private RectTransform tabsParent;
    [SerializeField]
    private GameObject tabPrefab;

    [SerializeField]
    private RectTransform editorsParent;
    [SerializeField]
    private GameObject editorPrefab;

    private ToggleGroup toggleGroup;

    private void Start()
    {
        // ToggleGroup がアタッチされていなければ追加
        toggleGroup = tabsParent.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
        {
            toggleGroup = tabsParent.gameObject.AddComponent<ToggleGroup>();
        }

        // 起動時にUIを構築
        DrawEditor();
    }

    public void DrawEditor()
    {
        // 既存のUI（タブとエディタ）をすべて破棄
        foreach (Transform child in tabsParent) { Destroy(child.gameObject); }
        foreach (Transform child in editorsParent) { Destroy(child.gameObject); }

        // ToggleGroup の状態をリセット
        if (toggleGroup != null)
        {
            toggleGroup.allowSwitchOff = true;
            toggleGroup.SetAllTogglesOff();
            toggleGroup.allowSwitchOff = false;
        }

        bool firstToggle = true;

        DataManager targetInstance = DataManager.Instance;
        if (targetInstance == null) { return; }

        Type type = targetInstance.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // IDictionaryを実装しているフィールドのみ対象
            if (typeof(IDictionary).IsAssignableFrom(field.FieldType))
            {
                // 1. 辞書の実体を取得
                object fieldValue = field.GetValue(targetInstance);
                if (fieldValue == null) { continue; }

                // 2. タイトル用の型名を取得 (Dictionary<string, ScoreData> -> "ScoreData")
                Type[] genericArgs = field.FieldType.GetGenericArguments();
                // Value側の型名を採用する (Key, Value の順なので index 1)
                string displayLabel = (genericArgs.Length > 1) ? genericArgs[1].Name : field.Name;
                displayLabel = StringCaseUtility.ToSpacedWords(displayLabel);

                // タブ生成
                GameObject tab = Instantiate(tabPrefab, tabsParent);
                if (tab.TryGetComponent<ParameterTabSetter>(out var tabSetter)) { tabSetter.SetLabelText(displayLabel); }

                // エディタ生成
                GameObject editor = Instantiate(editorPrefab, editorsParent);
                ParameterUIBuilder builder = editor.GetComponent<ParameterUIBuilder>();

                // タイトルと実体を渡して描画開始
                builder.RedrawAll(displayLabel, fieldValue);

                // トグル制御
                Toggle toggle = tab.GetComponent<Toggle>();
                toggle.group = toggleGroup;

                // クロージャ問題回避のためのローカル変数
                GameObject editorRef = editor;
                toggle.onValueChanged.AddListener(isOn => editorRef.SetActive(isOn));

                editor.SetActive(false);

                if (firstToggle)
                {
                    toggle.isOn = true;
                    editor.SetActive(true);
                    firstToggle = false;
                }
            }
        }
    }
}