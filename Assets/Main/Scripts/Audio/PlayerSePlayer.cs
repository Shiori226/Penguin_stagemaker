using UnityEngine;

public enum SeTypePlayer
{
    Accelerate,
    DropWater,
    Capture,
    CaptureGold,
    Boing,
}

[RequireComponent(typeof(AudioSource))]
public class PlayerSePlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private float baseVolume = 1f;   // プレハブに設定された基準音量

    private void Awake()
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

    public void Play(SeTypePlayer seType)
    {
        AudioClip clip = null;

        switch (seType)
        {
            case SeTypePlayer.Accelerate:
                clip = Resources.Load<AudioClip>("Audio/SE/Player/AccelerateSE");
                break;
            case SeTypePlayer.DropWater:
                clip = Resources.Load<AudioClip>("Audio/SE/Player/DropSE");
                break;
            case SeTypePlayer.Capture:
                clip = Resources.Load<AudioClip>("Audio/SE/Player/CaptureSE");
                break;
            case SeTypePlayer.CaptureGold:
                clip = Resources.Load<AudioClip>("Audio/SE/Player/CaptureGoldSE");
                break;
            case SeTypePlayer.Boing:
                clip = Resources.Load<AudioClip>("Audio/SE/Player/BoingSE");
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
