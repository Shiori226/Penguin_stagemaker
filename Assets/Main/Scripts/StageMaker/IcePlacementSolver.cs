using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 氷 (Platform カテゴリ) パーツの「重なり防止 + 隣接吸着」を解決する純ロジック。
    /// 各氷を XZ 平面上の外接円で近似する。外接円なら
    /// 「中心間距離 >= r1 + r2 → メッシュ非重複」が厳密に成立する。
    /// </summary>
    public static class IcePlacementSolver
    {
        public struct CircleObstacle
        {
            public Vector2 center;   // placement.worldPosition の XZ
            public float radius;
        }

        // 接触距離からこの範囲内に近づいたら「くっつく」吸着マージン (ワールド単位)
        private const float SnapMarginWorld = 1.5f;
        private const float ContactEpsilon = 1e-3f;
        private const int MaxIterations = 8;
        // AABB フォールバック用の安全係数 (= 1/cos(22.5°))。
        // AABB 半幅は向きによって外接円半径より小さくなり得るため上乗せする。
        private const float AabbSafetyFactor = 1.085f;

        private static readonly Dictionary<string, float> radiusCache = new();

        /// <summary>
        /// パーツの XZ 外接円半径をプレハブのメッシュから求める (partId でキャッシュ)。
        /// メッシュが読めない場合は AABB 半幅 × 安全係数にフォールバックする。
        /// </summary>
        public static float GetPartRadius(StagePartDefinition def)
        {
            if (def == null || def.prefab == null || string.IsNullOrEmpty(def.id)) { return 0f; }
            if (radiusCache.TryGetValue(def.id, out float cached)) { return cached; }

            float radius = 0f;
            Transform root = def.prefab.transform;
            foreach (var mf in def.prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) { continue; }

                if (mesh.isReadable)
                {
                    foreach (var v in mesh.vertices)
                    {
                        Vector3 p = root.InverseTransformPoint(mf.transform.TransformPoint(v));
                        float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                        if (r > radius) { radius = r; }
                    }
                }
                else
                {
                    // ビルドで頂点にアクセスできない場合: バウンズの角から半幅を推定
                    Bounds b = mesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 corner = new Vector3(
                            (i & 1) == 0 ? b.min.x : b.max.x,
                            (i & 2) == 0 ? b.min.y : b.max.y,
                            (i & 4) == 0 ? b.min.z : b.max.z);
                        Vector3 p = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                        float r = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)) * AabbSafetyFactor;
                        if (r > radius) { radius = r; }
                    }
                }
            }

            radiusCache[def.id] = radius;
            return radius;
        }

        /// <summary>
        /// partsRoot 直下の配置済み氷 (Platform 本体) を障害物として buffer に集める。
        /// exclude はドラッグ中の自分自身 / ゴーストを除外するための参照。
        /// </summary>
        public static void CollectPlatformObstacles(
            Transform partsRoot, CustomStagePartPlacement exclude, List<CircleObstacle> buffer)
        {
            buffer.Clear();
            if (partsRoot == null) { return; }

            for (int i = 0; i < partsRoot.childCount; i++)
            {
                var part = partsRoot.GetChild(i).GetComponent<DraggablePart>();
                if (part == null || part.isHandle) { continue; }
                if (part.placement == null || part.definition == null) { continue; }
                if (part.definition.category != StagePartCategory.Platform) { continue; }
                if (ReferenceEquals(part.placement, exclude)) { continue; }

                float radius = GetPartRadius(part.definition);
                if (radius <= 0f) { continue; }

                buffer.Add(new CircleObstacle
                {
                    center = new Vector2(part.placement.worldPosition.x, part.placement.worldPosition.z),
                    radius = radius,
                });
            }
        }

        /// <summary>
        /// 望みの位置 desired を「どの障害物とも重ならず、近ければ接する」位置に解決する。
        /// 戻り値 false は「desired 自体が重なっていて、置ける位置も見つからない」場合のみ。
        /// </summary>
        public static bool TryResolve(
            Vector2 desired, float radius,
            IReadOnlyList<CircleObstacle> obstacles,
            out Vector2 resolved)
        {
            resolved = desired;
            if (obstacles == null || obstacles.Count == 0) { return true; }

            // 最小クリアランス (負 = 重なり) の障害物を探す
            int nearestIndex = 0;
            float minClearance = float.MaxValue;
            for (int i = 0; i < obstacles.Count; i++)
            {
                float clearance = Vector2.Distance(desired, obstacles[i].center)
                    - (radius + obstacles[i].radius);
                if (clearance < minClearance)
                {
                    minClearance = clearance;
                    nearestIndex = i;
                }
            }
            if (minClearance > SnapMarginWorld) { return true; }   // 干渉なし: 自由配置

            bool wasOverlapping = minClearance < -ContactEpsilon;

            // 一次スナップ: 最近傍の接触円 (半径 r + rA) へ中心→カーソル方向に射影
            var nearest = obstacles[nearestIndex];
            float contactA = radius + nearest.radius;
            Vector2 dir = desired - nearest.center;
            if (dir.sqrMagnitude < 1e-8f) { dir = Vector2.right; }  // 退化: 中心一致
            Vector2 candidate = nearest.center + dir.normalized * contactA;
            if (IsClear(candidate, obstacles, radius))
            {
                resolved = candidate;
                return true;
            }

            // 2円同時接触: 射影先と重なる障害物 B との交点 (V字の谷に収まる位置) を試す
            Vector2 best = default;
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (i == nearestIndex) { continue; }
                float contactB = radius + obstacles[i].radius;
                if (Vector2.Distance(candidate, obstacles[i].center) >= contactB - ContactEpsilon) { continue; }

                int count = IntersectCircles(
                    nearest.center, contactA, obstacles[i].center, contactB,
                    out Vector2 pA, out Vector2 pB);
                for (int k = 0; k < count; k++)
                {
                    Vector2 p = (k == 0) ? pA : pB;
                    float dist = Vector2.Distance(p, desired);
                    if (dist < bestDist && IsClear(p, obstacles, radius))
                    {
                        best = p;
                        bestDist = dist;
                        found = true;
                    }
                }
            }
            if (found)
            {
                resolved = best;
                return true;
            }

            // フォールバック: 最深貫入の障害物から順に押し出す
            Vector2 pos = desired;
            for (int k = 0; k < MaxIterations; k++)
            {
                int deepestIndex = -1;
                float deepestPenetration = ContactEpsilon;
                for (int i = 0; i < obstacles.Count; i++)
                {
                    float penetration = (radius + obstacles[i].radius)
                        - Vector2.Distance(pos, obstacles[i].center);
                    if (penetration > deepestPenetration)
                    {
                        deepestPenetration = penetration;
                        deepestIndex = i;
                    }
                }
                if (deepestIndex < 0) { break; }

                var o = obstacles[deepestIndex];
                Vector2 push = pos - o.center;
                if (push.sqrMagnitude < 1e-8f) { push = Vector2.right; }
                pos = o.center + push.normalized * (radius + o.radius);
            }
            // 変位キャップで遠方への飛び (テレポート) を防ぐ
            if (IsClear(pos, obstacles, radius)
                && Vector2.Distance(pos, desired) <= 2.5f * radius + SnapMarginWorld)
            {
                resolved = pos;
                return true;
            }

            // 吸着だけ失敗したケースは元位置が合法なので素通し
            if (!wasOverlapping)
            {
                resolved = desired;
                return true;
            }
            return false;
        }

        private static bool IsClear(Vector2 p, IReadOnlyList<CircleObstacle> obstacles, float radius)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (Vector2.Distance(p, obstacles[i].center)
                    < radius + obstacles[i].radius - ContactEpsilon)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>2円の交点を求める。戻り値は交点数 (0/1/2)。</summary>
        private static int IntersectCircles(
            Vector2 c0, float r0, Vector2 c1, float r1, out Vector2 pA, out Vector2 pB)
        {
            pA = default;
            pB = default;
            float d = Vector2.Distance(c0, c1);
            if (d < 1e-6f) { return 0; }
            if (d > r0 + r1 || d < Mathf.Abs(r0 - r1)) { return 0; }

            float a = (r0 * r0 - r1 * r1 + d * d) / (2f * d);
            float h2 = r0 * r0 - a * a;
            Vector2 mid = c0 + (c1 - c0) * (a / d);
            if (h2 <= 0f)
            {
                pA = mid;
                return 1;
            }
            float h = Mathf.Sqrt(h2);
            Vector2 perp = new Vector2(-(c1.y - c0.y), c1.x - c0.x) / d;
            pA = mid + perp * h;
            pB = mid - perp * h;
            return 2;
        }
    }
}
