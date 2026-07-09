using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 氷 (Platform カテゴリ) パーツの「重なり防止 + 隣接吸着」の Unity 側窓口。
    /// 各氷はメッシュを XZ 平面へ投影した凸包 (実際の輪郭) として扱い、
    /// 2つの氷のミンコフスキー領域への最近点射影でスナップする (計算は IceGeometry)。
    /// 円近似と違い、接触した辺同士は隙間なくくっつく。
    /// </summary>
    public static class IcePlacementSolver
    {
        public struct PlacedIce
        {
            public Vector2 center;   // placement.worldPosition の XZ
            public StagePartDefinition def;
        }

        // partId → XZ 凸包 (アンカー原点基準)
        private static readonly Dictionary<string, Vector2[]> hullCache = new();
        // "movingId|obstacleId" → ミンコフスキー領域
        private static readonly Dictionary<string, Vector2[]> minkowskiCache = new();
        private static readonly List<Vector2> pointScratch = new();
        private static readonly List<(Vector2[] poly, Vector2 offset)> regionScratch = new();

        /// <summary>
        /// partsRoot 直下の配置済み氷 (Platform 本体) を障害物として buffer に集める。
        /// exclude はドラッグ中の自分自身 / ゴーストを除外するための参照。
        /// </summary>
        public static void CollectPlatformObstacles(
            Transform partsRoot, CustomStagePartPlacement exclude, List<PlacedIce> buffer)
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

                buffer.Add(new PlacedIce
                {
                    center = new Vector2(part.placement.worldPosition.x, part.placement.worldPosition.z),
                    def = part.definition,
                });
            }
        }

        /// <summary>
        /// 望みの位置 desired を「どの氷とも重ならず、近ければ辺が接する」位置に解決する。
        /// 戻り値 false は「desired 自体が重なっていて、置ける位置も見つからない」場合のみ。
        /// </summary>
        public static bool TryResolve(
            StagePartDefinition movingDef, Vector2 desired,
            IReadOnlyList<PlacedIce> obstacles, out Vector2 resolved)
        {
            resolved = desired;
            if (obstacles == null || obstacles.Count == 0) { return true; }

            Vector2[] movingHull = GetHull(movingDef);
            if (movingHull == null) { return true; }   // 形状を取得できない場合は判定しない

            regionScratch.Clear();
            for (int i = 0; i < obstacles.Count; i++)
            {
                var region = GetMinkowskiRegion(movingDef, obstacles[i].def);
                if (region == null) { continue; }
                regionScratch.Add((region, obstacles[i].center));
            }
            return IceGeometry.ResolvePlacement(
                desired, IceGeometry.Circumradius(movingHull), regionScratch, out resolved);
        }

        /// <summary>プレハブのメッシュ頂点を XZ 平面へ投影した凸包 (partId でキャッシュ)。</summary>
        private static Vector2[] GetHull(StagePartDefinition def)
        {
            if (def == null || def.prefab == null || string.IsNullOrEmpty(def.id)) { return null; }
            if (hullCache.TryGetValue(def.id, out var cached)) { return cached; }

            pointScratch.Clear();
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
                        pointScratch.Add(new Vector2(p.x, p.z));
                    }
                }
                else
                {
                    // Read/Write 無効で頂点が読めない場合はバウンズの角で保守的に近似
                    Bounds b = mesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 corner = new Vector3(
                            (i & 1) == 0 ? b.min.x : b.max.x,
                            (i & 2) == 0 ? b.min.y : b.max.y,
                            (i & 4) == 0 ? b.min.z : b.max.z);
                        Vector3 p = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                        pointScratch.Add(new Vector2(p.x, p.z));
                    }
                }
            }

            var hull = IceGeometry.ConvexHull(pointScratch);
            hullCache[def.id] = hull;   // null もキャッシュして再計算を避ける
            return hull;
        }

        private static Vector2[] GetMinkowskiRegion(
            StagePartDefinition movingDef, StagePartDefinition obstacleDef)
        {
            if (movingDef == null || obstacleDef == null) { return null; }
            string key = movingDef.id + "|" + obstacleDef.id;
            if (minkowskiCache.TryGetValue(key, out var cached)) { return cached; }

            var movingHull = GetHull(movingDef);
            var obstacleHull = GetHull(obstacleDef);
            Vector2[] region = (movingHull != null && obstacleHull != null)
                ? IceGeometry.MinkowskiRegion(movingHull, obstacleHull)
                : null;
            minkowskiCache[key] = region;
            return region;
        }
    }
}
