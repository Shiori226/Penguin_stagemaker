using UnityEngine;

public enum BgmType
{
    Title,
    StageIntro,
    InGame,
}

[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour
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

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンド
    }

    /// <summary>基準音量に対する倍率 (0~1) で音量を設定する。</summary>
    public void SetVolumeScale(float scale)
    {
        EnsureInitialized();
        audioSource.volume = baseVolume * Mathf.Clamp01(scale);
    }

    public void Change(BgmType bgmType) 
    {
        audioSource.Stop(); 
        audioSource.pitch = 1.0f; // Reset pitch to normal when changing BGM

        switch (bgmType) 
        {
            case BgmType.Title:
                audioSource.clip = Resources.Load<AudioClip>("Audio/BGM/TitleBGM");
                break;
            case BgmType.StageIntro:
                audioSource.clip = Resources.Load<AudioClip>("Audio/BGM/StageIntroBGM");
                break;
            case BgmType.InGame:
                audioSource.clip = Resources.Load<AudioClip>("Audio/BGM/InGameBGM");
                break;
            default:
                Debug.LogError("Invalid BGM Type");
                return;
        }
        Invoke(nameof(Play), 0.0f); // Delay to ensure clip is set before playing
    }

    public void Play()
    {
        audioSource.Play();
    }

    public void Stop() 
    {
        audioSource.Stop();
    }

    public void Pause()
    {
        audioSource.Pause();
    }

    public void UnPause()
    {
        audioSource.UnPause();
    }

    public void SetPitch(float pitch)
    {
        audioSource.pitch = pitch;
    }
}
