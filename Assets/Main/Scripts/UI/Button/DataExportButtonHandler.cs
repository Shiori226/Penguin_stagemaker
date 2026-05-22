using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DataExportButtonHandler : MonoBehaviour
{
    private Button exportButton;

    void Awake()
    {
        exportButton = GetComponent<Button>();

        if (StageGenerator.GetIsLatestSceneSettingMode())
        {
            exportButton.interactable = false;
            return;
        }

        exportButton.onClick.AddListener(OnLogExportButtonClicked);
    }

    private void OnLogExportButtonClicked()
    {
        Debug.Log("DataExport button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);
        if (DataLogger.Instance) { DataLogger.Instance.Export(); }
    }
}
