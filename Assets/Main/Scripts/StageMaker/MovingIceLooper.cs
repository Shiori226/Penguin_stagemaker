using System.Collections;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 単発の MovingIce 1 体分のサイクル (水中 → 浮上 → 移動 → 沈降 → 水中) を回すコンポーネント。
    /// 移動距離が長い場合に複数体を位相をずらして並走させたいので、ループ管理は 1 体ずつに分離している。
    /// CustomStageBuilder が Configure を呼んでパラメータを渡す。
    /// </summary>
    public class MovingIceLooper : MonoBehaviour
    {
        public Vector3 origin;
        public Vector3 target;
        public float speed = 4f;
        public float floatSinkDuration = 0.8f;
        public float floatSinkDistance = 1.5f;
        public float restartGap = 1.0f;

        /// <summary>サイクル全体に対する初期位相 (0..1)。複数体を等間隔にずらすために使う。</summary>
        public float startPhase;

        /// <summary>true のときは origin↔target を往復し、水中潜行を一切行わない。</summary>
        public bool surfacePingPong;

        public void Configure(Vector3 origin, Vector3 target, float speed,
            float floatSinkDuration, float floatSinkDistance, float restartGap, float startPhase,
            bool surfacePingPong = false)
        {
            this.origin = origin;
            this.target = target;
            this.speed = speed;
            this.floatSinkDuration = floatSinkDuration;
            this.floatSinkDistance = floatSinkDistance;
            this.restartGap = restartGap;
            this.startPhase = Mathf.Clamp01(startPhase);
            this.surfacePingPong = surfacePingPong;
        }

        private void Start()
        {
            StartCoroutine(LoopCycle());
        }

        private IEnumerator LoopCycle()
        {
            var col = GetComponent<Collider>();
            var renderers = GetComponentsInChildren<Renderer>(true);

            float distance = (target - origin).magnitude;
            if (distance < 0.01f) { yield break; }
            float moveTime = distance / Mathf.Max(0.1f, speed);

            if (surfacePingPong)
            {
                yield return RunSurfacePingPong(col, renderers, moveTime);
            }
            else
            {
                yield return RunSubmergedOneWay(col, renderers, moveTime);
            }
        }

        // 既存挙動: 水中で待機 → 浮上 → 移動 → 沈降 → 水中 → 待機 → 繰り返し (片道)
        private IEnumerator RunSubmergedOneWay(Collider col, Renderer[] renderers, float moveTime)
        {
            transform.position = origin - Vector3.up * floatSinkDistance;
            SetVisible(renderers, false);
            if (col != null) col.enabled = false;

            float cycle = floatSinkDuration * 2f + moveTime + restartGap;
            yield return new WaitForSeconds(startPhase * cycle);

            while (true)
            {
                Vector3 surfaceStart = origin;
                Vector3 underwaterStart = origin - Vector3.up * floatSinkDistance;
                Vector3 surfaceEnd = target;
                Vector3 underwaterEnd = target - Vector3.up * floatSinkDistance;

                // 浮上 (当たり判定 OFF)
                transform.position = underwaterStart;
                SetVisible(renderers, true);
                if (col != null) col.enabled = false;
                yield return MoveOver(underwaterStart, surfaceStart, floatSinkDuration);

                // 直線移動 (当たり判定 ON、一定速度)
                if (col != null) col.enabled = true;
                yield return MoveOver(surfaceStart, surfaceEnd, moveTime);

                // 沈降 (当たり判定 OFF)
                if (col != null) col.enabled = false;
                yield return MoveOver(surfaceEnd, underwaterEnd, floatSinkDuration);

                SetVisible(renderers, false);
                yield return new WaitForSeconds(restartGap);
            }
        }

        // 新挙動: 水上のみで origin ↔ target を往復、潜らない / 浮上もしない (常に表示・当たり判定 ON)
        private IEnumerator RunSurfacePingPong(Collider col, Renderer[] renderers, float moveTime)
        {
            transform.position = origin;
            SetVisible(renderers, true);
            if (col != null) col.enabled = true;

            float cycle = moveTime * 2f + restartGap * 2f;
            yield return new WaitForSeconds(startPhase * cycle);

            while (true)
            {
                yield return MoveOver(origin, target, moveTime);
                if (restartGap > 0f) { yield return new WaitForSeconds(restartGap); }

                yield return MoveOver(target, origin, moveTime);
                if (restartGap > 0f) { yield return new WaitForSeconds(restartGap); }
            }
        }

        private IEnumerator MoveOver(Vector3 from, Vector3 to, float duration)
        {
            transform.position = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            transform.position = to;
        }

        private static void SetVisible(Renderer[] renderers, bool visible)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
