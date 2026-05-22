using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// パレット項目のサムネイル用に、各パーツの 3D プレビューを RenderTexture に描き出すヘルパー。
    /// メインステージから遠く離れた位置に Z 方向に並べて、各カメラの near/far を絞ることで
    /// 隣接パーツが映り込まないようにする。
    /// </summary>
    public static class PalettePreviewRenderer
    {
        // プレビュー用に隔離するレイヤー (Unity のデフォルトでは 30 / 31 は通常未使用)
        private const int PreviewLayer = 30;

        // メインステージから十分離れた位置でレンダリングする
        private static readonly Vector3 PreviewRigOrigin = new Vector3(2000f, 0f, 0f);
        // Z 方向に大きく離してパーツ同士が干渉しないようにする
        private const float SpacingZ = 100f;
        private const int TexSize = 128;

        public class PreviewHandle
        {
            public string Id;
            public RenderTexture Texture;
            public Transform Target;
            public Camera Camera;
            public float HoverAmount;
        }

        private static GameObject rigRoot;
        private static readonly Dictionary<string, PreviewHandle> cache = new Dictionary<string, PreviewHandle>();

        public static RenderTexture GetPreview(StagePartDefinition def)
        {
            return GetPreviewHandle(def)?.Texture;
        }

        public static PreviewHandle GetPreviewHandle(StagePartDefinition def)
        {
            if (def == null || def.prefab == null) { return null; }
            if (cache.TryGetValue(def.id, out var handle) && handle?.Texture != null) { return handle; }
            return RenderPart(def);
        }

        public static void AnimatePreview(string id, bool hovered, float deltaTime)
        {
            if (string.IsNullOrEmpty(id)) { return; }
            if (!cache.TryGetValue(id, out var handle) || handle == null) { return; }

            float target = hovered ? 1f : 0f;
            handle.HoverAmount = Mathf.MoveTowards(handle.HoverAmount, target, deltaTime * 8f);
            if (handle.Target != null && handle.HoverAmount > 0.001f)
            {
                handle.Target.Rotate(Vector3.up, 90f * deltaTime * handle.HoverAmount, Space.World);
            }
            if (handle.Camera != null && handle.Texture != null)
            {
                handle.Camera.Render();
            }
        }

        public static void Cleanup()
        {
            if (rigRoot != null)
            {
                Object.Destroy(rigRoot);
                rigRoot = null;
            }
            foreach (var kv in cache)
            {
                if (kv.Value?.Texture != null)
                {
                    kv.Value.Texture.Release();
                    Object.Destroy(kv.Value.Texture);
                }
            }
            cache.Clear();
        }

        private static PreviewHandle RenderPart(StagePartDefinition def)
        {
            EnsureRig();
            int index = cache.Count;

            Vector3 anchor = PreviewRigOrigin + new Vector3(0f, 0f, index * SpacingZ);

            // パーツを配置
            var inst = Object.Instantiate(def.prefab, anchor + def.spawnOffset, Quaternion.identity, rigRoot.transform);
            inst.name = "Preview_" + def.id;
            DisableGameLogic(inst);
            SetLayerRecursive(inst, PreviewLayer);

            // バウンディングボックス (Renderer 由来)。極端に大きい場合は丸める。
            Bounds b = ComputeBounds(inst, anchor);
            float radius = Mathf.Clamp(b.extents.magnitude, 0.4f, 4f);

            // 専用カメラ
            var camGo = new GameObject("PreviewCam_" + def.id);
            camGo.transform.SetParent(rigRoot.transform, false);
            // 視点ベクトル: 上から斜めに 3D 感を出す
            Vector3 dir = new Vector3(0.6f, 0.7f, -1f).normalized;
            float camDist = radius * 3.2f;
            camGo.transform.position = b.center + dir * camDist;
            camGo.transform.LookAt(b.center);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 35f;
            // near/far はターゲット周りだけにすることで、Z 方向に並んだ隣のパーツを描画しない
            cam.nearClipPlane = Mathf.Max(0.05f, camDist - radius - 1f);
            cam.farClipPlane  = camDist + radius + 1f;
            cam.cullingMask = 1 << PreviewLayer;
            cam.depth = -100;

            var rt = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
            rt.name = "PaletteRT_" + def.id;
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();
            // 一度だけレンダリングしたら無効化 (パフォーマンス節約)
            cam.enabled = false;

            var handle = new PreviewHandle
            {
                Id = def.id,
                Texture = rt,
                Target = inst.transform,
                Camera = cam,
                HoverAmount = 0f
            };
            cache[def.id] = handle;
            return handle;
        }

        private static void EnsureRig()
        {
            if (rigRoot != null) { return; }
            rigRoot = new GameObject("PalettePreviewRig");

            // プレビュー専用ライト
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(rigRoot.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            light.cullingMask = 1 << PreviewLayer;
        }

        private static Bounds ComputeBounds(GameObject go, Vector3 fallback)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(fallback, Vector3.one * 1.5f);
            }
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                // ParticleSystem の bounds は無限のような巨大値をとることがあるので除外
                if (renderers[i] is ParticleSystemRenderer) { continue; }
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        private static void DisableGameLogic(GameObject root)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                mb.enabled = false;
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var rb2 in root.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb2.bodyType = RigidbodyType2D.Kinematic;
                rb2.simulated = false;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
