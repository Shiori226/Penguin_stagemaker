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
    private float baseVolume = 1f;   // プレハブに設定された基準音量

    void Awake()
    {
        EnsureInitialized();
    }

    // AudioManager.Awake が先に走っても動くよう遅延初期化にしている
    private void EnsureInitialized()
    {
        if (audioSource != null) { return; }
        audioSource = gameObject.GetComponent<AudioSource>();
        baseVolume = audioSource.volume;

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンド
    }

    /// <summary>基準音量に対する倍率 (0~1) で音量を設定する。</summary>
    public void SetVolumeScale(float scale)
    {
        EnsureInitialized();
        audioSource.volume = baseVolume * Mathf.Clamp01(scale);
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
        // ���݂͓����SE���~����@�\�͎������Ă��܂���B
        // �K�v�ɉ����Ċg�����Ă��������B
        audioSource.Stop();
    }
}
