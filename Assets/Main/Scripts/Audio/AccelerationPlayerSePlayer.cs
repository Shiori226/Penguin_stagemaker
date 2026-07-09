using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AccelerationPlayerSePlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip accelerateClip;
    private float baseVolume = 1f;   // プレハブに設定された基準音量

    private void Awake()
    {
        EnsureInitialized();
        accelerateClip = Resources.Load<AudioClip>("Audio/SE/Player/AccelerateSE");
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

    public void Play()
    {
        if (accelerateClip == null)
        {
            Debug.LogError("Accelerate sound clip not found!");
            return;
        }

        audioSource.Stop();
        audioSource.PlayOneShot(accelerateClip);
    }

    public void Stop()
    {
        // ���݂͓����SE���~����@�\�͎������Ă��܂���B
        // �K�v�ɉ����Ċg�����Ă��������B
        audioSource.Stop();
    }
}
