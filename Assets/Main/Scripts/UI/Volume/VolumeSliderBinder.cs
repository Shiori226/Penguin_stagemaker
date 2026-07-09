using StageMaker;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VolumePanel (BGM/SE スライダー) と AudioManager を結びつける。
/// シーンに静的配置してもコードで配線されるため、Inspector でイベントを
/// 設定する必要はない (パネルの位置・サイズはシーン上で自由に調整できる)。
/// </summary>
public class VolumeSliderBinder : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    // SE スライダー操作時の試聴フィードバック (連続発火しないよう間引く)
    private float lastSeFeedbackTime = -1f;

    private void Start()
    {
        // 手動配置などで参照が無い場合は名前で探す
        if (bgmSlider == null)
        {
            var t = transform.Find("BgmRow/Slider");
            if (t != null) { bgmSlider = t.GetComponent<Slider>(); }
        }
        if (seSlider == null)
        {
            var t = transform.Find("SeRow/Slider");
            if (t != null) { seSlider = t.GetComponent<Slider>(); }
        }

        var audio = AudioManager.Instance;
        if (audio == null) { return; }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(audio.BgmVolume);
            bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }
        if (seSlider != null)
        {
            seSlider.SetValueWithoutNotify(audio.SeVolume);
            seSlider.onValueChanged.RemoveListener(OnSeChanged);
            seSlider.onValueChanged.AddListener(OnSeChanged);
        }
    }

    private void OnBgmChanged(float value)
    {
        AudioManager.Instance?.SetBgmVolume(value);
    }

    private void OnSeChanged(float value)
    {
        AudioManager.Instance?.SetSeVolume(value);

        if (Time.unscaledTime - lastSeFeedbackTime < 0.15f) { return; }
        lastSeFeedbackTime = Time.unscaledTime;
        AudioManager.Instance?.se.Play(SeTypeSystem.SlideMove);
    }

    /// <summary>
    /// VolumePanel を canvasParent 直下に構築する。
    /// 実行時のフォールバック注入と、エディタメニューからのシーン焼き込みの両方で使う。
    /// </summary>
    public static GameObject Build(Transform canvasParent)
    {
        var panelGo = new GameObject("VolumePanel", typeof(RectTransform));
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.SetParent(canvasParent, false);
        // 画面右下に配置 (シーンに焼き込んだ後は自由に動かせる)
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 0);
        panelRt.pivot = new Vector2(1, 0);
        panelRt.anchoredPosition = new Vector2(-130, 58);
        panelRt.sizeDelta = new Vector2(200, 80);
        var panelImg = panelGo.AddComponent<Image>();
        StageMakerUIFactory.StylePanelImage(panelImg, new Color(1f, 1f, 1f, 0.6f));

        var binder = panelGo.AddComponent<VolumeSliderBinder>();
        binder.bgmSlider = CreateVolumeRow(panelGo, "BgmRow", "BGM", -4f);
        binder.seSlider = CreateVolumeRow(panelGo, "SeRow", "SE", -42f);
        return panelGo;
    }

    private static Slider CreateVolumeRow(GameObject parent, string name, string label, float y)
    {
        var rowGo = new GameObject(name, typeof(RectTransform));
        var rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.SetParent(parent.transform, false);
        rowRt.anchorMin = new Vector2(0, 1);
        rowRt.anchorMax = new Vector2(1, 1);
        rowRt.pivot = new Vector2(0.5f, 1);
        rowRt.anchoredPosition = new Vector2(0, y);
        rowRt.sizeDelta = new Vector2(-24, 38);

        var labelText = StageMakerUIFactory.CreateText(rowGo, "Label", label,
            20, StageMakerUIFactory.IceText, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(0, 1));
        labelText.fontStyle = FontStyle.Bold;
        var labelRt = (RectTransform)labelText.transform;
        labelRt.sizeDelta = new Vector2(56, 0);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.anchoredPosition = new Vector2(0, 0);

        var (sliderGo, slider) = StageMakerUIFactory.CreateSlider(rowGo, "Slider", new Vector2(0, 24));
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0.5f);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.offsetMin = new Vector2(60, -12);
        sliderRt.offsetMax = new Vector2(0, 12);

        return slider;
    }
}
