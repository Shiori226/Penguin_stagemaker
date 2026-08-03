/// <summary>
/// プレイログで使用するイベント名。
/// 入力の物理キーではなく、ゲーム上の論理操作を表す。
/// </summary>
public static class PlayLogEventTypes
{
    public const string TrialStart = "trial_start";
    public const string TrialEnd = "trial_end";
    public const string Pause = "pause";
    public const string Resume = "resume";

    public const string Input = "input";
    public const string FallDetected = "fall_detected";
    public const string Respawn = "respawn";
    public const string WallCollision = "wall_collision";
    public const string WallReflect = "wall_reflect";
    public const string ObstacleCollision = "obstacle_collision";
    public const string SealCollision = "seal_collision";
    public const string IceReached = "ice_reached";
    public const string CheckpointReached = "checkpoint_reached";
    public const string FishCollected = "fish_collected";
    public const string GoalEnter = "goal_enter";
    public const string StageClear = "stage_clear";
    public const string TimeUp = "time_up";
    public const string GameOver = "game_over";
}
