using UnityEngine;

public enum RespawnMode
{
    NearestPlatform,
    NearestCheckPoint,
}

public class PlayerRespawnController : MonoBehaviour
{
    private PlayerData playerData;
    private PlatformController[] platformControllers;
    private PlatformController currentCheckpoint;
    private BoxCollider playerCollider;

    // 初期化
    public void Initialize(PlayerData playerData)
    {
        this.playerData = playerData;

        // The sliding penguin always uses per-ice checkpoint respawning.
        // Keep RespawnMode for compatibility with existing UI and data.
        this.playerData.respawnMode = RespawnMode.NearestCheckPoint;
        playerCollider = GetComponent<BoxCollider>();
        platformControllers = FindObjectsOfType<PlatformController>();

        foreach (PlatformController platform in platformControllers)
        {
            platform.ResetForNewPlay();
        }

        currentCheckpoint = FindStartPlatform();
        if (currentCheckpoint != null)
        {
            currentCheckpoint.MarkReached(false);
        }
    }

    // 最新のチェックポイントの中央にリスポーンする
    public void Respawn()
    {
        if (!TryGetRespawnPosition(out Vector3 respawnPos))
        {
            respawnPos = Vector3.up;
        }

        transform.SetPositionAndRotation(respawnPos, Quaternion.LookRotation(Vector3.forward, Vector3.up));
    }

    public void RegisterCheckpoint(PlatformController platform)
    {
        if (platform == null || !platform.IsCheckPoint)
        {
            return;
        }

        currentCheckpoint = platform;
    }

    public bool TryGetRespawnPosition(out Vector3 respawnPosition)
    {
        if (currentCheckpoint == null)
        {
            currentCheckpoint = FindStartPlatform();
        }

        if (currentCheckpoint == null)
        {
            respawnPosition = default;
            return false;
        }

        respawnPosition = currentCheckpoint.GetCenterRespawnPosition(playerCollider);
        return true;
    }

    public PlatformController GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    private PlatformController FindStartPlatform()
    {
        GameObject startObject = GameObject.FindGameObjectWithTag("Start");
        if (startObject != null)
        {
            PlatformController startPlatform = startObject.GetComponent<PlatformController>();
            if (startPlatform != null)
            {
                return startPlatform;
            }
        }

        PlatformController firstPlatform = null;
        foreach (PlatformController platform in platformControllers)
        {
            if (firstPlatform == null || platform.transform.position.z < firstPlatform.transform.position.z)
            {
                firstPlatform = platform;
            }
        }

        return firstPlatform;
    }

    public RespawnMode GetRespawnMode()
    {
        return playerData.respawnMode;
    }
}
