using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StageMaker;

public class TitleManager : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.bgm.Change(BgmType.Title);
        InjectStageMakerButton();
        InjectQuitButton();
        InjectVolumeSliders();
    }

    /// <summary>
    /// Title シーンに「Stage Maker」ボタンを動的に挿入する。
    /// シーンファイルを変更せずに済むよう実行時に追加している。
    /// </summary>
    private void InjectStageMakerButton()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { return; }

        // 既に挿入済みなら何もしない
        var existing = canvas.transform.Find("StageMakerButton");
        if (existing != null) { return; }

        var go = new GameObject("StageMakerButton", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        // タイトルの左端と、既存 StartButton の下端に揃える。
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(30, 58);
        rt.sizeDelta = new Vector2(190, 54);

        var img = go.AddComponent<Image>();
        StageMakerUIFactory.StyleButtonImage(img, Color.white);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.font = StageMakerUIFactory.GetFont();
        label.text = "STAGE MAKER";
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.color = StageMakerUIFactory.IceText;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 20;

        btn.onClick.AddListener(OnStageMakerButtonClicked);
    }

    /// <summary>
    /// Title シーンに「Quit」ボタンを動的に挿入する。
    /// シーンファイルを変更せずに済むよう実行時に追加している。
    /// </summary>
    private void InjectQuitButton()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { return; }

        // 既に挿入済みなら何もしない
        var existing = canvas.transform.Find("QuitButton");
        if (existing != null) { return; }

        var go = new GameObject("QuitButton", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        // StageMakerButton の右隣に並べる。
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(230, 58);
        rt.sizeDelta = new Vector2(120, 54);

        var img = go.AddComponent<Image>();
        StageMakerUIFactory.StyleButtonImage(img, Color.white);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.font = StageMakerUIFactory.GetFont();
        label.text = "QUIT";
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.color = StageMakerUIFactory.IceText;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 20;

        btn.onClick.AddListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// Title シーンに BGM / SE の音量スライダーを挿入する。
    /// シーンに配置済み (Tools > SlidingPenguin > 音量パネルをシーンに配置 で焼き込んだもの)
    /// があればそれを使い、無ければ実行時に生成する。配線は VolumeSliderBinder が行う。
    /// </summary>
    private void InjectVolumeSliders()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { return; }

        // シーンに配置済みなら何もしない (VolumeSliderBinder が自分で配線する)
        if (canvas.transform.Find("VolumePanel") != null) { return; }

        VolumeSliderBinder.Build(canvas.transform);
    }

    private void OnQuitButtonClicked()
    {
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (DataLogger.Instance != null) { DataLogger.Instance.AbortTrial(); }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnStageMakerButtonClicked()
    {
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (DataLogger.Instance != null) { DataLogger.Instance.AbortTrial(); }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.bgm.Stop();
            AudioManager.Instance.bgm.SetPitch(1.0f);
        }
        SceneManager.LoadScene("StageMaker");
    }
}
