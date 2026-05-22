using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClearState : IGameState
{
    private Coroutine contextCoroutine;

    public void OnEnter(GameStateMachine context)
    {
        Debug.Log("Enter GameClearState");

        // 試遊モード: 演出・結果なしでステージ制作画面に即帰還
        if (StageGenerator.IsTestPlay)
        {
            StageGenerator.SetTestPlay(false);
            context.PlayingCanvas.SetActive(false);
            context.PauseCanvas.SetActive(false);
            context.CountDownCanvas.SetActive(false);
            context.GameOverCanvas.SetActive(false);
            AudioManager.Instance.bgm.Stop();
            SceneManager.LoadScene("StageMaker");
            return;
        }

        ScoreManager.Instance.SetCleared(true);

        if (!context.StageGenerator.IsSettingMode)
        {
            // ログのスナップショットを記録 & ログのストリーム記録を終了
            DataLogger.Instance.CaptureSnapshot();
            DataLogger.Instance.StopStream();
            DataLogger.Instance.EndTrial();
        }

        context.PlayingCanvas.SetActive(false);
        context.PauseCanvas.SetActive(false);
        context.CountDownCanvas.SetActive(false);
        context.GameOverCanvas.SetActive(false);

        contextCoroutine = context.StartCoroutine(GameClearSequence(context));
    }

    public void OnExecute(GameStateMachine context)
    {

    }

    public void OnExit(GameStateMachine context)
    {
        Debug.Log("Exit GameClearState");

        if (contextCoroutine != null)
        {
            context.StopCoroutine(contextCoroutine);
            contextCoroutine = null;
        }

        SceneManager.LoadScene("Result");
    }

    public void OnSuspend()
    {

    }

    public void OnResume()
    {

    }

    private IEnumerator GameClearSequence(GameStateMachine context)
    {
        if (context.GoalObject)
        {
            context.PlayerCameraController.ChangeFollowTarget(context.GoalObject.transform);
            context.PlayerCameraController.SetSmoothSpeed(0.010f);
        }

        AudioManager.Instance.bgm.Stop();
        AudioManager.Instance.se.Play(SeTypeSystem.Applause);

        yield return new WaitForSeconds(2f);

        if (context == null || !Application.isPlaying)
        {
            yield break;
        }

        AudioManager.Instance.se.Play(SeTypeSystem.Success);

        yield return new WaitForSeconds(2f);

        if (context == null || !Application.isPlaying)
        {
            yield break;
        }

        contextCoroutine = null;
        context.PopState();
    }
}