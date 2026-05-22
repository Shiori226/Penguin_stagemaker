using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 方向性のあるパーツ (Blizzard / Seal) を CustomStage 実行時に
    /// 適切な方向 / 動きで初期化するためのランタイムヘルパ。
    /// MovingIce は CustomStageBuilder.SpawnMovingIceGroup で別途複数体を生成するため、ここでは扱わない。
    /// </summary>
    public class CustomDirectionalRuntime : MonoBehaviour
    {
        public string kind;
        public Vector3 origin;
        public Vector3 target;

        // 風 (Blizzard) の挙動 — 既存ステージのプレハブと同じ値に揃えてある
        public const float BlizzardWindStrength = 6f;
        public const float BlizzardPushValue = 0.06f;
        public static readonly Vector3 BlizzardAreaScale = new Vector3(4f, 1f, 3f);

        private void Start()
        {
            switch (kind)
            {
                case "Blizzard":
                    SetupBlizzard();
                    break;
                case "Seal":
                    SetupSeal();
                    break;
            }
        }

        private void SetupBlizzard()
        {
            var bc = GetComponent<BlizzardController>();
            if (bc == null) { return; }
            ApplyBlizzardWind(bc, origin, target);
        }

        private void SetupSeal()
        {
            var sc = GetComponent<SealController>();
            if (sc == null) { return; }
            // 始終 seal の transform.y を保つ。
            // (origin = worldPosition は地面 Y=0 で記録され、
            //  target = directionTarget はハンドル位置の Y を含むため
            //  そのまま渡すと終点で地面に潜るので、両端を body の Y に揃える)
            float bodyY = transform.position.y;
            Vector3 begin = new Vector3(origin.x, bodyY, origin.z);
            Vector3 end = new Vector3(target.x, bodyY, target.z);
            sc.OverrideWaypoints(begin, end);
        }

        /// <summary>
        /// 編集ビューからもリアルタイム適用するための public 静的ヘルパ。
        /// </summary>
        public static void ApplyBlizzardWind(BlizzardController bc, Vector3 origin, Vector3 target)
        {
            if (bc == null) { return; }
            Vector3 direction = target - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) { direction = Vector3.forward; }
            Vector3 windVel = direction.normalized * BlizzardWindStrength;
            bc.OverrideWind(windVel, BlizzardPushValue, BlizzardAreaScale);
        }
    }
}
