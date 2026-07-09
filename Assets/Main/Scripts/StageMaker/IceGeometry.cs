using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 氷スナップ用の 2D 凸多角形幾何 (純ロジック、Unity のシーンに依存しない)。
    /// 多角形は反時計回り (CCW) の頂点配列で表す。
    ///
    /// 重なり判定は「移動パーツ中心が 障害物中心 + ミンコフスキー領域 の内部にあるか」
    /// に帰着させ、スナップは領域境界への最近点射影で行う。
    /// 円近似と違い、辺同士の接触は隙間ゼロになる。
    /// </summary>
    public static class IceGeometry
    {
        // 接触からこの範囲内に近づいたら「くっつく」吸着マージン (ワールド単位)
        public const float SnapMarginWorld = 1.5f;
        // 接触辺の中央 (きれいに整列して敷き詰められる位置) に吸い付く範囲
        public const float EdgeAlignSnapWorld = 0.9f;
        public const float ContactEpsilon = 1e-3f;
        private const int MaxPushIterations = 8;
        private const float CollinearEpsilon = 1e-6f;

        private static readonly List<Vector2> intersectionScratch = new();

        /// <summary>Andrew の monotone chain による凸包 (CCW、共線点は除去)。入力リストはソートされる。</summary>
        public static Vector2[] ConvexHull(List<Vector2> points)
        {
            if (points == null || points.Count < 3) { return null; }
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var hull = new List<Vector2>(points.Count);
            // 下側 → 上側の順に構築。各パスの最後の点は次パス/全体の始点と重複するので落とす
            for (int pass = 0; pass < 2; pass++)
            {
                int guard = hull.Count;
                int begin = pass == 0 ? 0 : points.Count - 1;
                int end = pass == 0 ? points.Count : -1;
                int step = pass == 0 ? 1 : -1;
                for (int i = begin; i != end; i += step)
                {
                    Vector2 p = points[i];
                    while (hull.Count >= guard + 2
                        && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= CollinearEpsilon)
                    {
                        hull.RemoveAt(hull.Count - 1);
                    }
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull.Count >= 3 ? hull.ToArray() : null;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        /// <summary>
        /// ミンコフスキー領域 M = { b - a | a ∈ movingHull, b ∈ obstacleHull }。
        /// (移動パーツ中心 - 障害物中心) が M の内部にあるとき、両者のメッシュは重なる。
        /// </summary>
        public static Vector2[] MinkowskiRegion(Vector2[] movingHull, Vector2[] obstacleHull)
        {
            var points = new List<Vector2>(movingHull.Length * obstacleHull.Length);
            for (int i = 0; i < obstacleHull.Length; i++)
            {
                for (int j = 0; j < movingHull.Length; j++)
                {
                    points.Add(obstacleHull[i] - movingHull[j]);
                }
            }
            return ConvexHull(points);
        }

        /// <summary>凸包の外接円半径 (原点=パーツのアンカーからの最大距離)。</summary>
        public static float Circumradius(Vector2[] hull)
        {
            float best = 0f;
            for (int i = 0; i < hull.Length; i++)
            {
                float m = hull[i].magnitude;
                if (m > best) { best = m; }
            }
            return best;
        }

        /// <summary>
        /// 点 p と多角形 (poly を offset だけ平行移動したもの) の符号付き距離 (負 = 内部)。
        /// closest は境界上の最近点、closestEdge はその辺のインデックス。
        /// </summary>
        public static float SignedDistance(Vector2 p, Vector2[] poly, Vector2 offset,
            out Vector2 closest, out int closestEdge)
        {
            closest = default;
            closestEdge = 0;
            bool inside = true;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 v0 = poly[i] + offset;
                Vector2 v1 = poly[(i + 1) % poly.Length] + offset;
                Vector2 e = v1 - v0;
                if (e.x * (p.y - v0.y) - e.y * (p.x - v0.x) < 0f) { inside = false; }

                float len2 = e.x * e.x + e.y * e.y;
                float t = len2 > 1e-12f
                    ? Mathf.Clamp01(((p.x - v0.x) * e.x + (p.y - v0.y) * e.y) / len2)
                    : 0f;
                Vector2 q = v0 + e * t;
                float d2 = (p - q).sqrMagnitude;
                if (d2 < bestSqr)
                {
                    bestSqr = d2;
                    closest = q;
                    closestEdge = i;
                }
            }
            float dist = Mathf.Sqrt(bestSqr);
            return inside ? -dist : dist;
        }

        /// <summary>CCW 多角形の辺の外向き単位法線。</summary>
        public static Vector2 OutwardNormal(Vector2[] poly, int edgeIndex)
        {
            Vector2 e = poly[(edgeIndex + 1) % poly.Length] - poly[edgeIndex];
            Vector2 n = new Vector2(e.y, -e.x);
            float m = n.magnitude;
            return m > 1e-9f ? n / m : Vector2.right;
        }

        /// <summary>p がどの領域にも (許容誤差を超えて) 食い込んでいないか。</summary>
        public static bool IsClear(Vector2 p, IReadOnlyList<(Vector2[] poly, Vector2 offset)> regions)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                if (SignedDistance(p, regions[i].poly, regions[i].offset, out _, out _) < -ContactEpsilon)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>2つの多角形境界の交点を results に追加する。</summary>
        public static void BoundaryIntersections(
            Vector2[] polyA, Vector2 offsetA, Vector2[] polyB, Vector2 offsetB, List<Vector2> results)
        {
            for (int i = 0; i < polyA.Length; i++)
            {
                Vector2 a0 = polyA[i] + offsetA;
                Vector2 a1 = polyA[(i + 1) % polyA.Length] + offsetA;
                for (int j = 0; j < polyB.Length; j++)
                {
                    Vector2 b0 = polyB[j] + offsetB;
                    Vector2 b1 = polyB[(j + 1) % polyB.Length] + offsetB;
                    if (TrySegmentIntersection(a0, a1, b0, b1, out Vector2 hit))
                    {
                        results.Add(hit);
                    }
                }
            }
        }

        private static bool TrySegmentIntersection(
            Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out Vector2 hit)
        {
            hit = default;
            Vector2 r = a1 - a0;
            Vector2 s = b1 - b0;
            float denom = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(denom) < 1e-9f) { return false; }   // 平行 (共線の重なりは交点扱いしない)
            Vector2 qp = b0 - a0;
            float t = (qp.x * s.y - qp.y * s.x) / denom;
            float u = (qp.x * r.y - qp.y * r.x) / denom;
            if (t < -1e-4f || t > 1f + 1e-4f || u < -1e-4f || u > 1f + 1e-4f) { return false; }
            hit = a0 + r * Mathf.Clamp01(t);
            return true;
        }

        /// <summary>
        /// 望みの位置 desired を「どの領域にも重ならず、近ければ境界に接する」位置に解決する。
        /// regions は (障害物ごとのミンコフスキー領域, その障害物中心) のリスト。
        /// 戻り値 false は「desired 自体が重なっていて、置ける位置も見つからない」場合のみ。
        /// </summary>
        public static bool ResolvePlacement(
            Vector2 desired, float movingCircumradius,
            IReadOnlyList<(Vector2[] poly, Vector2 offset)> regions,
            out Vector2 resolved)
        {
            resolved = desired;
            if (regions == null || regions.Count == 0) { return true; }

            // 最も食い込みの深い (符号付き距離最小の) 領域を探す
            int nearestIndex = 0;
            float minSigned = float.MaxValue;
            Vector2 nearestClosest = default;
            int nearestEdge = 0;
            for (int i = 0; i < regions.Count; i++)
            {
                float sd = SignedDistance(desired, regions[i].poly, regions[i].offset,
                    out Vector2 closest, out int edge);
                if (sd < minSigned)
                {
                    minSigned = sd;
                    nearestIndex = i;
                    nearestClosest = closest;
                    nearestEdge = edge;
                }
            }
            if (minSigned > SnapMarginWorld) { return true; }   // 干渉なし: 自由配置

            bool wasOverlapping = minSigned < -ContactEpsilon;
            var (nearPoly, nearOffset) = regions[nearestIndex];

            // 接触辺の中央 (辺同士が整列して敷き詰められる位置) に近ければそちらへ吸着。
            // 微小な辺 (面取り等) では発動しないよう、範囲を辺の長さに応じて絞る
            {
                Vector2 v0 = nearPoly[nearestEdge] + nearOffset;
                Vector2 v1 = nearPoly[(nearestEdge + 1) % nearPoly.Length] + nearOffset;
                Vector2 mid = (v0 + v1) * 0.5f;
                float alignRange = Mathf.Min(EdgeAlignSnapWorld, (v1 - v0).magnitude * 0.35f);
                if ((nearestClosest - mid).magnitude <= alignRange && IsClear(mid, regions))
                {
                    resolved = mid;
                    return true;
                }
            }

            // 一次スナップ: 境界最近点 (= 最小移動で接触する位置)
            if (IsClear(nearestClosest, regions))
            {
                resolved = nearestClosest;
                return true;
            }

            // 2領域同時接触: 最近傍領域の境界と、それを侵す他領域の境界の交点 (V字の谷) を試す
            {
                Vector2 best = default;
                float bestDist = float.MaxValue;
                bool found = false;
                for (int i = 0; i < regions.Count; i++)
                {
                    if (i == nearestIndex) { continue; }
                    float sd = SignedDistance(nearestClosest, regions[i].poly, regions[i].offset, out _, out _);
                    if (sd >= -ContactEpsilon) { continue; }

                    intersectionScratch.Clear();
                    BoundaryIntersections(nearPoly, nearOffset,
                        regions[i].poly, regions[i].offset, intersectionScratch);
                    for (int k = 0; k < intersectionScratch.Count; k++)
                    {
                        Vector2 p = intersectionScratch[k];
                        float dist = (p - desired).magnitude;
                        if (dist < bestDist && IsClear(p, regions))
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
            }

            // フォールバック: 食い込みが残る領域から順に境界へ押し出す
            {
                Vector2 pos = desired;
                for (int k = 0; k < MaxPushIterations; k++)
                {
                    int deepestIndex = -1;
                    float deepest = -ContactEpsilon;
                    Vector2 deepestClosest = default;
                    int deepestEdge = 0;
                    for (int i = 0; i < regions.Count; i++)
                    {
                        float sd = SignedDistance(pos, regions[i].poly, regions[i].offset,
                            out Vector2 closest, out int edge);
                        if (sd < deepest)
                        {
                            deepest = sd;
                            deepestIndex = i;
                            deepestClosest = closest;
                            deepestEdge = edge;
                        }
                    }
                    if (deepestIndex < 0) { break; }
                    Vector2 outward = OutwardNormal(regions[deepestIndex].poly, deepestEdge);
                    pos = deepestClosest + outward * ContactEpsilon;
                }
                // 変位キャップで遠方への飛び (テレポート) を防ぐ
                if (IsClear(pos, regions)
                    && (pos - desired).magnitude <= 2.5f * movingCircumradius + SnapMarginWorld)
                {
                    resolved = pos;
                    return true;
                }
            }

            // 吸着だけ失敗したケースは元位置が合法なので素通し
            if (!wasOverlapping)
            {
                resolved = desired;
                return true;
            }
            return false;
        }
    }
}
