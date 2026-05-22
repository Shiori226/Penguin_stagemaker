using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AccelerationPlayerSePlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip accelerateClip;

    private void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンド

        accelerateClip = Resources.Load<AudioClip>("Audio/SE/Player/AccelerateSE");
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
        // 現在は特定のSEを停止する機能は実装していません。
        // 必要に応じて拡張してください。
        audioSource.Stop();
    }
}
