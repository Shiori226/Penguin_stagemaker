using System.Collections;
using UnityEngine;

public class GameCourseIntroState : IGameState
{
    private bool isActive = false;

    private CourseCameraController courseCameraController;

    private Coroutine contextCoroutine;

    public void OnEnter(GameStateMachine context)
    {
        Debug.Log("Enter GameCourseIntroState");

        isActive = true;

        context.PlayingCanvas.SetActive(false);
        context.PauseCanvas.SetActive(false);
        context.CountDownCanvas.SetActive(false);
        context.GameOverCanvas.SetActive(false);

        AudioManager.Instance.bgm.Change(BgmType.StageIntro);

        context.CameraSwitcher.SwitchCamera(CameraSwitcher.CameraType.CourseCamera);
        courseCameraController = context.CameraSwitcher.GetComponentInChildren<CourseCameraController>();
        courseCameraController.Init();

        contextCoroutine = context.StartCoroutine(CourseIntroSequence(context));
    }

    public void OnExecute(GameStateMachine context)
    {
        if (courseCameraController.IsInitialized == true)
        {
            courseCameraController.UpdateIntroCamera();
        }

        if (courseCameraController.IsIntroductionFinished || Input.anyKeyDown)
        {
            context.ChangeState(new GameCountdownState());
        }
    }

    public void OnExit(GameStateMachine context)
    {
        Debug.Log("Exit GameCourseIntroState");

        isActive = false;

        if (contextCoroutine != null)
        {
            context.StopCoroutine(contextCoroutine);
            contextCoroutine = null;
        }
    }

    public void OnSuspend()
    {

    }

    public void OnResume()
    {

    }

    private IEnumerator CourseIntroSequence(GameStateMachine context)
    {
        yield return new WaitForSeconds(2f);
        if (context == null || !Application.isPlaying || !isActive) { yield break; }

        courseCameraController.InitializeState();
        contextCoroutine = null;
    }
}