using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParameterUIBuilder : MonoBehaviour
{
    [Header("UI Roots")]
    [SerializeField] private RectTransform contentRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject groupPrefab;
    [SerializeField] private GameObject inputPrefab;
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private GameObject colorPrefab;
    [SerializeField] private GameObject multiFloatPrefab;
    [SerializeField] private GameObject dropdownPrefab;
    [SerializeField] private GameObject unsupportedPrefab;
    [SerializeField] private GameObject nullPrefab;

    private const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Instance;

    // 型（Type）をキーに、対応するUI構築メソッドを保持する辞書
    private readonly Dictionary<Type, Action<ValueHandle, RectTransform>> typeBuilders
        = new Dictionary<Type, Action<ValueHandle, RectTransform>>();

    // 複数のfloatで構成される構造体(Struct)の定義を保持する辞書
    // Key: 型 (typeof(Vector3))
    // Value: プロパティ名（"x", "y", "z"など）のリスト
    private readonly Dictionary<Type, IReadOnlyList<string>> structDefinitions
        = new Dictionary<Type, IReadOnlyList<string>>();

    private void Awake()
    {
        // typeBuilders (プリミティブ型)
        typeBuilders[typeof(int)] = BuildNumberRow;
        typeBuilders[typeof(long)] = BuildNumberRow;
        typeBuilders[typeof(float)] = BuildNumberRow;
        typeBuilders[typeof(double)] = BuildNumberRow;
        typeBuilders[typeof(string)] = BuildStringRow;
        typeBuilders[typeof(bool)] = BuildBoolRow;
        typeBuilders[typeof(Color)] = BuildColorRow;
        typeBuilders[typeof(Color32)] = BuildColor32Row;

        // 汎用ビルダー(BuildStructRow)を typeBuilders に登録 （Vector3, Vector2, Rect... すべて同じメソッドを呼び出す｝
        // Quaternion は、UI/UX（ユーザー体験）の観点で、x,y,z,w よりも Euler(Vector3) で編集するほうが簡単なため除外
        Action<ValueHandle, RectTransform> structBuilder = BuildStructRow;
        typeBuilders[typeof(Vector3)] = structBuilder;
        typeBuilders[typeof(Vector2)] = structBuilder;
        typeBuilders[typeof(Rect)] = structBuilder;
        typeBuilders[typeof(Vector4)] = structBuilder;
        // （)

        // structDefinitions（メタデータ） ※ コンストラクタの引数順と一致する
        structDefinitions[typeof(Vector3)] = new[] { "x", "y", "z" };
        structDefinitions[typeof(Vector2)] = new[] { "x", "y" };
        structDefinitions[typeof(Rect)] = new[] { "x", "y", "width", "height" };
        structDefinitions[typeof(Vector4)] = new[] { "x", "y", "z", "w" };
    }

    public void RedrawAll(string title, object rootInstance)
    {
        foreach (Transform c in contentRoot) { Destroy(c.gameObject); }

        if (rootInstance == null) { return; }

        ValueHandle rootHandle = new ValueHandle(
            title,
            rootInstance.GetType(),
            () => rootInstance,
            _ => { }
        );

        BuildUIForHandle(rootHandle, contentRoot);
    }

    // ========= メインディスパッチャ (振り分け役) =========

    /// <summary>
    /// 渡された ValueHandle に基づいて、適切なUIビルダーを呼び出す
    /// </summary>
    private void BuildUIForHandle(ValueHandle handle, RectTransform parent)
    {
        // 値が null の場合
        if (handle.GetValue() == null)
        {
            // Nullの型(ValueType)が取得できない場合もあるため、Objectとして扱う
            if (handle.ValueType == null || handle.ValueType.IsClass || handle.ValueType.IsInterface)
            {
                BuildNullRow(handle, parent);
                return;
            }
            // (int? など、構造体のNullable型はここでは考慮しない)
        }

        // 辞書(typeBuilders)に登録された型か？ (int, string, Color...)
        if (typeBuilders.TryGetValue(handle.ValueType, out var builder))
        {
            builder(handle, parent);
        }
        // Enum(列挙型)か？
        else if (handle.ValueType.IsEnum)
        {
            BuildEnumRow(handle, parent);
        }
        // Dictionaryか？ (再帰)
        else if (typeof(IDictionary).IsAssignableFrom(handle.ValueType))
        {
            BuildDictionaryUI(handle, parent);
        }
        // Listまたは配列か？ (再帰)
        else if (typeof(IList).IsAssignableFrom(handle.ValueType))
        {
            BuildListUI(handle, parent);
        }
        // 上記以外のクラスか？ (再帰)
        else if (handle.ValueType.IsClass)
        {
            BuildObjectUI(handle.Name, handle.GetValue(), handle.ValueType, parent);
        }
        // サポート外
        else
        {
            BuildUnsupportedRow(handle, parent);
        }
    }


    // ========= コンテナ (再帰) =========

    /// <summary>
    /// クラス/構造体 のUIを構築 (ネストされたオブジェクト用)
    /// </summary>
    private void BuildObjectUI(string title, object instance, Type type, RectTransform parent)
    {
        RectTransform parentGroup = InstantiateGroup(title, parent);

        // public フィールド一覧を取得
        FieldInfo[] fields = type.GetFields(fieldFlags);
        foreach (FieldInfo field in fields)
        {
            // 1. FieldInfoから ValueHandle を作成
            ValueHandle handle = ValueHandle.FromField(instance, field);
            // 2. ディスパッチャに投げる
            BuildUIForHandle(handle, parentGroup);
        }
    }

    /// <summary>
    /// Dictionary のUIを構築
    /// </summary>
    private void BuildDictionaryUI(ValueHandle handle, RectTransform parent)
    {
        IDictionary dict = (IDictionary)handle.GetValue();
        RectTransform parentGroup = InstantiateGroup($"{handle.Name}", parent);

        if (dict == null) { return; }

        foreach (object key in dict.Keys)
        {
            // 1. Dictionaryの要素から ValueHandle を作成
            ValueHandle itemHandle = ValueHandle.FromDictionaryEntry(dict, key);
            // 2. ディスパッチャに投げる
            BuildUIForHandle(itemHandle, parentGroup);
        }
    }

    /// <summary>
    /// List/Array のUIを構築
    /// </summary>
    private void BuildListUI(ValueHandle handle, RectTransform parent)
    {
        IList list = (IList)handle.GetValue();
        RectTransform parentGroup = InstantiateGroup($"{handle.Name}", parent);

        if (list == null) { return; }

        for (int i = 0; i < list.Count; i++)
        {
            // 1. Listの要素から ValueHandle を作成
            var itemHandle = ValueHandle.FromListEntry(list, i);
            // 2. ディスパッチャに投げる
            BuildUIForHandle(itemHandle, parentGroup);
        }
    }


    // ========= プリミティブUIビルダー (共通化) =========

    private void BuildStringRow(ValueHandle handle, RectTransform parent)
    {
        GameObject go = Instantiate(inputPrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);

        TMP_InputField input = go.transform.Find("InputField").GetComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.Standard;
        input.text = (string)handle.GetValue() ?? "";
        input.onEndEdit.AddListener( text => { handle.SetValue(text); });
    }

    private void BuildNumberRow(ValueHandle handle, RectTransform parent)
    {
        GameObject go = Instantiate(inputPrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);

        TMP_InputField input = go.transform.Find("InputField").GetComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.DecimalNumber;

        object currentValue = handle.GetValue();
        input.text = Convert.ToString(currentValue, CultureInfo.InvariantCulture);

        input.onEndEdit.AddListener(text =>
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                try
                {
                    // 元の型 (int, float, double...) に変換してセット
                    object newValue = Convert.ChangeType(d, handle.ValueType);
                    handle.SetValue(newValue);
                    input.text = Convert.ToString(newValue, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Type conversion failed for {handle.Name}: {e.Message}");
                    input.text = Convert.ToString(handle.GetValue(), CultureInfo.InvariantCulture);
                }
            }
            else
            {
                input.text = Convert.ToString(handle.GetValue(), CultureInfo.InvariantCulture);
            }
        });
    }

    private void BuildBoolRow(ValueHandle handle, RectTransform parent)
    {
        GameObject go = Instantiate(togglePrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);

        Toggle toggle = go.transform.Find("Toggle").GetComponent<Toggle>();
        toggle.isOn = (bool)handle.GetValue();

        toggle.onValueChanged.AddListener( v => { handle.SetValue(v); });
    }

    private void BuildEnumRow(ValueHandle handle, RectTransform parent)
    {
        if (dropdownPrefab == null)
        {
            BuildUnsupportedRow(handle, parent);
            return;
        }

        GameObject go = Instantiate(dropdownPrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);

        TMP_Dropdown dropdown = go.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();

        string[] enumNames = Enum.GetNames(handle.ValueType);
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(enumNames));

        object currentValue = handle.GetValue();
        dropdown.value = dropdown.options.FindIndex(opt => opt.text == currentValue.ToString());

        dropdown.onValueChanged.AddListener(index =>
        {
            object newValue = Enum.Parse(handle.ValueType, enumNames[index]);
            handle.SetValue(newValue);
        });
    }

    private void BuildColorRow(ValueHandle handle, RectTransform parent)
    {
        GameObject go = Instantiate(colorPrefab, parent);
        Color currentColor = (Color)handle.GetValue();

        TMP_Text label = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label) { label.text = StringCaseUtility.ToSpacedWords(handle.Name); }

        TMP_InputField rIn = go.transform.Find("RInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField gIn = go.transform.Find("GInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField bIn = go.transform.Find("BInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField aIn = go.transform.Find("AInputField")?.GetComponent<TMP_InputField>();
        Image prev = go.transform.Find("Preview")?.GetComponent<Image>();

        string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        void SetInputs(Color c)
        {
            if (rIn) rIn.text = F(c.r);
            if (gIn) gIn.text = F(c.g);
            if (bIn) bIn.text = F(c.b);
            if (aIn) aIn.text = F(c.a);
            if (prev) prev.color = c;
        }

        SetInputs(currentColor);

        UnityEngine.Events.UnityAction<string> onEndEdit = (text) =>
        {
            float P(TMP_InputField input, float defaultVal)
            {
                return float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                    ? Mathf.Clamp01(v) : defaultVal;
            }

            Color newColor = new Color(
                P(rIn, currentColor.r),
                P(gIn, currentColor.g),
                P(bIn, currentColor.b),
                P(aIn, currentColor.a)
            );

            // 1. Handle経由で値を設定
            handle.SetValue(newColor);
            // 2. ローカルキャッシュを更新
            currentColor = newColor;
            // 3. UIを整形し直す
            SetInputs(newColor);
        };

        if (rIn) { rIn.contentType = TMP_InputField.ContentType.DecimalNumber; rIn.onEndEdit.AddListener(onEndEdit); }
        if (gIn) { gIn.contentType = TMP_InputField.ContentType.DecimalNumber; gIn.onEndEdit.AddListener(onEndEdit); }
        if (bIn) { bIn.contentType = TMP_InputField.ContentType.DecimalNumber; bIn.onEndEdit.AddListener(onEndEdit); }
        if (aIn) { aIn.contentType = TMP_InputField.ContentType.DecimalNumber; aIn.onEndEdit.AddListener(onEndEdit); }
    }

    /// <summary>
    /// Color32 (0-255) 用のUIビルダー
    /// </summary>
    private void BuildColor32Row(ValueHandle handle, RectTransform parent)
    {
        GameObject go = Instantiate(colorPrefab, parent);
        Color32 currentColor = (Color32)handle.GetValue();

        TMP_Text label = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label) { label.text = StringCaseUtility.ToSpacedWords(handle.Name); }

        TMP_InputField rIn = go.transform.Find("RInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField gIn = go.transform.Find("GInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField bIn = go.transform.Find("BInputField")?.GetComponent<TMP_InputField>();
        TMP_InputField aIn = go.transform.Find("AInputField")?.GetComponent<TMP_InputField>();
        Image prev = go.transform.Find("Preview")?.GetComponent<Image>();

        // 0-255 の整数 (byte) を文字列に変換
        string F(byte v) => v.ToString(CultureInfo.InvariantCulture);

        void SetInputs(Color32 c)
        {
            // InputFieldには 0-255 の文字列を設定
            if (rIn) rIn.text = F(c.r);
            if (gIn) gIn.text = F(c.g);
            if (bIn) bIn.text = F(c.b);
            if (aIn) aIn.text = F(c.a);

            // Preview (Image) には Color (0-1) が必要なので変換
            if (prev) prev.color = c; // Color32からColorへの暗黙的型変換
        }

        SetInputs(currentColor);

        UnityEngine.Events.UnityAction<string> onEndEdit = (text) =>
        {
            // 入力文字列を 0-255 の byte に変換
            byte P(TMP_InputField input, byte defaultVal)
            {
                if (int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                {
                    // 0-255の範囲にクランプ
                    return (byte)Mathf.Clamp(v, 0, 255);
                }
                return defaultVal;
            }

            // 新しい Color32 を構築
            Color32 newColor = new Color32(
                P(rIn, currentColor.r),
                P(gIn, currentColor.g),
                P(bIn, currentColor.b),
                P(aIn, currentColor.a)
            );

            // 1. Handle経由で値を設定
            handle.SetValue(newColor);
            // 2. ローカルキャッシュを更新
            currentColor = newColor;
            // 3. UIを整形し直す
            SetInputs(newColor);
        };

        // 入力タイプを整数に設定 (DecimalNumberでも整数は入力可)
        if (rIn) { rIn.contentType = TMP_InputField.ContentType.IntegerNumber; rIn.onEndEdit.AddListener(onEndEdit); }
        if (gIn) { gIn.contentType = TMP_InputField.ContentType.IntegerNumber; gIn.onEndEdit.AddListener(onEndEdit); }
        if (bIn) { bIn.contentType = TMP_InputField.ContentType.IntegerNumber; bIn.onEndEdit.AddListener(onEndEdit); }
        if (aIn) { aIn.contentType = TMP_InputField.ContentType.IntegerNumber; aIn.onEndEdit.AddListener(onEndEdit); }
    }

    /// <summary>
    /// 汎用：Vector3, Vector2, Rect などの構造体(Struct)UIビルダー
    /// </summary>
    private void BuildStructRow(ValueHandle handle, RectTransform parent)
    {
        // 1. プレハブまたは定義の確認
        if (multiFloatPrefab == null || !structDefinitions.TryGetValue(handle.ValueType, out var propNames))
        {
            BuildUnsupportedRow(handle, parent);
            return;
        }

        GameObject go = Instantiate(multiFloatPrefab, parent);

        // 2. UI要素の参照を取得 (最大4つ)
        TMP_Text label = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label) { label.text = StringCaseUtility.ToSpacedWords(handle.Name); }

        TMP_InputField[] inputs = new[]
        {
            go.transform.Find("InputField1")?.GetComponent<TMP_InputField>(),
            go.transform.Find("InputField2")?.GetComponent<TMP_InputField>(),
            go.transform.Find("InputField3")?.GetComponent<TMP_InputField>(),
            go.transform.Find("InputField4")?.GetComponent<TMP_InputField>()
        };

        // 3. フィールド情報（"x", "y", "z" など）を取得
        int propCount = propNames.Count;

        FieldInfo[] fields = new FieldInfo[propCount];
        for (int i = 0; i < propCount; i++) { fields[i] = handle.ValueType.GetField(propNames[i]); }

        // --- ヘルパー関数: オブジェクトの値から全UI入力欄を更新する ---
        Action<object> setUiFromValue = (objValue) =>
        {
            for (int i = 0; i < propCount; i++)
            {
                if (inputs[i] != null && fields[i] != null)
                {
                    object propVal = fields[i].GetValue(objValue);
                    inputs[i].text = Convert.ToString(propVal, CultureInfo.InvariantCulture);
                }
            }
        };

        // --- 共通の編集終了リスナー ---
        UnityEngine.Events.UnityAction<string> onEndEdit = (text) =>
        {
            object[] constructorArgs = new object[propCount];
            bool allParsed = true;

            // 1. 全ての入力欄（x, y, z...）のテキストをパース
            for (int i = 0; i < propCount; i++)
            {
                string currentText = inputs[i].text;

                Type fieldType = fields[i].FieldType;

                try
                {
                    // Vector2/3/4/Rect は全て float で構成されている前提
                    if (float.TryParse(currentText, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    {
                        constructorArgs[i] = val;
                    }
                    else
                    {
                        allParsed = false;
                        break;
                    }
                }
                catch { allParsed = false; break; }
            }

            // 2. もし全てのパースが成功したら、新しいインスタンスを作って値を保存
            if (allParsed)
            {
                object newValue = Activator.CreateInstance(handle.ValueType, constructorArgs);
                handle.SetValue(newValue);
            }

            // 3. 常にUIを再描画
            setUiFromValue(handle.GetValue());
        };


        // 4. 入力欄のセットアップ
        for (int i = 0; i < inputs.Length; i++)
        {
            TMP_InputField input = inputs[i];
            if (input == null) { continue; }

            if (i < propCount)
            {
                input.contentType = TMP_InputField.ContentType.DecimalNumber;
                input.onEndEdit.AddListener(onEndEdit);
            }
            else
            {
                input.gameObject.SetActive(false);
            }
        }

        // 5. 最初の値をUIに設定
        setUiFromValue(handle.GetValue());
    }

    // ========= ヘルパ系 =========

    /// <summary>
    /// グループPrefabを生成し、ContentのRectTransformを返す
    /// </summary>
    private RectTransform InstantiateGroup(string title, RectTransform parent)
    {
        GameObject group = Instantiate(groupPrefab, parent);

        string displayTitle = StringCaseUtility.ToSpacedWords(title);
        group.transform.Find("Header/Text").GetComponent<TMP_Text>().text = displayTitle;

        RectTransform content = group.transform.Find("Content").GetComponent<RectTransform>();

        return content;
    }

    private void BuildUnsupportedRow(ValueHandle handle, RectTransform parent)
    {
        if (unsupportedPrefab == null) { return; }
        GameObject go = Instantiate(unsupportedPrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);
        go.transform.Find("Type").GetComponent<TMP_Text>().text = $"({handle.ValueType.Name})";
    }

    private void BuildNullRow(ValueHandle handle, RectTransform parent)
    {
        // rowNullPrefabは、Labelと "(null)" と表示するText(Type)で構成想定
        if (unsupportedPrefab == null) { return; }
        GameObject go = Instantiate(unsupportedPrefab, parent);
        go.transform.Find("Label").GetComponent<TMP_Text>().text = StringCaseUtility.ToSpacedWords(handle.Name);
        if (go.transform.Find("Type") != null) { go.transform.Find("Type").GetComponent<TMP_Text>().text = "(null)"; }
    }
}