using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// ステージ一覧ビュー (デフォルト4ステージ + 自作ステージ)。
    /// </summary>
    public class StageMakerListView : MonoBehaviour
    {
        private static readonly (StageType type, string label)[] DefaultStages =
        {
            (StageType.Practice,    "Practice Stage"),
            (StageType.FirstStage,  "1st Stage"),
            (StageType.SecondStage, "2nd Stage"),
            (StageType.ThirdStage,  "3rd Stage"),
        };

        private StageMakerSceneController controller;
        private RectTransform listContent;

        public void Initialize(StageMakerSceneController c)
        {
            controller = c;
            BuildLayout();
        }

        private void BuildLayout()
        {
            // 一覧ビューだけがフルスクリーン背景を持つ (編集ビューでは 3D シーンを見せるため非表示)
            var bg = StageMakerUIFactory.AddRect(gameObject, "Background",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            StageMakerUIFactory.AddBackgroundImage(bg.gameObject);

            // ヘッダ
            var header = StageMakerUIFactory.AddRect(gameObject, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(32, -118), new Vector2(-32, -18));
            StageMakerUIFactory.AddPanelImage(header.gameObject, new Color(1f, 1f, 1f, 0.6f));

            var title = StageMakerUIFactory.CreateText(header.gameObject, "Title", "SLIDING PENGUIN MAKER",
                38, StageMakerUIFactory.IceText, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 1));
            title.fontStyle = FontStyle.Bold;
            var titleRt = (RectTransform)title.transform;
            titleRt.offsetMin = new Vector2(126, 0);
            titleRt.offsetMax = new Vector2(-260, 0);

            var backGo = new GameObject("BackButton", typeof(RectTransform));
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.SetParent(header.transform, false);
            backRt.anchorMin = new Vector2(0, 0.5f);
            backRt.anchorMax = new Vector2(0, 0.5f);
            backRt.sizeDelta = new Vector2(76, 70);
            backRt.anchoredPosition = new Vector2(70, 0);
            var backHit = backGo.AddComponent<Image>();
            backHit.color = new Color(1f, 1f, 1f, 0f);
            backHit.raycastTarget = true;
            var backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backHit;
            var backLabel = StageMakerUIFactory.CreateText(backGo, "Label", "←",
                52, StageMakerUIFactory.IceText, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            backLabel.fontStyle = FontStyle.Bold;
            backBtn.onClick.AddListener(() => controller.BackToTitle());

            // 一覧スクロール領域
            var scrollGo = new GameObject("StageScrollView", typeof(RectTransform));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(transform, false);
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(64, 116);
            scrollRt.offsetMax = new Vector2(-64, -146);

            var scrollImage = scrollGo.AddComponent<Image>();
            StageMakerUIFactory.StylePanelImage(scrollImage, new Color(1f, 1f, 1f, 0.6f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollGo.AddComponent<RectMask2D>();

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollGo.transform, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            scrollRect.viewport = viewportRt;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            listContent = contentGo.GetComponent<RectTransform>();
            listContent.SetParent(viewportGo.transform, false);
            listContent.anchorMin = new Vector2(0, 1);
            listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = new Vector2(0, 0);
            scrollRect.content = listContent;

            var vlayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(30, 30, 30, 30);
            vlayout.spacing = 6;
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childForceExpandWidth = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildFooterButtons();
        }

        private Text statusText;

        private void BuildFooterButtons()
        {
            // 新規作成ボタン (中央)
            var (newGo, newBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "NewStageButton",
                "NEW STAGE", Color.white, StageMakerUIFactory.IceText, new Vector2(280, 72));
            var newRt = newGo.GetComponent<RectTransform>();
            newRt.anchorMin = new Vector2(0.5f, 0);
            newRt.anchorMax = new Vector2(0.5f, 0);
            newRt.anchoredPosition = new Vector2(0, 64);
            newBtn.onClick.AddListener(() =>
            {
                var newData = CustomStageData.CreateNew("New Stage");
                controller.EnterEditor(newData);
            });

            // Import ボタン (左)
            var (importGo, importBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "ImportButton",
                "IMPORT", Color.white, StageMakerUIFactory.IceText, new Vector2(180, 60));
            var importRt = importGo.GetComponent<RectTransform>();
            importRt.anchorMin = new Vector2(0.5f, 0);
            importRt.anchorMax = new Vector2(0.5f, 0);
            importRt.anchoredPosition = new Vector2(-252, 64);
            importBtn.onClick.AddListener(() =>
            {
                StageMakerFilePicker.PickJson(this, ImportJson, ShowStatus);
            });

            // ステータス表示テキスト (フッタ下)
            var statusGo = new GameObject("Status", typeof(RectTransform));
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.SetParent(transform, false);
            statusRt.anchorMin = new Vector2(0, 0);
            statusRt.anchorMax = new Vector2(1, 0);
            statusRt.pivot = new Vector2(0.5f, 0);
            statusRt.anchoredPosition = new Vector2(0, 16);
            statusRt.sizeDelta = new Vector2(-40, 24);
            statusText = statusGo.AddComponent<Text>();
            statusText.font = StageMakerUIFactory.GetFont();
            statusText.fontSize = 14;
            statusText.color = StageMakerUIFactory.IceText;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statusText.text = "";
        }

        private void ShowStatus(string msg)
        {
            if (statusText != null) { statusText.text = msg; }
            Debug.Log("[StageMakerListView] " + msg);
        }

        private void ImportJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                ShowStatus("Import canceled.");
                return;
            }

            if (!CustomStageRepository.ImportFromJson(json))
            {
                ShowStatus("Import failed: invalid stage JSON.");
                return;
            }

            ShowStatus("Imported stage.");
            controller.ReloadStages();
            controller.ShowList();
        }

        public void Refresh(List<CustomStageData> customStages)
        {
            // 既存の行を削除
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            bool needsDivider = false;
            foreach (var (type, label) in DefaultStages)
            {
                if (needsDivider) { CreateDivider(); }
                CreateDefaultRow(label, type);
                needsDivider = true;
            }

            foreach (var data in customStages)
            {
                if (needsDivider) { CreateDivider(); }
                CreateCustomRow(data);
                needsDivider = true;
            }
        }

        private void CreateDivider()
        {
            var dividerGo = new GameObject("Divider", typeof(RectTransform));
            var dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.SetParent(listContent, false);
            dividerRt.sizeDelta = new Vector2(0, 2);

            var lineGo = new GameObject("Line", typeof(RectTransform));
            var lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.SetParent(dividerGo.transform, false);
            lineRt.anchorMin = new Vector2(0, 0.5f);
            lineRt.anchorMax = new Vector2(1, 0.5f);
            lineRt.offsetMin = new Vector2(20, -0.75f);
            lineRt.offsetMax = new Vector2(-10, 0.75f);
            var line = lineGo.AddComponent<Image>();
            line.color = new Color(1f, 1f, 1f, 0.36f);
            line.raycastTarget = false;
        }

        private void CreateDefaultRow(string label, StageType type)
        {
            var row = CreateRowBase(label, "Default");
            CreateRowButton(row, "PLAY", Color.white,
                () => controller.PlayDefaultStage(type));
        }

        private void CreateCustomRow(CustomStageData data)
        {
            string subTitle = $"Custom · {data.parts.Count} parts";
            var row = CreateRowBase(data.displayName, subTitle);
            CreateRowButton(row, "EDIT", Color.white,
                () => controller.EnterEditor(data));
            CreateRowButton(row, "PLAY", Color.white,
                () => controller.PlayCustomStage(data.id));
            CreateRowButton(row, "EXPORT", Color.white,
                () =>
                {
                    string path = CustomStageRepository.Export(data);
                    ShowStatus(string.IsNullOrEmpty(path) ? "Export failed." : "Exported to: " + path);
                });
            CreateRowButton(row, "DELETE", Color.white,
                () =>
                {
                    CustomStageRepository.Delete(data.id);
                    controller.ReloadStages();
                    controller.ShowList();
                });
        }

        private GameObject CreateRowBase(string title, string subtitle)
        {
            var rowGo = new GameObject(title, typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(listContent, false);
            rowRt.sizeDelta = new Vector2(0, 90);

            // タイトルテキスト
            var titleGo = new GameObject("Title", typeof(RectTransform));
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.SetParent(rowGo.transform, false);
            titleRt.anchorMin = new Vector2(0, 0.5f);
            titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = new Vector2(20, 0);
            titleRt.offsetMax = new Vector2(0, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = StageMakerUIFactory.GetFont();
            titleText.text = title;
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = StageMakerUIFactory.IceText;
            titleText.alignment = TextAnchor.LowerLeft;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // サブタイトル
            var subGo = new GameObject("Subtitle", typeof(RectTransform));
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.SetParent(rowGo.transform, false);
            subRt.anchorMin = new Vector2(0, 0);
            subRt.anchorMax = new Vector2(0.7f, 0.5f);
            subRt.offsetMin = new Vector2(20, 4);
            subRt.offsetMax = new Vector2(0, 0);
            var subText = subGo.AddComponent<Text>();
            subText.font = StageMakerUIFactory.GetFont();
            subText.text = subtitle;
            subText.fontSize = 16;
            subText.color = new Color(0.86f, 0.95f, 1f, 1f);
            subText.alignment = TextAnchor.UpperLeft;

            // ボタン用 HorizontalLayoutGroup
            var btnsGo = new GameObject("Buttons", typeof(RectTransform));
            var btnsRt = btnsGo.GetComponent<RectTransform>();
            btnsRt.SetParent(rowGo.transform, false);
            btnsRt.anchorMin = new Vector2(0.7f, 0);
            btnsRt.anchorMax = new Vector2(1, 1);
            btnsRt.offsetMin = new Vector2(0, 10);
            btnsRt.offsetMax = new Vector2(-10, -10);
            var hlayout = btnsGo.AddComponent<HorizontalLayoutGroup>();
            hlayout.padding = new RectOffset(0, 0, 0, 0);
            hlayout.spacing = 8;
            hlayout.childAlignment = TextAnchor.MiddleRight;
            hlayout.childControlHeight = true;
            hlayout.childControlWidth = false;
            hlayout.childForceExpandHeight = true;
            hlayout.childForceExpandWidth = false;

            return btnsGo;
        }

        private void CreateRowButton(GameObject parent, string label, Color color, System.Action onClick)
        {
            var (go, btn, _) = StageMakerUIFactory.CreateButton(parent, label, label, color, StageMakerUIFactory.IceText, new Vector2(116, 58));
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
