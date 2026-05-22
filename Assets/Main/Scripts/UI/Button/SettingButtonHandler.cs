using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingButtonHandler : MonoBehaviour
{
    private Button settingButton;

    private void OnEnable()
    {
        // Awake ではなく OnEnable で確実に紐づけ直す。
        // Unity 6 アップグレード時に m_OnClick のシリアライズ済みコールが空になる事象に備え、
        // 毎回 RemoveListener → AddListener して二重登録は避けつつ確実にハンドルを保証する。
        if (settingButton == null) { settingButton = GetComponent<Button>(); }
        settingButton.onClick.RemoveListener(OnSettingButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
    }

    private void OnSettingButtonClicked()
    {
        Debug.Log("Setting button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);
        SceneLoadUtility.LoadScene("Setting");
    }
}
