using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartButtonHandler : MonoBehaviour
{
    private Button startButton;

    private void Awake()
    {
        startButton = GetComponent<Button>();
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("Start button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);
        SceneLoadUtility.LoadScene("InGame");
    }
}
