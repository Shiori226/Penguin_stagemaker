using System;
using System.Collections;
using System.Reflection;

/// <summary>
/// フィールドやList/Dictの要素の「値」を統一的に扱うためのラッパー。
/// これにより、UIビルダーメソッドの重複をなくす。
/// </summary>
public class ValueHandle
{
    public string Name { get; }
    public Type ValueType { get; }

    // 値を取得するデリゲート
    private readonly Func<object> getter;
    // 値を設定するデリゲート
    private readonly Action<object> setter;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ValueHandle(string name, Type type, Func<object> getter, Action<object> setter)
    {
        this.Name = name;
        this.ValueType = type;
        this.getter = getter;
        this.setter = setter;
    }

    /// <summary>
    /// 現在の値を取得
    /// </summary>
    public object GetValue() => getter();

    /// <summary>
    /// 新しい値を設定
    /// </summary>
    public void SetValue(object newValue) => setter(newValue);

    /// <summary>
    /// ファクトリ: クラスのフィールド(FieldInfo)からHandleを作成
    /// </summary>
    public static ValueHandle FromField(object instance, FieldInfo field)
    {
        return new ValueHandle(
            field.Name,
            field.FieldType,
            () => field.GetValue(instance), // Getter
            (val) => field.SetValue(instance, val) // Setter
        );
    }

    /// <summary>
    /// ファクトリ: Listの要素からHandleを作成
    /// </summary>
    public static ValueHandle FromListEntry(IList list, int index)
    {
        return new ValueHandle(
            $"[{index}]",
            list[index]?.GetType(), // Null許容
            () => list[index], // Getter
            (val) => list[index] = val // Setter
        );
    }

    /// <summary>
    /// ファクトリ: Dictionaryの要素からHandleを作成
    /// </summary>
    public static ValueHandle FromDictionaryEntry(IDictionary dict, object key)
    {
        return new ValueHandle(
            key.ToString(),
            dict[key]?.GetType(), // Null許容
            () => dict[key], // Getter
            (val) => dict[key] = val // Setter
        );
    }
}