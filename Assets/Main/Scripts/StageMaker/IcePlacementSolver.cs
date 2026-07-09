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
            public float rotationY;  // placement.rotationY
            public StagePartDefinition def;
        }

        // partId → XZ 凸包 (アンカー原点基準、回転なし)
        private static readonly Dictionary<string, Vector2[]> hullCache = new();
        // "partId@回転キー" → 回転済み凸包
        private static readonly Dictionary<string, Vector2[]> rotatedHullCache = new();
        // "movingId@回転|obstacleId@回転" → ミンコフスキー領域
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
                    rotationY = part.placement.rotationY,
                    def = part.definition,
                });
            }
        }

        /// <summary>
        /// 望みの位置 desired を「どの氷とも重ならず、近ければ辺が接する」位置に解決する。
        /// movingRotationY は移動パーツの Y 軸回転 (度)。
        /// 戻り値 false は「desired 自体が重なっていて、置ける位置も見つからない」場合のみ。
        /// </summary>
        public static bool TryResolve(
            StagePartDefinition movingDef, float movingRotationY, Vector2 desired,
            IReadOnlyList<PlacedIce> obstacles, out Vector2 resolved)
        {
            resolved = desired;
            if (obstacles == null || obstacles.Count == 0) { return true; }

            int movingRotKey = RotationKey(movingRotationY);
            Vector2[] movingHull = GetRotatedHull(movingDef, movingRotKey);
            if (movingHull == null) { return true; }   // 形状を取得できない場合は判定しない

            regionScratch.Clear();
            for (int i = 0; i < obstacles.Count; i++)
            {
                var region = GetMinkowskiRegion(
                    movingDef, movingRotKey,
                    obstacles[i].def, RotationKey(obstacles[i].rotationY));
                if (region == null) { continue; }
                regionScratch.Add((region, obstacles[i].center));
            }
            return IceGeometry.ResolvePlacement(
                desired, IceGeometry.Circumradius(movingHull), regionScratch, out resolved);
        }

        /// <summary>回転角を 0.1° 単位に量子化したキャッシュキー。</summary>
        private static int RotationKey(float rotationY)
        {
            return Mathf.RoundToInt(Mathf.Repeat(rotationY, 360f) * 10f) % 3600;
        }

        /// <summary>回転済み凸包 ("partId@回転キー" でキャッシュ)。</summary>
        private static Vector2[] GetRotatedHull(StagePartDefinition def, int rotKey)
        {
            if (rotKey == 0) { return GetHull(def); }
            if (def == null || string.IsNullOrEmpty(def.id)) { return null; }

            string key = def.id + "@" + rotKey;
            if (rotatedHullCache.TryGetValue(key, out var cached)) { return cached; }

            var baseHull = GetHull(def);
            Vector2[] rotated = null;
            if (baseHull != null)
            {
                // Unity の Y 軸回転 (+Z が +X へ倒れる向き) を XZ 平面 (x, z)→(x, y) に適用
                float rad = rotKey * 0.1f * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                rotated = new Vector2[baseHull.Length];
                for (int i = 0; i < baseHull.Length; i++)
                {
                    Vector2 v = baseHull[i];
                    rotated[i] = new Vector2(v.x * cos + v.y * sin, -v.x * sin + v.y * cos);
                }
            }
            rotatedHullCache[key] = rotated;
            return rotated;
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
            StagePartDefinition movingDef, int movingRotKey,
            StagePartDefinition obstacleDef, int obstacleRotKey)
        {
            if (movingDef == null || obstacleDef == null) { return null; }
            string key = movingDef.id + "@" + movingRotKey + "|" + obstacleDef.id + "@" + obstacleRotKey;
            if (minkowskiCache.TryGetValue(key, out var cached)) { return cached; }

            var movingHull = GetRotatedHull(movingDef, movingRotKey);
            var obstacleHull = GetRotatedHull(obstacleDef, obstacleRotKey);
            Vector2[] region = (movingHull != null && obstacleHull != null)
                ? IceGeometry.MinkowskiRegion(movingHull, obstacleHull)
                : null;
            minkowskiCache[key] = region;
            return region;
        }
    }
}
