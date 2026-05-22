using UnityEngine;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// 実行時に Unity UGUI のオブジェクトを生成するためのヘルパー。
    /// シーンファイルを最小限に保つため、すべてのUIをコードで構築している。
    /// </summary>
    public static class StageMakerUIFactory
    {
        private const string BackgroundSpritePath = "StageMaker/UI/StageMakerBackground";
        private const string ButtonSpritePath = "StageMaker/UI/StageMakerButton";
        private const string EraserIconSpritePath = "StageMaker/UI/EraserIcon";
        private const string PanelSpritePath = "StageMaker/UI/StageMakerPanel";

        public static Font defaultFont;
        private static Sprite backgroundSprite;
        private static Sprite buttonSprite;
        private static Sprite eraserIconSprite;
        private static Sprite panelSprite;

        public static readonly Color TitleBlue = new Color(0.02f, 0.35f, 0.95f, 1f);
        public static readonly Color IceText = new Color(0.96f, 1f, 1f, 1f);

        public static Font GetFont()
        {
            if (defaultFont != null) { return defaultFont; }
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return defaultFont;
        }

        public static Sprite GetBackgroundSprite()
        {
            return LoadSprite(ref backgroundSprite, BackgroundSpritePath);
        }

        public static Sprite GetButtonSprite()
        {
            return LoadSprite(ref buttonSprite, ButtonSpritePath);
        }

        public static Sprite GetPanelSprite()
        {
            return LoadSprite(ref panelSprite, PanelSpritePath);
        }

        public static Sprite GetEraserIconSprite()
        {
            return LoadSprite(ref eraserIconSprite, EraserIconSpritePath);
        }

        private static Sprite LoadSprite(ref Sprite cache, string path)
        {
            if (cache != null) { return cache; }
            cache = Resources.Load<Sprite>(path);
            return cache;
        }

        public static GameObject CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        public static RectTransform AddRect(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image AddImage(GameObject target, Color color, bool raycastTarget = true)
        {
            var img = target.GetComponent<Image>();
            if (img == null) { img = target.AddComponent<Image>(); }
            img.color = color;
            img.raycastTarget = raycastTarget;
            return img;
        }

        public static Image AddBackgroundImage(GameObject target, float alpha = 1f)
        {
            var img = target.GetComponent<Image>();
            if (img == null) { img = target.AddComponent<Image>(); }
            var sprite = GetBackgroundSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = new Color(1f, 1f, 1f, alpha);
                img.preserveAspect = false;
            }
            else
            {
                img.color = new Color(0.02f, 0.35f, 0.95f, alpha);
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image AddPanelImage(GameObject target, Color tint, bool raycastTarget = true)
        {
            var img = target.GetComponent<Image>();
            if (img == null) { img = target.AddComponent<Image>(); }
            StylePanelImage(img, tint, raycastTarget);
            return img;
        }

        public static void StylePanelImage(Image img, Color tint, bool raycastTarget = true)
        {
            var sprite = GetPanelSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1f, 1f, 1f, tint.a);
            }
            else
            {
                img.color = tint;
            }
            img.raycastTarget = raycastTarget;
        }

        public static void StyleButtonImage(Image img, Color tint)
        {
            var sprite = GetButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1f, 1f, 1f, tint.a);
            }
            else
            {
                img.color = tint;
            }
            img.raycastTarget = true;
        }

        public static (GameObject go, Button button, Text label) CreateButton(GameObject parent, string name, string text, Color bg, Color textColor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            var img = go.AddComponent<Image>();
            StyleButtonImage(img, bg);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(go.transform, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelRt.localScale = Vector3.one;
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = GetFont();
            labelText.text = text;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = textColor;
            labelText.fontSize = 22;
            labelText.fontStyle = FontStyle.Bold;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 12;
            labelText.resizeTextMaxSize = 24;

            return (go, btn, labelText);
        }

        public static Text CreateText(GameObject parent, string name, string text, int fontSize, Color color, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent.transform, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var t = go.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
