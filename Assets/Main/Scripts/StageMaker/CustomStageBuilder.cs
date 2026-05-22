using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CustomStageData から実際のゲームオブジェクトを生成するビルダー。
    ///   - Start / Goal はカスタム配置不可。固定位置に必ず置かれる。
    ///   - Shark は既存シーン (海) 側でスポーンしているのでここでは生成しない。
    ///   - 方向性パーツ (Moving Ice / Blizzard / Seal) は CustomDirectionalRuntime で挙動を上書き。
    /// </summary>
    public static class CustomStageBuilder
    {
        // 固定位置 (CustomStageData.parts には含まれない)
        public static readonly Vector3 FixedStartPosition = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 FixedGoalPosition = new Vector3(0f, 0f, 60f);


        public static GameObject Build(CustomStageData data, Transform parent = null)
        {
            if (data == null)
            {
                Debug.LogError("[CustomStageBuilder] data is null");
                return null;
            }

            var catalog = StagePartCatalog.Load();
            if (catalog == null)
            {
                Debug.LogError("[CustomStageBuilder] catalog not loaded");
                return null;
            }

            var root = new GameObject("CustomStage_" + data.displayName);
            if (parent != null) { root.transform.SetParent(parent, false); }
            root.AddComponent<CustomStageInfo>().SetData(data);

            // カテゴリごとの親
            // ※ Shark は既存シーン側 (海) でスポーンしているのでここでは生成しない
            var platformsRoot = CreateGroup(root.transform, "Platforms");
            var itemsRoot     = CreateGroup(root.transform, "Items");
            var gimmicksRoot  = CreateGroup(root.transform, "Gimmicks");
            var enemyRoot     = CreateGroup(root.transform, "Enemies");
            var sealsRoot     = CreateGroup(enemyRoot, "Seals");

            // Start / Goal は固定位置に強制配置
            SpawnFixedSpecial(catalog, "PlatformStart", FixedStartPosition, platformsRoot);
            SpawnFixedSpecial(catalog, "PlatformGoal", FixedGoalPosition, platformsRoot);

            // ユーザ配置パーツ
            foreach (var p in data.parts)
            {
                var def = catalog.Find(p.partId);
                if (def == null || def.prefab == null)
                {
                    Debug.LogWarning($"[CustomStageBuilder] Unknown part id '{p.partId}' — skipping");
                    continue;
                }
                // Start/Goal/Shark は配置データに混ざっていても無視 (旧スキーマ救済)
                if (def.id == "PlatformStart" || def.id == "PlatformGoal" || def.id == "Shark") { continue; }

                Transform groupParent;
                switch (def.category)
                {
                    case StagePartCategory.Platform: groupParent = platformsRoot; break;
                    case StagePartCategory.Item:     groupParent = itemsRoot; break;
                    case StagePartCategory.Gimmick:  groupParent = gimmicksRoot; break;
                    case StagePartCategory.Enemy:    groupParent = sealsRoot; break;
                    default:                          groupParent = root.transform; break;
                }

                // Moving Ice は距離に応じて複数体スポーン (単独の Instantiate ではなく専用処理)
                if (def.directionalKind == "MovingIce" || def.directionalKind == "MovingIcePingPong")
                {
                    bool surfacePingPong = def.directionalKind == "MovingIcePingPong";
                    SpawnMovingIceGroup(def, p, gimmicksRoot, surfacePingPong);
                    continue;
                }

                Vector3 pos = p.worldPosition + def.spawnOffset;
                Quaternion rot = Quaternion.Euler(0f, p.rotationY, 0f);
                var go = Object.Instantiate(def.prefab, pos, rot, groupParent);
                go.name = def.id;

                // 方向性パーツ: ランタイムで挙動を上書き (Blizzard / Seal)
                if (def.isDirectional && !string.IsNullOrEmpty(def.directionalKind))
                {
                    var dir = go.AddComponent<CustomDirectionalRuntime>();
                    dir.kind = def.directionalKind;
                    dir.origin = p.worldPosition;
                    Vector3 t = p.directionTarget;
                    if (t == Vector3.zero) { t = p.worldPosition + Vector3.forward * 5f; }
                    dir.target = t;
                }
            }

            return root;
        }

        private static void SpawnFixedSpecial(StagePartCatalog catalog, string id, Vector3 worldPos, Transform parent)
        {
            var def = catalog.Find(id);
            if (def == null || def.prefab == null)
            {
                Debug.LogError($"[CustomStageBuilder] Required fixed part '{id}' not in catalog");
                return;
            }
            Vector3 pos = worldPos + def.spawnOffset;
            var go = Object.Instantiate(def.prefab, pos, Quaternion.identity, parent);
            go.name = def.id;
        }

        /// <summary>
        /// Moving Ice 用の特別生成処理。距離に応じて複数体を同時に走らせ、
        /// 位相をずらして等間隔に並ぶようにする。
        /// surfacePingPong = true のときは水中潜行をしない origin↔target の往復モード。
        /// </summary>
        private static void SpawnMovingIceGroup(StagePartDefinition def, CustomStagePartPlacement p, Transform parent, bool surfacePingPong)
        {
            const float Speed = 4f;
            const float FloatSinkDuration = 0.8f;
            const float FloatSinkDistance = 1.5f;
            const float RestartGap = 1.0f;
            const float SpacingPerIce = 8f; // 1 体あたりがカバーするおおよその距離 (m)
            const int MaxIceCount = 5;

            Vector3 dir = p.directionTarget - p.worldPosition;
            float distance = dir.magnitude;
            if (distance < 0.01f) { distance = 1f; }

            // PingPong (往復) は 1 体が往来するだけなので、距離に関わらず常に 1 体のみ生成する
            int count = surfacePingPong ? 1 : Mathf.Clamp(Mathf.CeilToInt(distance / SpacingPerIce), 1, MaxIceCount);

            var groupGo = new GameObject(def.id + "_Group");
            groupGo.transform.SetParent(parent, false);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = p.worldPosition + def.spawnOffset;
                var ice = Object.Instantiate(def.prefab, spawnPos, Quaternion.identity, groupGo.transform);
                ice.name = def.id + "_" + i;

                // 元の MovingIceController は使わない (gameObject.SetActive(false) を呼ぶので扱いづらい)
                var origCtrl = ice.GetComponent<MovingIceController>();
                if (origCtrl != null) { origCtrl.enabled = false; }

                var looper = ice.AddComponent<MovingIceLooper>();
                float phase = (count <= 1) ? 0f : ((float)i / count);
                looper.Configure(p.worldPosition, p.directionTarget,
                    Speed, FloatSinkDuration, FloatSinkDistance, RestartGap, phase, surfacePingPong);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }

    /// <summary>
    /// 生成された CustomStage に貼り付けて、元の CustomStageData を保持する。
    /// CSV エクスポート時にステージ情報を取り出すために使用される。
    /// </summary>
    public class CustomStageInfo : MonoBehaviour
    {
        private CustomStageData data;
        public CustomStageData Data => data;

        public void SetData(CustomStageData d) { data = d; }
    }
}
