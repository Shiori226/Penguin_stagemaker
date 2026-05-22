using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RestartButtonHandler : MonoBehaviour
{
    private Button restartButton;

    private void Awake()
    {
        restartButton = GetComponent<Button>();
        restartButton.onClick.AddListener(OnRestartButtonClicked);
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log("Restart button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);
        SceneLoadUtility.LoadCurrentScene();
    }
}
