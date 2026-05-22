using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ClosePauseButtonHandler : MonoBehaviour
{
    private Button closePauseButton;

    private void Awake()
    {
        closePauseButton = GetComponent<Button>();
        closePauseButton.onClick.AddListener(OnClosePauseButtonClicked);
    }

    private void OnClosePauseButtonClicked()
    {
        Debug.Log("Close pause button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickNormal);
        InputDataManager.Instance.TriggerPauseInput();
    }
}
