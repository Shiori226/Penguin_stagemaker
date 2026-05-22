using System.Collections;
using UnityEngine;

public class GameCountdownState : IGameState
{
    private bool isActive = false;

    private CountDownController countDownController;

    private Coroutine contextCoroutine;

    public void OnEnter(GameStateMachine context)
    {
        Debug.Log("Enter GameCountdownState");

        isActive = true;

        context.PlayingCanvas.SetActive(false);
        context.PauseCanvas.SetActive(false);
        context.CountDownCanvas.SetActive(true);
        context.GameOverCanvas.SetActive(false);

        context.CameraSwitcher.SwitchCamera(CameraSwitcher.CameraType.PlayerCamera);
        AudioManager.Instance.bgm.Stop();

        var seals = GameObject.FindObjectsOfType<SealController>();
        foreach (var seal in seals)
        {
            seal.ResetTransform();
        }

        var spawners = GameObject.FindObjectsOfType<MovingIceSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.ResetSpawner();
        }

        countDownController = context.CountDownController;

        contextCoroutine = context.StartCoroutine(CountdownSequence(context));
    }

    public void OnExecute(GameStateMachine context)
    {
        if (countDownController.IsInitialized == false)
        {
            return;
        }

        countDownController.UpdateCountDown();

        if (countDownController.IsCountDownFinished)
        {
            context.ChangeState(new GamePlayingState());
        }
    }

    public void OnExit(GameStateMachine context)
    {
        Debug.Log("Exit GameCountdownState");

        isActive = false;

        if (contextCoroutine != null)
        {
            context.StopCoroutine(contextCoroutine);
            contextCoroutine = null;
        }

        context.CountDownCanvas.SetActive(false);
        countDownController.FinishCountDown();
    }

    public void OnSuspend()
    {

    }
    public void OnResume()
    {

    }

    private IEnumerator CountdownSequence(GameStateMachine context)
    {
        // 少し待ってからカウントダウン開始
        yield return new WaitForSeconds(1f);
        if (context == null || !Application.isPlaying || !isActive) { yield break; }

        countDownController.InitializeState();
        contextCoroutine = null;
    }
}