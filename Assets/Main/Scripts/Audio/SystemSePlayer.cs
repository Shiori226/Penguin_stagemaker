using UnityEngine;

public enum SeTypeSystem
{
    ButtonClickNormal,
    ButtonClickTransition,
    SlideMove,
    CountDownBeep,
    Success,
    Failure,
    Applause,
    Miss,
    RushStart,
    DisplayScore,
}

[RequireComponent(typeof(AudioSource))]
public class SystemSePlayer : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンド
    }

    public void Play(SeTypeSystem seType)
    {
        AudioClip clip = null;

        switch (seType)
        {
            case SeTypeSystem.ButtonClickNormal:
                clip = Resources.Load<AudioClip>("Audio/SE/System/Common/ClickNormalSE");
                break;
            case SeTypeSystem.ButtonClickTransition:
                clip = Resources.Load<AudioClip>("Audio/SE/System/Common/ClickTransitionSE");
                break;
            case SeTypeSystem.SlideMove:
                clip = Resources.Load<AudioClip>("Audio/SE/System/Common/SliderMoveSE");
                break;
            case SeTypeSystem.CountDownBeep:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/CountDown");
                break;
            case SeTypeSystem.Success:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/SuccessSE");
                break;
            case SeTypeSystem.Failure:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/FailureSE");
                break;
            case SeTypeSystem.Applause:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/ApplauseSE");
                break;
            case SeTypeSystem.Miss:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/MissSE");
                break;
            case SeTypeSystem.RushStart:
                clip = Resources.Load<AudioClip>("Audio/SE/System/InGame/RushStartSE");
                break;
            case SeTypeSystem.DisplayScore:
                clip = Resources.Load<AudioClip>("Audio/SE/System/Result/DisplayScore");
                break;
            default:
                Debug.LogError("Invalid SE Type");
                return;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            return;
        }
        Debug.LogError("Sound clip not found for SE Type: " + seType);
    }

    public void Stop()
    {
        // 現在は特定のSEを停止する機能は実装していません。
        // 必要に応じて拡張してください。
        audioSource.Stop();
    }
}
