using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public BgmPlayer bgm { get; private set; }
    public SystemSePlayer se { get; private set; }
    public PlayerSePlayer playerSe { get; private set; }
    public AccelerationPlayerSePlayer accelPlayerSe { get; private set; }

    private const string BgmVolumePrefsKey = "BgmVolume";
    private const string SeVolumePrefsKey = "SeVolume";

    /// <summary>BGM 音量 (0~1、プレハブの基準音量への倍率)</summary>
    public float BgmVolume { get; private set; } = 1f;

    /// <summary>効果音の音量 (0~1、プレハブの基準音量への倍率)</summary>
    public float SeVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            bgm = GetComponentInChildren<BgmPlayer>();
            se = GetComponentInChildren<SystemSePlayer>();
            playerSe = GetComponentInChildren<PlayerSePlayer>();
            accelPlayerSe = GetComponentInChildren<AccelerationPlayerSePlayer>();

            // 保存済みの音量設定を復元して適用
            ApplyBgmVolume(PlayerPrefs.GetFloat(BgmVolumePrefsKey, 1f));
            ApplySeVolume(PlayerPrefs.GetFloat(SeVolumePrefsKey, 1f));

            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>BGM 音量 (0~1) を設定して保存する。</summary>
    public void SetBgmVolume(float volume)
    {
        ApplyBgmVolume(volume);
        PlayerPrefs.SetFloat(BgmVolumePrefsKey, BgmVolume);
        PlayerPrefs.Save();
    }

    /// <summary>効果音の音量 (0~1) を設定して保存する。</summary>
    public void SetSeVolume(float volume)
    {
        ApplySeVolume(volume);
        PlayerPrefs.SetFloat(SeVolumePrefsKey, SeVolume);
        PlayerPrefs.Save();
    }

    private void ApplyBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        if (bgm != null) { bgm.SetVolumeScale(BgmVolume); }
    }

    private void ApplySeVolume(float volume)
    {
        SeVolume = Mathf.Clamp01(volume);
        if (se != null) { se.SetVolumeScale(SeVolume); }
        if (playerSe != null) { playerSe.SetVolumeScale(SeVolume); }
        if (accelPlayerSe != null) { accelPlayerSe.SetVolumeScale(SeVolume); }
    }
}
