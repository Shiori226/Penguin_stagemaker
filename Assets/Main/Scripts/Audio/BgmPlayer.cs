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

    void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        
        audioSource.loop = true; 
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2DƒTƒEƒ“ƒh
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
