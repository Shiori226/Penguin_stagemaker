using UnityEngine;

public static class PauseUtility
{
    public static bool IsPause { get; private set; } = false;

    public static void Pause()
    {
        if (IsPause) { return; }

        // 時間・オーディオ・ログ記録
        Time.timeScale = 0f;
        AudioManager.Instance.bgm.Pause();

        // ログ記録
        DataLogger.Instance.PauseTrial();

        IsPause = true;
        Debug.Log("Paused");
    }

    public static void Unpause()
    {
        if (!IsPause) { return; }

        // 時間・オーディオ
        Time.timeScale = 1.0f;
        AudioManager.Instance.bgm.UnPause();

        // ログ記録
        DataLogger.Instance.ResumeTrial();

        IsPause = false;
        Debug.Log("Unpaused");
    }
}
