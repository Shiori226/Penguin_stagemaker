using UnityEngine;

/// <summary>
/// プレイヤーの連続状態をDataLoggerのStreamへ追加する。
/// 位置は既存のLogDataManagerが担当するため、ここでは補助的な状態だけを記録する。
/// </summary>
public sealed class PlayLogPlayerTelemetry : MonoBehaviour
{
    private PlayerMover playerMover;
    private PlayerGroundChecker groundChecker;

    [StreamData("player_velocity", Order = 10)]
    private Vector3 PlayerVelocity => playerMover != null ? playerMover.velocity : Vector3.zero;

    [StreamData("input_direction", Order = 11)]
    private Vector3 InputDirection => InputDataManager.Instance != null
        ? InputDataManager.Instance.inputData.direction
        : Vector3.zero;

    [StreamData("player_grounded", Order = 12)]
    private bool IsGrounded => groundChecker != null && groundChecker.isGroundedBuffered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if (FindObjectOfType<PlayLogPlayerTelemetry>() != null)
        {
            return;
        }

        var telemetryObject = new GameObject("PlayLogPlayerTelemetry");
        DontDestroyOnLoad(telemetryObject);
        telemetryObject.AddComponent<PlayLogPlayerTelemetry>();
    }

    private void Awake()
    {
        RefreshPlayerReferences();
    }

    private void Update()
    {
        if (playerMover == null || groundChecker == null)
        {
            RefreshPlayerReferences();
        }
    }

    private void RefreshPlayerReferences()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            playerMover = null;
            groundChecker = null;
            return;
        }

        playerMover = player.GetComponent<PlayerMover>();
        groundChecker = player.GetComponent<PlayerGroundChecker>();
    }
}
