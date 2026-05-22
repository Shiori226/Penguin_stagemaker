using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ParameterExportButtonHandler : MonoBehaviour
{
    private Button saveButton;

    private void Awake()
    {
        saveButton = GetComponent<Button>();
        saveButton.onClick.AddListener(OnParameterExportButtonClicked);
    }

    private void Start()
    {
        bool isDesktopPlatform =
        Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.OSXPlayer ||
        Application.platform == RuntimePlatform.LinuxEditor ||
        Application.platform == RuntimePlatform.LinuxPlayer;

        gameObject.SetActive(isDesktopPlatform);
    }

    private void OnParameterExportButtonClicked()
    {
        Debug.Log("Parameter export button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickNormal);
        DataManager.Instance.ExportData();
    }
}
