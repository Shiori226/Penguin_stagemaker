using UnityEngine;

/// <summary>
/// プレイイベントを試行単位の統計値へ集計する。
/// 個々の発生時刻はevents_trialN.csvに残し、ここではSnapshot用の合計値を提供する。
/// </summary>
public sealed class PlayLogStatistics : MonoBehaviour
{
    private int normalFishCount;
    private int goldFishCount;
    private int totalFishCount;
    private int sealCollisionCount;
    private int wallCollisionCount;
    private int wallReflectCount;
    private int obstacleCollisionCount;
    private int fallCount;
    private int respawnCount;
    private int iceReachedCount;
    private int checkpointReachedCount;
    private bool goalReached;
    private bool stageCleared;
    private bool gameOver;
    private bool timeUp;
    private double firstFishTime = -1.0;
    private double lastFishTime = -1.0;
    private double firstIceTime = -1.0;
    private double goalTime = -1.0;
    private bool subscribed;

    [SnapshotData("stats_fish_normal", Order = 10)]
    private int NormalFishCount => normalFishCount;

    [SnapshotData("stats_fish_gold", Order = 11)]
    private int GoldFishCount => goldFishCount;

    [SnapshotData("stats_fish_total", Order = 12)]
    private int TotalFishCount => totalFishCount;

    [SnapshotData("stats_seal_collision", Order = 20)]
    private int SealCollisionCount => sealCollisionCount;

    [SnapshotData("stats_wall_collision", Order = 21)]
    private int WallCollisionCount => wallCollisionCount;

    [SnapshotData("stats_wall_reflect", Order = 22)]
    private int WallReflectCount => wallReflectCount;

    [SnapshotData("stats_obstacle_collision", Order = 23)]
    private int ObstacleCollisionCount => obstacleCollisionCount;

    [SnapshotData("stats_fall_count", Order = 30)]
    private int FallCount => fallCount;

    [SnapshotData("stats_respawn_count", Order = 31)]
    private int RespawnCount => respawnCount;

    [SnapshotData("stats_ice_reached", Order = 32)]
    private int IceReachedCount => iceReachedCount;

    [SnapshotData("stats_checkpoint_reached", Order = 33)]
    private int CheckpointReachedCount => checkpointReachedCount;

    [SnapshotData("stats_goal_reached", Order = 40)]
    private bool GoalReached => goalReached;

    [SnapshotData("stats_stage_cleared", Order = 41)]
    private bool StageCleared => stageCleared
        || (ScoreManager.Instance != null && ScoreManager.Instance.isStageCleared);

    [SnapshotData("stats_game_over", Order = 42)]
    private bool GameOver => gameOver
        || (!StageCleared && DataLogger.Instance != null && DataLogger.Instance.IsTrialInProgress);

    [SnapshotData("stats_time_up", Order = 43)]
    private bool TimeUp => timeUp;

    [SnapshotData("stats_first_fish_time", Order = 50)]
    private double FirstFishTime => firstFishTime;

    [SnapshotData("stats_last_fish_time", Order = 51)]
    private double LastFishTime => lastFishTime;

    [SnapshotData("stats_first_ice_time", Order = 52)]
    private double FirstIceTime => firstIceTime;

    [SnapshotData("stats_goal_time", Order = 53)]
    private double GoalTime => goalTime;

    [SnapshotData("stats_play_time", Order = 60)]
    private double PlayTime => DataLogger.Instance != null
        ? DataLogger.Instance.ElapsedSeconds
        : 0.0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if (FindObjectOfType<PlayLogStatistics>() != null)
        {
            return;
        }

        var statisticsObject = new GameObject("PlayLogStatistics");
        DontDestroyOnLoad(statisticsObject);
        statisticsObject.AddComponent<PlayLogStatistics>();
    }

    private void Awake()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    private void OnDestroy()
    {
        if (subscribed && DataLogger.Instance != null)
        {
            DataLogger.Instance.EventRecorded -= OnEventRecorded;
        }
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || DataLogger.Instance == null)
        {
            return;
        }

        DataLogger.Instance.EventRecorded += OnEventRecorded;
        subscribed = true;
    }

    private void OnEventRecorded(PlayLogEvent item)
    {
        if (item == null)
        {
            return;
        }

        if (item.eventType == PlayLogEventTypes.TrialStart)
        {
            Reset();
            return;
        }

        switch (item.eventType)
        {
            case PlayLogEventTypes.FishCollected:
                totalFishCount++;
                if (firstFishTime < 0.0) { firstFishTime = item.time; }
                lastFishTime = item.time;
                if (item.itemName == "FishGold") { goldFishCount++; }
                else if (item.itemName == "FishNormal") { normalFishCount++; }
                break;
            case PlayLogEventTypes.SealCollision:
                sealCollisionCount++;
                break;
            case PlayLogEventTypes.WallCollision:
                wallCollisionCount++;
                break;
            case PlayLogEventTypes.WallReflect:
                wallReflectCount++;
                break;
            case PlayLogEventTypes.ObstacleCollision:
                obstacleCollisionCount++;
                break;
            case PlayLogEventTypes.FallDetected:
                fallCount++;
                break;
            case PlayLogEventTypes.Respawn:
                respawnCount++;
                break;
            case PlayLogEventTypes.IceReached:
                iceReachedCount++;
                if (firstIceTime < 0.0) { firstIceTime = item.time; }
                break;
            case PlayLogEventTypes.CheckpointReached:
                checkpointReachedCount++;
                break;
            case PlayLogEventTypes.GoalEnter:
                goalReached = true;
                if (goalTime < 0.0) { goalTime = item.time; }
                break;
            case PlayLogEventTypes.StageClear:
                stageCleared = true;
                break;
            case PlayLogEventTypes.GameOver:
                gameOver = true;
                break;
            case PlayLogEventTypes.TimeUp:
                timeUp = true;
                break;
        }
    }

    private void Reset()
    {
        normalFishCount = 0;
        goldFishCount = 0;
        totalFishCount = 0;
        sealCollisionCount = 0;
        wallCollisionCount = 0;
        wallReflectCount = 0;
        obstacleCollisionCount = 0;
        fallCount = 0;
        respawnCount = 0;
        iceReachedCount = 0;
        checkpointReachedCount = 0;
        goalReached = false;
        stageCleared = false;
        gameOver = false;
        timeUp = false;
        firstFishTime = -1.0;
        lastFishTime = -1.0;
        firstIceTime = -1.0;
        goalTime = -1.0;
    }
}
