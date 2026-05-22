using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

/// <summary>
/// シーン内のログ対象メンバー（[StreamData]/[SnapshotData]属性を持つ）を
/// リフレクションを用いて収集・管理するクラス。
/// </summary>
public sealed class DataRegistry
{
    public IReadOnlyList<LogItem> StreamItems => streamItems;
    public IReadOnlyList<LogItem> SnapshotItems => snapshotItems;

    public event Action<LogItem> StreamMemberRegistered;
    public event Action<LogItem> StreamMemberUnregistered;
    public event Action<LogItem> SnapshotMemberRegistered;
    public event Action<LogItem> SnapshotMemberUnregistered;

    private readonly List<LogItem> streamItems = new();
    private readonly List<LogItem> snapshotItems = new();

    // メンバー検索に使う共通フラグ
    private const BindingFlags MemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// シーン全体をスキャンし、ログ対象メンバーのリストを再構築。
    /// </summary>
    public void Refresh()
    {
        streamItems.Clear();
        snapshotItems.Clear();

        // アクティブ・非アクティブ問わず全ての MonoBehaviour を検索
        IEnumerable<MonoBehaviour> scope = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

        foreach (MonoBehaviour mb in scope)
        {
            Type type = mb.GetType();
            ScanMembers(mb, type);
        }

        SortByOrderThenKey(streamItems);
        SortByOrderThenKey(snapshotItems);
    }

    /// <summary>
    /// 指定されたコンポーネントの特定メンバーを動的にログ対象として登録。
    /// </summary>
    /// <param name="target">対象コンポーネント</param>
    /// <param name="memberName">登録するフィールド名またはプロパティ名</param>
    public void RegisterMember(Component target, string memberName)
    {
        if (!target || string.IsNullOrEmpty(memberName))
        {
            return;
        }

        if (!TryRegisterMember(target, memberName))
        {
            Debug.LogWarning($"[DataRegistry] メンバー '{memberName}' が見つからないか、属性が付いていません（型: {target.GetType().Name}）。", target);
        }
    }

    /// <summary>
    /// 指定されたコンポーネントの特定メンバーをログ対象から動的に解除。
    /// </summary>
    /// <param name="target">対象コンポーネント</param>
    /// <param name="memberName">解除するフィールド名またはプロパティ名</param>
    public void UnregisterMember(Component target, string memberName)
    {
        if (!target || string.IsNullOrEmpty(memberName))
        {
            return;
        }

        Type type = target.GetType();

        // メンバー情報 (Field または Property) を取得
        MemberInfo memberInfo = (MemberInfo)type.GetField(memberName, MemberBindingFlags)
                                ?? type.GetProperty(memberName, MemberBindingFlags);

        if (memberInfo == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[DataRegistry] 解除対象のメンバー '{memberName}' が見つかりません（型: {type.Name}）。", target);
#endif
            return;
        }

        string key = $"{memberInfo.DeclaringType.FullName}.{memberName}";
        int removedCount = 0;

        // Stream から解除
        removedCount += streamItems.RemoveAll(item =>
        {
            if (item != null && item.target == target && item.stableKey == key)
            {
                StreamMemberUnregistered?.Invoke(item); // 削除直前にイベント発行
                return true;
            }
            return false;
        });

        // Snapshot から解除
        removedCount += snapshotItems.RemoveAll(item =>
        {
            if (item != null && item.target == target && item.stableKey == key)
            {
                SnapshotMemberUnregistered?.Invoke(item); // 削除直前にイベント発行
                return true;
            }
            return false;
        });

#if UNITY_EDITOR
        if (removedCount == 0)
        {
            Debug.LogWarning($"[DataRegistry] 解除対象が見つかりません: {type.Name}.{memberName} (key: {key})", target);
        }
#endif
    }

    /// <summary>
    /// 現在の StreamItems と SnapshotItems を 'order' と 'stableKey' に基づいて再ソート。
    /// （主に DataLogger で OrderOverride を適用した後に呼ばれる）
    /// </summary>
    public void Resort()
    {
        SortByOrderThenKey(streamItems);
        SortByOrderThenKey(snapshotItems);
    }

    // ===== 内部処理 =====

    /// <summary>
    /// 指定されたインスタンスの型をスキャンし、属性を持つメンバーをリストに追加。
    /// </summary>
    private void ScanMembers(MonoBehaviour mb, Type type)
    {
        // Fields
        foreach (FieldInfo fieldInfo in type.GetFields(MemberBindingFlags))
        {
            if (fieldInfo.GetCustomAttribute<StreamDataAttribute>() is StreamDataAttribute stream)
            {
                streamItems.Add(CreateLogItem(mb, fieldInfo, fieldInfo.FieldType, stream.Label, stream.Order));
            }
            if (fieldInfo.GetCustomAttribute<SnapshotDataAttribute>() is SnapshotDataAttribute snapshot)
            {
                snapshotItems.Add(CreateLogItem(mb, fieldInfo, fieldInfo.FieldType, snapshot.Label, snapshot.Order));
            }
        }

        // Properties (getter only)
        foreach (PropertyInfo propertyInfo in type.GetProperties(MemberBindingFlags))
        {
            MethodInfo get = propertyInfo.GetGetMethod(true);
            if (get == null || get.GetParameters().Length != 0)
            {
                continue;
            }

            if (propertyInfo.GetCustomAttribute<StreamDataAttribute>() is StreamDataAttribute stream)
            {
                streamItems.Add(CreateLogItem(mb, propertyInfo, propertyInfo.PropertyType, stream.Label, stream.Order));
            }
            if (propertyInfo.GetCustomAttribute<SnapshotDataAttribute>() is SnapshotDataAttribute snapshot)
            {
                snapshotItems.Add(CreateLogItem(mb, propertyInfo, propertyInfo.PropertyType, snapshot.Label, snapshot.Order));
            }
        }
    }

    /// <summary>
    /// RegisterMember のための個別登録処理。
    /// </summary>
    private bool TryRegisterMember(Component target, string memberName)
    {
        Type type = target.GetType();

        // Field
        FieldInfo fieldInfo = type.GetField(memberName, MemberBindingFlags);
        if (fieldInfo != null)
        {
            // stableKey を計算
            string key = $"{fieldInfo.DeclaringType.FullName}.{memberName}";

            // 重複チェック
            if (streamItems.Exists(it => it.target == target && it.stableKey == key)) { return true; }
            if (snapshotItems.Exists(it => it.target == target && it.stableKey == key)) { return true; }


            if (fieldInfo.GetCustomAttribute<StreamDataAttribute>() is StreamDataAttribute stream)
            {
                LogItem item = CreateLogItem(target as MonoBehaviour, fieldInfo, fieldInfo.FieldType, stream.Label, stream.Order);
                streamItems.Add(item);
                SortByOrderThenKey(streamItems);
                StreamMemberRegistered?.Invoke(item);
                return true;
            }
            if (fieldInfo.GetCustomAttribute<SnapshotDataAttribute>() is SnapshotDataAttribute snapshot)
            {
                LogItem item = CreateLogItem(target as MonoBehaviour, fieldInfo, fieldInfo.FieldType, snapshot.Label, snapshot.Order);
                snapshotItems.Add(item);
                SortByOrderThenKey(snapshotItems);
                SnapshotMemberRegistered?.Invoke(item);
                return true;
            }
            return false; // 属性なし
        }

        // Property
        PropertyInfo propertyInfo = type.GetProperty(memberName, MemberBindingFlags);
        if (propertyInfo != null)
        {
            // stableKey を計算
            string key = $"{propertyInfo.DeclaringType.FullName}.{memberName}";

            // 重複チェック
            if (streamItems.Exists(it => it.target == target && it.stableKey == key)) { return true; }
            if (snapshotItems.Exists(it => it.target == target && it.stableKey == key)) { return true; }

            MethodInfo get = propertyInfo.GetGetMethod(true);
            if (get == null || get.GetParameters().Length != 0)
            {
                Debug.LogWarning($"[DataRegistry] '{type.Name}.{propertyInfo.Name}' は引数なし getter が必要です。", target);
                return false;
            }

            if (propertyInfo.GetCustomAttribute<StreamDataAttribute>() is StreamDataAttribute stream)
            {
                LogItem item = CreateLogItem(target as MonoBehaviour, propertyInfo, propertyInfo.PropertyType, stream.Label, stream.Order);
                streamItems.Add(item);
                SortByOrderThenKey(streamItems);
                StreamMemberRegistered?.Invoke(item);
                return true;
            }
            if (propertyInfo.GetCustomAttribute<SnapshotDataAttribute>() is SnapshotDataAttribute snapshot)
            {
                LogItem item = CreateLogItem(target as MonoBehaviour, propertyInfo, propertyInfo.PropertyType, snapshot.Label, snapshot.Order);
                snapshotItems.Add(item);
                SortByOrderThenKey(snapshotItems);
                SnapshotMemberRegistered?.Invoke(item);
                return true;
            }
            return false; // 属性なし
        }

        return false; // メンバー見つからず
    }

    /// <summary>
    /// LogItem インスタンスを生成するファクトリメソッド。
    /// </summary>
    private LogItem CreateLogItem(MonoBehaviour target, MemberInfo memberInfo, Type memberType, string label, int order)
    {
        string baseLabel = string.IsNullOrWhiteSpace(label) ? memberInfo.Name : label;
        Func<object> getter = null;

        if (memberInfo is FieldInfo fi)
        {
            getter = MakeFastFieldGetter(fi, target);
        }
        else if (memberInfo is PropertyInfo pi)
        {
            getter = MakeFastPropertyGetter(pi, target);
        }

        return new LogItem
        {
            target = target,
            label = baseLabel,
            typeName = memberType.Name,
            order = order,
            stableKey = $"{memberInfo.DeclaringType.FullName}.{memberInfo.Name}",
            getter = getter
        };
    }

    private static void SortByOrderThenKey(List<LogItem> list)
    {
        list.Sort((a, b) =>
        {
            int c = a.order.CompareTo(b.order);
            if (c != 0)
            {
                return c;
            }
            return string.CompareOrdinal(a.stableKey, b.stableKey);
        });
    }

    // ===== 高速 Getter 生成 (Expression) =====

    private static Func<object> MakeFastFieldGetter(FieldInfo fieldInfo, object target)
    {
        // (object o) => (object)((TDeclaring)o).FieldName
        var obj = Expression.Parameter(typeof(object), "o");
        var cast = Expression.Convert(obj, fieldInfo.DeclaringType);
        var fld = Expression.Field(cast, fieldInfo);
        var box = Expression.Convert(fld, typeof(object));
        var lam = Expression.Lambda<Func<object, object>>(box, obj).Compile();
        return () => lam(target);
    }

    private static Func<object> MakeFastPropertyGetter(PropertyInfo propertyInfo, object target)
    {
        // (object o) => (object)((TDeclaring)o).PropertyName
        MethodInfo get = propertyInfo.GetGetMethod(true);
        var obj = Expression.Parameter(typeof(object), "o");
        var cast = Expression.Convert(obj, propertyInfo.DeclaringType);
        var call = Expression.Call(cast, get);
        var box = Expression.Convert(call, typeof(object));
        var lam = Expression.Lambda<Func<object, object>>(box, obj).Compile();
        return () => lam(target);
    }
}