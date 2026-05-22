using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PersistentDataPathButtonHandler : MonoBehaviour
{
    private Button openDirectoryButton;

    private void Awake()
    {
        openDirectoryButton = GetComponent<Button>();
        openDirectoryButton.onClick.AddListener(OnOpenDirectoryButtonClicked);
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

    private void OnOpenDirectoryButtonClicked()
    {
        UnityEngine.Debug.Log("Parameter open directory button clicked");

        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickNormal);

        string path = Application.persistentDataPath;

        try
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // Windows
                Process.Start(path);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                // macOS (Finderで開く)
                Process.Start("open", path);
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // Linux (デフォルトのファイルマネージャで開く)
                Process.Start("xdg-open", path);
            }
            else
            {
                UnityEngine.Debug.LogWarning("This platform does not support opening the path directly.");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to open path: {path}\nError: {e.Message}");
        }
    }
}
