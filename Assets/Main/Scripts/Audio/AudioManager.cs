using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public BgmPlayer bgm { get; private set; }
    public SystemSePlayer se { get; private set; }
    public PlayerSePlayer playerSe { get; private set; }
    public AccelerationPlayerSePlayer accelPlayerSe { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            bgm = GetComponentInChildren<BgmPlayer>();
            se = GetComponentInChildren<SystemSePlayer>();
            playerSe = GetComponentInChildren<PlayerSePlayer>();
            accelPlayerSe = GetComponentInChildren<AccelerationPlayerSePlayer>();

            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
