using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// 編集ビュー。3D の世界に置かれた実際のパーツプレハブを掴んでドラッグ移動できる。
    /// 左にパーツパレット、上部にステージ名 / 保存 / プレイのボタンを表示する。
    /// </summary>
    public class StageMakerEditorView : MonoBehaviour
    {
        public const string EraserId = "__eraser__";
        private const float PaletteWidth = 640f;
        private const float StageCameraXOffset = -22f;

        // パレットに表示しない (= ユーザが配置できない) 内部パーツ
        // Start / Goal は固定位置・Shark は周辺の海に自動配置
        private static readonly HashSet<string> InternalPartIds = new HashSet<string>
        {
            "PlatformStart", "PlatformGoal", "Shark"
        };

        private StageMakerSceneController controller;
        private StagePartCatalog catalog;
        private CustomStageData currentData;

        private InputField nameField;
        private GameObject paletteContent;
        private readonly Dictionary<string, Image> paletteSelectionFrame = new();
        private readonly List<RaycastResult> uiRaycastResults = new();
        private bool eraserMode;
        private string selectedPartId;   // クリックで配置するためのカレント選択
        public bool IsEraserMode => eraserMode;

        private Camera editorCamera;
        private GameObject sceneRoot;       // 3D シーンの親 (light, ground, parts)
        private Transform partsRoot;        // 配置パーツの親

        // 地面プレーンと衝突するレイヤー (デフォルトレイヤーで十分)
        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        public void Initialize(StageMakerSceneController c)
        {
            controller = c;
            catalog = StagePartCatalog.Load();
            // パレットの3Dプレビュー用に先にシーンを準備しておく
            EnsureSceneInfra();
            BuildLayout();
        }

        public void LoadStage(CustomStageData data)
        {
            currentData = data;
            if (nameField != null) { nameField.text = data.displayName; }
            RebuildScene();
        }

        // ========== UI 構築 ==========

        private void BuildLayout()
        {
            // ヘッダ (戻る + ステージ名 + 保存 + プレイ)
            var header = StageMakerUIFactory.AddRect(gameObject, "Header",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -102), new Vector2(-32, -16)).gameObject;
            StageMakerUIFactory.AddPanelImage(header, new Color(1f, 1f, 1f, 0.6f));

            var backGo = new GameObject("BackButton", typeof(RectTransform));
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.SetParent(header.transform, false);
            backRt.anchorMin = new Vector2(0, 0.5f);
            backRt.anchorMax = new Vector2(0, 0.5f);
            backRt.sizeDelta = new Vector2(76, 70);
            backRt.anchoredPosition = new Vector2(70, 5);
            var backHit = backGo.AddComponent<Image>();
            backHit.color = new Color(1f, 1f, 1f, 0f);
            backHit.raycastTarget = true;
            var backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backHit;
            var backLabel = StageMakerUIFactory.CreateText(backGo, "Label", "←",
                52, StageMakerUIFactory.IceText, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            backLabel.fontStyle = FontStyle.Bold;
            backBtn.onClick.AddListener(() => controller.ShowList());

            // 名前入力
            var nameGo = new GameObject("NameField", typeof(RectTransform));
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(header.transform, false);
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.anchoredPosition = new Vector2(420, 0);
            nameRt.sizeDelta = new Vector2(420, 58);
            var nameImg = nameGo.AddComponent<Image>();
            nameImg.color = new Color(0.90f, 0.99f, 1f, 0.48f);
            nameField = nameGo.AddComponent<InputField>();

            var nameTextGo = new GameObject("Text", typeof(RectTransform));
            var nameTextRt = nameTextGo.GetComponent<RectTransform>();
            nameTextRt.SetParent(nameGo.transform, false);
            nameTextRt.anchorMin = Vector2.zero;
            nameTextRt.anchorMax = Vector2.one;
            nameTextRt.offsetMin = new Vector2(12, 6);
            nameTextRt.offsetMax = new Vector2(-12, -6);
            var nameText = nameTextGo.AddComponent<Text>();
            nameText.font = StageMakerUIFactory.GetFont();
            nameText.fontSize = 22;
            nameText.color = Color.black;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameField.targetGraphic = nameImg;
            nameField.textComponent = nameText;
            nameField.onValueChanged.AddListener(v =>
            {
                if (currentData != null) { currentData.displayName = string.IsNullOrEmpty(v) ? "Untitled" : v; }
            });

            // 保存ボタン
            var (saveGo, saveBtn, _) = StageMakerUIFactory.CreateButton(header, "SaveButton",
                "SAVE", Color.white, StageMakerUIFactory.IceText, new Vector2(132, 52));
            var saveRt = saveGo.GetComponent<RectTransform>();
            saveRt.anchorMin = new Vector2(1, 0.5f);
            saveRt.anchorMax = new Vector2(1, 0.5f);
            saveRt.anchoredPosition = new Vector2(-286, 0);
            saveBtn.onClick.AddListener(SaveCurrent);

            // プレイ (即反映) ボタン
            var (playGo, playBtn, _) = StageMakerUIFactory.CreateButton(header, "PlayButton",
                "PLAY", Color.white, StageMakerUIFactory.IceText, new Vector2(140, 52));
            var playRt = playGo.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(1, 0.5f);
            playRt.anchorMax = new Vector2(1, 0.5f);
            playRt.anchoredPosition = new Vector2(-128, 0);
            playBtn.onClick.AddListener(SaveAndPlay);

            // パレット
            BuildPalette();

            // クリアボタンは置かず、パーツパレットに縦の余白を回す。
        }

        private void BuildPalette()
        {
            var paletteGo = new GameObject("Palette", typeof(RectTransform));
            var paletteRt = paletteGo.GetComponent<RectTransform>();
            paletteRt.SetParent(transform, false);
            paletteRt.anchorMin = new Vector2(0, 0);
            paletteRt.anchorMax = new Vector2(0, 1);
            paletteRt.pivot = new Vector2(0, 0.5f);
            paletteRt.offsetMin = new Vector2(24, 24);
            paletteRt.offsetMax = new Vector2(0, -120);
            paletteRt.sizeDelta = new Vector2(PaletteWidth, paletteRt.sizeDelta.y);
            var paletteImg = paletteGo.AddComponent<Image>();
            StageMakerUIFactory.StylePanelImage(paletteImg, new Color(1f, 1f, 1f, 0.6f));

            var scrollGo = new GameObject("PartScrollView", typeof(RectTransform));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(paletteGo.transform, false);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(28, 36);
            scrollRt.offsetMax = new Vector2(-28, -36);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollGo.AddComponent<RectMask2D>();

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollGo.transform, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            scrollRect.viewport = viewportRt;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            paletteContent = contentGo;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportGo.transform, false);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            scrollRect.content = contentRt;

            var vlayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(8, 8, 8, 8);
            vlayout.spacing = 18;
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childForceExpandWidth = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var visibleParts = new List<StagePartDefinition>();
            if (catalog != null)
            {
                foreach (var def in catalog.Parts)
                {
                    if (def == null) continue;
                    if (InternalPartIds.Contains(def.id)) { continue; }
                    visibleParts.Add(def);
                }
            }

            for (int i = 0; i < visibleParts.Count; i += 3)
            {
                int remaining = visibleParts.Count - i;
                var row = CreatePaletteRow();
                CreatePartRow(row, visibleParts[i]);

                if (remaining >= 2)
                {
                    CreatePartRow(row, visibleParts[i + 1]);
                }
                if (remaining >= 3)
                {
                    CreatePartRow(row, visibleParts[i + 2]);
                }
                else
                {
                    BuildEraserSlot(row, columns: Mathf.Max(1, 3 - remaining));
                }
            }

            if (visibleParts.Count % 3 == 0)
            {
                var row = CreatePaletteRow();
                BuildEraserSlot(row, columns: 3);
            }
        }

        private GameObject CreatePaletteRow()
        {
            var rowGo = new GameObject("PaletteRow", typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(paletteContent.transform, false);
            rowRt.sizeDelta = new Vector2(0, 150);

            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rowGo;
        }

        private void BuildEraserSlot(GameObject parent, int columns)
        {
            var rowGo = new GameObject("EraserSlot", typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent.transform, false);
            rowRt.sizeDelta = new Vector2(176 * columns + 16 * (columns - 1), 150);

            var bgImg = rowGo.AddComponent<Image>();
            StageMakerUIFactory.StyleButtonImage(bgImg, Color.white);
            var button = rowGo.AddComponent<Button>();
            button.targetGraphic = bgImg;

            // 選択枠
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.SetParent(rowGo.transform, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.color = new Color(1, 1, 1, 0);
            frameImg.raycastTarget = false;
            paletteSelectionFrame[EraserId] = frameImg;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(rowGo.transform, false);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(82, 82);
            iconRt.anchoredPosition = Vector2.zero;
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = StageMakerUIFactory.GetEraserIconSprite();
            icon.color = StageMakerUIFactory.IceText;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var handler = rowGo.AddComponent<PaletteDragHandler>();
            handler.Initialize(this, EraserId);
        }

        private void CreatePartRow(GameObject parent, StagePartDefinition def)
        {
            var rowGo = new GameObject(def.id, typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent.transform, false);
            rowRt.sizeDelta = new Vector2(176, 150);

            var bgImg = rowGo.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0f);
            bgImg.raycastTarget = true;

            // 選択枠
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.SetParent(rowGo.transform, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.color = new Color(1, 1, 1, 0);
            frameImg.raycastTarget = false;
            paletteSelectionFrame[def.id] = frameImg;

            // 3Dプレビューサムネイル
            var thumbGo = new GameObject("Thumb", typeof(RectTransform));
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.SetParent(rowGo.transform, false);
            thumbRt.anchorMin = new Vector2(0, 0.28f);
            thumbRt.anchorMax = new Vector2(1, 1);
            thumbRt.offsetMin = new Vector2(6, 6);
            thumbRt.offsetMax = new Vector2(-6, -4);
            var rawImage = thumbGo.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            var preview = PalettePreviewRenderer.GetPreviewHandle(def);
            if (preview?.Texture != null)
            {
                rawImage.texture = preview.Texture;
            }
            else
            {
                // フォールバック (色塗りつぶし)
                Destroy(rawImage);
                var img = thumbGo.AddComponent<Image>();
                img.color = def.paletteColor;
                img.raycastTarget = false;
            }

            // ラベル
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rowGo.transform, false);
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0.27f);
            labelRt.offsetMin = new Vector2(6, 0);
            labelRt.offsetMax = new Vector2(-6, -2);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = StageMakerUIFactory.GetFont();
            labelText.text = def.displayName.ToUpperInvariant();
            labelText.fontSize = 15;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = StageMakerUIFactory.IceText;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 10;
            labelText.resizeTextMaxSize = 18;

            var handler = rowGo.AddComponent<PaletteDragHandler>();
            handler.Initialize(this, def.id, thumbRt);
        }

        // ========== パレット選択 ==========

        public void SelectPaletteItem(string id)
        {
            eraserMode = (id == EraserId);
            selectedPartId = eraserMode ? null : id;
            foreach (var kv in paletteSelectionFrame)
            {
                kv.Value.color = (kv.Key == id)
                    ? new Color(1, 1, 1, 0.16f)
                    : new Color(1, 1, 1, 0);
            }
        }

        // ========== 3D シーン構築 ==========

        private void EnsureSceneInfra()
        {
            if (sceneRoot != null) return;

            sceneRoot = new GameObject("StageMakerScene");

            // カメラ: 既存の Main Camera を流用
            editorCamera = Camera.main;
            if (editorCamera == null)
            {
                var camGo = new GameObject("EditorCamera");
                camGo.tag = "MainCamera";
                editorCamera = camGo.AddComponent<Camera>();
            }
            ConfigureCamera();

            // ライト
            var lightGo = new GameObject("DirectionalLight");
            lightGo.transform.SetParent(sceneRoot.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = Color.white;

            // 地面プレーン (水面風) — 視覚的なガイド
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "EditorGround";
            ground.transform.SetParent(sceneRoot.transform, false);
            ground.transform.position = new Vector3(0, -0.05f, 35f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200x200
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            // InGame と同じ海面マテリアルを使用する
            var seaMat = Resources.Load<Material>("StageMaker/Sea");
            if (seaMat != null)
            {
                groundRenderer.material = seaMat;
            }
            else
            {
                // フォールバック: 海色の単色マテリアル
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.20f, 0.45f, 0.65f, 1f);
                groundRenderer.material = mat;
            }

            // パーツ親
            var partsGo = new GameObject("Parts");
            partsGo.transform.SetParent(sceneRoot.transform, false);
            partsRoot = partsGo.transform;

            // ステージ範囲ガイド (ワイヤー)
            DrawBoundsGuide(sceneRoot.transform);
        }

        private void ConfigureCamera()
        {
            if (editorCamera == null) return;
            editorCamera.transform.position = new Vector3(StageCameraXOffset, 60f, 30f);
            editorCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            editorCamera.orthographic = true;
            editorCamera.orthographicSize = 38f;
            editorCamera.nearClipPlane = 0.1f;
            editorCamera.farClipPlane = 200f;
            editorCamera.clearFlags = CameraClearFlags.SolidColor;
            editorCamera.backgroundColor = StageMakerUIFactory.TitleBlue;
            // パレットプレビュー用のレイヤー (30) はエディタカメラに映さない
            editorCamera.cullingMask &= ~(1 << 30);
        }

        private static void DrawBoundsGuide(Transform parent)
        {
            // 中央線 (Z 方向) の薄いラインを描く
            var guide = new GameObject("BoundsGuide");
            guide.transform.SetParent(parent, false);
            var lr = guide.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.3f);
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(0, 0.02f, 0));
            lr.SetPosition(1, new Vector3(0, 0.02f, 80));
        }

        public void RebuildScene()
        {
            EnsureSceneInfra();

            // 既存パーツを破棄
            if (partsRoot != null)
            {
                for (int i = partsRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(partsRoot.GetChild(i).gameObject);
                }
            }

            if (currentData == null || catalog == null) return;

            // 固定 Start / Goal をビジュアルとして表示 (DraggablePart を付けないので操作対象にならない)
            SpawnLockedFixedPart("PlatformStart", CustomStageBuilder.FixedStartPosition);
            SpawnLockedFixedPart("PlatformGoal", CustomStageBuilder.FixedGoalPosition);

            foreach (var p in currentData.parts)
            {
                SpawnPlacementInScene(p);
            }
        }

        /// <summary>
        /// 固定位置に置かれた Start/Goal を編集ビュー上に表示する (操作不可)。
        /// </summary>
        private void SpawnLockedFixedPart(string id, Vector3 worldPos)
        {
            var def = catalog.Find(id);
            if (def == null || def.prefab == null) { return; }
            Vector3 pos = worldPos + def.spawnOffset;
            var go = Instantiate(def.prefab, pos, Quaternion.identity, partsRoot);
            go.name = def.id + "_Locked";
            DisableGameLogicForEditor(go);
            // DraggablePart は付けないので FindPartUnderCursor の対象外 = ドラッグ不可
        }

        private GameObject SpawnPlacementInScene(CustomStagePartPlacement p)
        {
            var def = catalog.Find(p.partId);
            if (def == null || def.prefab == null) return null;

            Vector3 pos = p.worldPosition + def.spawnOffset;
            Quaternion rot = Quaternion.Euler(0f, p.rotationY, 0f);
            var go = Instantiate(def.prefab, pos, rot, partsRoot);
            go.name = def.id;

            // 編集中はゲームロジック (Controller類) を走らせない。
            DisableGameLogicForEditor(go);

            // 入力ハンドリングはエディタの Update() で行うので Collider 追加は不要。
            var dragger = go.AddComponent<DraggablePart>();
            dragger.Initialize(p, def);

            // 方向性パーツ: 方向ハンドルと結ぶ線をスポーン
            if (def.isDirectional)
            {
                EnsureDirectionTarget(p);
                SpawnDirectionHandle(dragger);
            }
            return go;
        }

        private static void EnsureDirectionTarget(CustomStagePartPlacement p)
        {
            // 既定の方向ターゲット = 配置位置 + Z+5
            if (p.directionTarget == Vector3.zero)
            {
                p.directionTarget = p.worldPosition + new Vector3(0f, 0f, 5f);
            }
        }

        // Blizzard ハンドルは固定半径 (回転のみ)
        public const float BlizzardHandleRadius = 4.0f;

        private void SpawnDirectionHandle(DraggablePart owner)
        {
            GameObject handleGo;
            string kind = owner.definition.directionalKind;

            if (kind == "MovingIce" || kind == "MovingIcePingPong" || kind == "Seal")
            {
                // 移動先プレビュー: パーツの prefab そのものを半透明で配置
                handleGo = Instantiate(owner.definition.prefab, owner.placement.directionTarget + owner.definition.spawnOffset, Quaternion.identity, partsRoot);
                handleGo.name = owner.definition.id + "_GhostEnd";
                DisableGameLogicForEditor(handleGo);
                ApplyTransparency(handleGo, 0.35f);
            }
            else if (kind == "Blizzard")
            {
                // Blizzard: 半径固定の小さな矢印アイコン (= スフィア + ライン)
                handleGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                handleGo.name = owner.definition.id + "_Handle";
                handleGo.transform.SetParent(partsRoot, false);
                handleGo.transform.localScale = Vector3.one * 0.9f;
                var col = handleGo.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                var renderer = handleGo.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.85f, 1.0f, 1f);
                renderer.material = mat;

                // Blizzard の方向ハンドルは固定半径の円周上に置く
                Vector3 ownerPos = owner.transform.position;
                Vector3 dir = (owner.placement.directionTarget - ownerPos);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) { dir = Vector3.forward; }
                handleGo.transform.position = ownerPos + dir.normalized * BlizzardHandleRadius;
                owner.placement.directionTarget = handleGo.transform.position;
            }
            else
            {
                // 想定外: フォールバックで小さなスフィア
                handleGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                handleGo.name = owner.definition.id + "_Handle";
                handleGo.transform.SetParent(partsRoot, false);
                handleGo.transform.position = owner.placement.directionTarget;
                handleGo.transform.localScale = Vector3.one * 1.2f;
                var col = handleGo.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }

            var handleDp = handleGo.AddComponent<DraggablePart>();
            handleDp.isHandle = true;
            handleDp.placement = owner.placement;
            handleDp.definition = owner.definition;
            handleDp.partner = owner;
            owner.partner = handleDp;

            // 配置直後から見た目に反映する (特に風の向き)
            if (kind == "Blizzard")
            {
                ApplyBlizzardWindLive(owner);
            }

            // 接続線
            var lineGo = new GameObject("Link");
            lineGo.transform.SetParent(owner.transform, false);
            var lr = lineGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.18f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.3f, 1.0f, 0.4f, 0.9f);
            lr.endColor = new Color(0.3f, 1.0f, 0.4f, 0.4f);
            lr.positionCount = 2;
            owner.linkLine = lr;
            UpdateLinkLine(owner);
        }

        public static void UpdateLinkLine(DraggablePart owner)
        {
            if (owner == null || owner.linkLine == null || owner.partner == null) return;
            owner.linkLine.SetPosition(0, owner.transform.position + new Vector3(0f, 0.5f, 0f));
            owner.linkLine.SetPosition(1, owner.partner.transform.position);
        }

        /// <summary>
        /// 編集ビュー上でも風向き / エフェクトをリアルタイムに反映するためのヘルパ。
        /// ※ BlizzardController が disabled の状態でも、public メソッド呼び出しは行えて
        ///    内部の ParticleSystem への反映自体は走るのでそのまま使える。
        /// </summary>
        public static void ApplyBlizzardWindLive(DraggablePart body)
        {
            if (body == null || body.placement == null) { return; }
            var bc = body.GetComponent<BlizzardController>();
            if (bc == null) { return; }
            CustomDirectionalRuntime.ApplyBlizzardWind(bc, body.placement.worldPosition, body.placement.directionTarget);
        }

        /// <summary>
        /// Blizzard 系のハンドルは「回転のみ」を許可するので、
        /// オーナー (本体) を中心とした固定半径の円周上に拘束する。
        /// それ以外のパーツは入力された位置をそのまま返す。
        /// </summary>
        public static Vector3 ConstrainHandlePosition(DraggablePart handle, Vector3 desired)
        {
            if (handle == null || handle.partner == null || handle.definition == null) { return desired; }
            if (handle.definition.directionalKind != "Blizzard") { return desired; }

            Vector3 ownerPos = handle.partner.transform.position;
            Vector3 dir = desired - ownerPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) { dir = Vector3.forward; }
            Vector3 constrained = ownerPos + dir.normalized * BlizzardHandleRadius;
            constrained.y = ownerPos.y; // 高さも本体に合わせる
            return constrained;
        }

        /// <summary>
        /// 任意のヒエラルキーの Renderer に対し、Standard シェーダーを Transparent モードへ切替えて
        /// 半透明にする。プレビュー / 移動先ゴースト用。
        /// </summary>
        private static void ApplyTransparency(GameObject root, float alpha)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = r.materials; // インスタンスを生成して個別に編集する
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;
                    if (mat.HasProperty("_Mode"))
                    {
                        mat.SetFloat("_Mode", 3); // Transparent
                    }
                    if (mat.HasProperty("_SrcBlend"))
                    {
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                    }
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    if (mat.HasProperty("_Color"))
                    {
                        var c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        c.a = alpha;
                        mat.SetColor("_BaseColor", c);
                    }
                }
                r.materials = mats;
            }
        }

        private static void DisableGameLogicForEditor(GameObject root)
        {
            // すべての MonoBehaviour を無効化 (Animator/Renderer/Collider/Rigidbody は MonoBehaviour ではない)
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                mb.enabled = false;
            }
            // 物理を凍結
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

        // ========== レイキャスト (UI ↔ 3D) ==========

        public bool TryRaycastGround(out Vector3 worldHit)
        {
            return TryRaycastGround(Input.mousePosition, out worldHit);
        }

        public bool TryRaycastGround(Vector3 screenPos, out Vector3 worldHit)
        {
            if (IsScreenPointBlockedByUi(screenPos))
            {
                worldHit = Vector3.zero;
                return false;
            }

            if (editorCamera == null) editorCamera = Camera.main;
            if (editorCamera == null) { worldHit = Vector3.zero; return false; }

            Ray ray = editorCamera.ScreenPointToRay(screenPos);
            if (GroundPlane.Raycast(ray, out float enter))
            {
                worldHit = ray.GetPoint(enter);
                return true;
            }
            worldHit = Vector3.zero;
            return false;
        }

        private bool IsScreenPointBlockedByUi(Vector3 screenPos)
        {
            if (EventSystem.current == null) { return false; }

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointer, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }

        // ========== パレットドラッグ ==========

        public GameObject SpawnGhostFromPalette(string partId, PointerEventData eventData)
        {
            EnsureSceneInfra();
            if (catalog == null) return null;
            var def = catalog.Find(partId);
            if (def == null || def.prefab == null) return null;
            if (currentData == null) return null;

            // ドラッグ開始時は配置データ未追加。EndDrag で確定する。
            var placement = new CustomStagePartPlacement
            {
                partId = partId,
                worldPosition = Vector3.zero,
                rotationY = 0f,
            };

            if (TryRaycastGround(eventData.position, out Vector3 hit))
            {
                placement.worldPosition = hit;
            }

            var go = SpawnPlacementInScene(placement);
            if (go == null) return null;

            // Pending: 確定するまで currentData.parts には入れない
            return go;
        }

        public void FinalizeGhost(GameObject ghost, DraggablePart ghostDraggable, bool accepted)
        {
            if (ghost == null) return;
            if (!accepted || currentData == null || ghostDraggable == null || ghostDraggable.placement == null)
            {
                if (ghostDraggable != null && ghostDraggable.partner != null)
                {
                    Destroy(ghostDraggable.partner.gameObject);
                }
                Destroy(ghost);
                return;
            }

            var placement = ghostDraggable.placement;
            var def = ghostDraggable.definition;

            // unique なら既存を消してから入れ替え
            if (def != null && def.unique)
            {
                currentData.parts.RemoveAll(x => x.partId == def.id);
                // 既存のシーン上 unique 物体も破棄
                for (int i = partsRoot.childCount - 1; i >= 0; i--)
                {
                    var child = partsRoot.GetChild(i).gameObject;
                    if (child == ghost) continue;
                    var dp = child.GetComponent<DraggablePart>();
                    if (dp != null && dp.definition != null && dp.definition.id == def.id)
                    {
                        Destroy(child);
                    }
                }
            }

            currentData.parts.Add(placement);
        }

        // ========== マウス入力 (配置済みパーツのドラッグ移動 / 削除) ==========

        private DraggablePart currentDrag;
        private Vector3 currentDragGroundOffset;
        private Vector3 currentDragStartMousePosition;
        private bool currentDragMoved;

        // 画面ピクセル単位の判定半径
        private const float PartPickPixelRadius = 60f;
        private const float DragStartPixelThreshold = 8f;

        private void Update()
        {
            if (sceneRoot == null) { return; }
            if (editorCamera == null) { return; }

            if (Input.GetMouseButtonDown(0))
            {
                // UI の上 (パレット等) なら 3D 入力を無視
                if (IsScreenPointBlockedByUi(Input.mousePosition)) { return; }

                var part = FindPartUnderCursor(Input.mousePosition);

                // 既存パーツの上をクリック
                if (part != null)
                {
                    // 消しゴムは本体パーツのみ削除可 (ハンドル単独では消せない)
                    if (eraserMode)
                    {
                        if (!part.isHandle) { RequestDelete(part); }
                        return;
                    }
                    if (TryRaycastGround(out Vector3 hitForDrag))
                    {
                        currentDrag = part;
                        currentDragGroundOffset = part.transform.position - hitForDrag;
                        currentDragStartMousePosition = Input.mousePosition;
                        currentDragMoved = false;
                    }
                    return;
                }

                // 既存パーツがない場所のクリック: 選択中のパーツがあれば置く (クリック配置モード)
                if (!eraserMode && !string.IsNullOrEmpty(selectedPartId)
                    && TryRaycastGround(out Vector3 placePos))
                {
                    PlacePartAt(selectedPartId, placePos);
                }
            }
            else if (Input.GetMouseButton(0) && currentDrag != null)
            {
                if (!currentDragMoved)
                {
                    float dragDistance = Vector2.Distance(Input.mousePosition, currentDragStartMousePosition);
                    if (dragDistance < DragStartPixelThreshold) { return; }
                    currentDragMoved = true;
                }

                if (TryRaycastGround(out Vector3 hit))
                {
                    Vector3 newPos = hit + currentDragGroundOffset;

                    if (currentDrag.placement == null) { return; }

                    if (currentDrag.isHandle)
                    {
                        // ハンドル: 種別ごとに配置先を拘束
                        newPos = ConstrainHandlePosition(currentDrag, newPos);
                        currentDrag.transform.position = newPos;
                        currentDrag.placement.directionTarget = newPos;
                        if (currentDrag.partner != null)
                        {
                            UpdateLinkLine(currentDrag.partner);
                            // Blizzard はハンドル回転に追従して風向きをリアルタイム更新
                            if (currentDrag.partner.definition != null
                                && currentDrag.partner.definition.directionalKind == "Blizzard")
                            {
                                ApplyBlizzardWindLive(currentDrag.partner);
                            }
                        }
                    }
                    else
                    {
                        // 本体: worldPosition を更新し、ハンドル (とリンク) も同じ delta だけ動かす
                        Vector3 oldPos = currentDrag.transform.position;
                        Vector3 delta = newPos - oldPos;
                        currentDrag.transform.position = newPos;
                        if (currentDrag.definition != null)
                        {
                            currentDrag.placement.worldPosition = newPos - currentDrag.definition.spawnOffset;
                        }
                        if (currentDrag.partner != null)
                        {
                            // Blizzard ハンドルは本体に対して固定半径なので位置を再計算
                            if (currentDrag.partner.definition != null && currentDrag.partner.definition.directionalKind == "Blizzard")
                            {
                                Vector3 newHandlePos = ConstrainHandlePosition(currentDrag.partner, currentDrag.partner.transform.position + delta);
                                currentDrag.partner.transform.position = newHandlePos;
                                currentDrag.placement.directionTarget = newHandlePos;
                            }
                            else
                            {
                                currentDrag.partner.transform.position += delta;
                                currentDrag.placement.directionTarget += delta;
                            }
                        }
                        UpdateLinkLine(currentDrag);
                        // Blizzard 本体を動かしたときも風向きをリアルタイム更新
                        if (currentDrag.definition != null
                            && currentDrag.definition.directionalKind == "Blizzard")
                        {
                            ApplyBlizzardWindLive(currentDrag);
                        }
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (currentDrag != null
                    && !currentDragMoved
                    && !eraserMode
                    && !string.IsNullOrEmpty(selectedPartId)
                    && TryRaycastGround(out Vector3 placePos))
                {
                    PlacePartAt(selectedPartId, placePos);
                }
                currentDrag = null;
                currentDragMoved = false;
            }
        }

        /// <summary>
        /// カーソル位置から最も近い配置済みパーツを探す (画面距離で判定)。
        /// パーツに 3D Collider を強制せずに済むよう、Physics ではなく
        /// WorldToScreenPoint 距離で選ぶ。
        /// </summary>
        private DraggablePart FindPartUnderCursor(Vector3 mouseScreen)
        {
            if (partsRoot == null) { return null; }

            DraggablePart best = null;
            float bestDist = PartPickPixelRadius;

            for (int i = 0; i < partsRoot.childCount; i++)
            {
                var child = partsRoot.GetChild(i);
                var part = child.GetComponent<DraggablePart>();
                if (part == null) { continue; }

                Vector3 sp = editorCamera.WorldToScreenPoint(child.position);
                if (sp.z < 0f) { continue; } // カメラ後ろ
                float dx = sp.x - mouseScreen.x;
                float dy = sp.y - mouseScreen.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = part;
                }
            }
            return best;
        }

        public void RequestDelete(DraggablePart part)
        {
            if (part == null || currentData == null) return;
            if (part.placement != null) { currentData.parts.Remove(part.placement); }

            // 方向性パーツの場合は対のハンドルも消す
            if (part.partner != null)
            {
                Destroy(part.partner.gameObject);
            }
            Destroy(part.gameObject);
        }

        /// <summary>
        /// パレット選択中のパーツを指定位置に新規配置する (クリック配置モード)。
        /// </summary>
        private void PlacePartAt(string partId, Vector3 worldPos)
        {
            if (currentData == null || catalog == null) return;
            var def = catalog.Find(partId);
            if (def == null) return;

            var placement = new CustomStagePartPlacement
            {
                partId = partId,
                worldPosition = worldPos,
                rotationY = 0f,
            };

            // unique なパーツは既存を取り除く
            if (def.unique)
            {
                currentData.parts.RemoveAll(x => x.partId == def.id);
                if (partsRoot != null)
                {
                    for (int i = partsRoot.childCount - 1; i >= 0; i--)
                    {
                        var child = partsRoot.GetChild(i);
                        var dp = child.GetComponent<DraggablePart>();
                        if (dp != null && dp.definition != null && dp.definition.id == def.id)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }

            currentData.parts.Add(placement);
            SpawnPlacementInScene(placement);
        }

        // ========== 保存・プレイ ==========

        private void SaveCurrent()
        {
            if (currentData == null) return;
            CustomStageRepository.Save(currentData);
            controller.ReloadStages();
        }

        private void SaveAndPlay()
        {
            if (currentData == null) return;
            CustomStageRepository.Save(currentData);
            controller.PlayCustomStage(currentData.id);
        }

        private void OnDisable()
        {
            // 編集ビューを抜けるときは 3D シーンを破棄。プレビューリグは再表示時に再利用するので残す。
            if (sceneRoot != null)
            {
                Destroy(sceneRoot);
                sceneRoot = null;
                partsRoot = null;
            }
        }

        private void OnDestroy()
        {
            // 編集ビュー自体が破棄されるとき (シーン遷移時) のみプレビューを破棄
            PalettePreviewRenderer.Cleanup();
        }
    }
}
