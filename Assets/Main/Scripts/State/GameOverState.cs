using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverState : IGameState
{
    private Coroutine contextCoroutine;

    public void OnEnter(GameStateMachine context)
    {
        Debug.Log("Enter GameOverState");

        ScoreManager.Instance.SetCleared(false);

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
        context.GameOverCanvas.SetActive(true);

        AudioManager.Instance.bgm.Stop();
        AudioManager.Instance.se.Play(SeTypeSystem.Failure);

        contextCoroutine = context.StartCoroutine(GameOverSequence(context));
    }

    public void OnExecute(GameStateMachine context)
    {

    }

    public void OnExit(GameStateMachine context)
    {
        Debug.Log("Exit GameOverState");

        if (contextCoroutine != null)
        {
            context.StopCoroutine(contextCoroutine);
            contextCoroutine = null;
        }

        context.GameOverCanvas.SetActive(false);
        SceneManager.LoadScene("Result");
    }

    public void OnSuspend()
    {

    }

    public void OnResume()
    {

    }

    private IEnumerator GameOverSequence(GameStateMachine context)
    {
        yield return new WaitForSeconds(2f);
        if (context == null || !Application.isPlaying) { yield break; }

        contextCoroutine = null;
        context.PopState();
    }
}