/// <summary>
/// 試行の時間・フレームの記録用の時計。
/// 試行の開始、一時停止、再開、停止と連動。
/// </summary>
public sealed class RecordingClock
{
    private bool isTrialActive; // 「試行がアクティブか」を管理
    private bool isPaused; // 「一時停止中か」を管理
    private double elapsedSeconds;
    private int elapsedFrames;

    public double ElapsedSeconds => elapsedSeconds;
    public int ElapsedFrames => elapsedFrames;

    /// <summary>
    /// 時計をリセットし、計測を開始。
    /// </summary>
    public void Start()
    {
        if (isTrialActive)
        {
            return;
        }

        Reset();

        isTrialActive = true;
        isPaused = false;
    }

    /// <summary>
    /// 時間を経過（毎フレーム呼び出す想定）。
    /// </summary>
    /// <param name="deltaUnscaled">非スケール時間（Time.unscaledDeltaTime）</param>
    public void Tick(float deltaUnscaled)
    {
        if (!isTrialActive || isPaused)
        {
            return;
        }

        elapsedSeconds += deltaUnscaled;
        elapsedFrames++;
    }

    /// <summary>
    /// 時計の計測を一時停止。
    /// </summary>
    public void Pause()
    {
        if (!isTrialActive)
        {
            return;
        }
        isPaused = true;
    }

    /// <summary>
    /// 時計の計測を再開。
    /// </summary>
    public void Resume()
    {
        if (!isTrialActive)
        {
            return;
        }
        isPaused = false;
    }

    /// <summary>
    /// 時計を停止し、計測値をリセット。
    /// </summary>
    public void Stop()
    {
        if (!isTrialActive)
        {
            return;
        }

        isTrialActive = false;
        isPaused = false;

        Reset();
    }

    public void Reset()
    {
        elapsedSeconds = 0.0;
        elapsedFrames = 0;
    }
}