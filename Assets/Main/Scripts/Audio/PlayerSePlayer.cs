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

    private void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンド
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
        // 現在は特定のSEを停止する機能は実装していません。
        // 必要に応じて拡張してください。
        audioSource.Stop();
    }
}
