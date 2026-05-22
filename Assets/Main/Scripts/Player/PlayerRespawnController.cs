using UnityEngine;

public enum RespawnMode
{
    NearestPlatform,
    NearestCheckPoint,
}

public class PlayerRespawnController : MonoBehaviour
{
    private PlayerData playerData;

    private GameObject platformsRoot;
    private PlatformController[] platformControllers;

    // 初期化
    public void Initialize(PlayerData playerData)
    {
        this.playerData = playerData;

        if (platformsRoot == null)
        {
            platformsRoot = GameObject.Find("Platforms");
        }
        platformControllers = platformsRoot.GetComponentsInChildren<PlatformController>();
    }

    // 最も近いPlatformの子オブジェクトの位置にリスポーンする
    public void Respawn()
    {
        Transform closestPlatform = GetClosestPlatform(playerData.respawnMode);
        Vector3 respawnPos = GetClosestRespawnPoint(closestPlatform);

        transform.position = respawnPos;
        transform.rotation = Quaternion.identity;
    }

    // 最も近いPlatformを取得する
    // RespawnModeによって、チェックポイントのPlatformのみに絞る
    private Transform GetClosestPlatform(RespawnMode respawnMode)
    {
        Transform closest = null;
        float minDistance = float.MaxValue;
        foreach (var pc in platformControllers)
        {
            if (!pc.IsReached) { continue; }
            if (respawnMode == RespawnMode.NearestCheckPoint && !pc.IsCheckPoint) { continue; }

            float dist = Vector3.Distance(transform.position, pc.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = pc.transform;
            }
        }
        return closest;
    }

    // 指定したPlatformの子オブジェクトの中で、最も近い位置を取得する
    private Vector3 GetClosestRespawnPoint(Transform platform)
    {
        if (platform == null) { return Vector3.up; }
        Vector3 closestPoint = default;

        float minDistance = float.MaxValue;
        foreach (Transform point in platform)
        {
            float dist = Vector3.Distance(transform.position, point.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = point.position;
            }
        }
        return closestPoint;
    }

    public RespawnMode GetRespawnMode()
    {
        return playerData.respawnMode;
    }
}
