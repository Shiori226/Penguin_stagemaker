using UnityEngine.SceneManagement;

public static class SceneLoadUtility
{
    public static void LoadScene(string nextSceneName)
    {
        // 遷移後のSceneで操作不能にならないための保険
        PauseUtility.Unpause();

        // 今鳴っているBGMを止める
        AudioManager.Instance.bgm.Stop();
        // BGMのピッチを元に戻す
        AudioManager.Instance.bgm.SetPitch(1.0f);
        // 今鳴っているシステムSE以外のSEを止める
        AudioManager.Instance.playerSe.Stop();
        AudioManager.Instance.accelPlayerSe.Stop();

        // 試行中のデータを破棄する
        DataLogger.Instance.AbortTrial();

        SceneManager.LoadScene(nextSceneName);
    }

    public static void LoadCurrentScene()
    {
        // 遷移後のSceneで操作不能にならないための保険
        PauseUtility.Unpause();

        // 今鳴っているBGMを止める
        AudioManager.Instance.bgm.Stop();
        // BGMのピッチを元に戻す
        AudioManager.Instance.bgm.SetPitch(1.0f);
        // 今鳴っているシステムSE以外のSEを止める
        AudioManager.Instance.playerSe.Stop();
        AudioManager.Instance.accelPlayerSe.Stop();

        // 試行中のデータを破棄する
        DataLogger.Instance.AbortTrial();

        // 現在のシーンを再読み込み
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
