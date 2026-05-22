using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のカメラを切り替えるコンポーネント。
/// CameraTypeは追加していくことで、任意のカメラを切り替えることができる。
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    public enum CameraType
    {
        PlayerCamera,
        CourseCamera,
    }

    [Header("現在のカメラタイプ")]
    [SerializeField]
    private CameraType currentType;

    [Header("カメラ参照")]
    [Tooltip("プレイヤーを追従するカメラ")]
    [SerializeField]
    private GameObject playerCamera;
    [Tooltip("コースを紹介するカメラ")]
    [SerializeField]
    private GameObject courseCamera;

    private Dictionary<CameraType, GameObject> cameras;

    void Awake()
    {
        if(playerCamera == null)
        {
            playerCamera = Camera.main.gameObject;
        }
        if(courseCamera == null)
        {
            courseCamera = GameObject.FindWithTag("SubCamera");
        }

        cameras = new Dictionary<CameraType, GameObject>
        {
            { CameraType.PlayerCamera, playerCamera },
            { CameraType.CourseCamera, courseCamera }
        };
    }

    public void SwitchCamera(CameraType type)
    {
        if(currentType == type)
        {
            return;
        }

        foreach (var cam in cameras)
        {
            if (cam.Value != null)
            {
                cam.Value.gameObject.SetActive(false);
            }
        }

        cameras[type].gameObject.SetActive(true);
        currentType = type;
    }
}

