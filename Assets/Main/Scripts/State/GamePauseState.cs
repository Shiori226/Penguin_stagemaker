public class GamePauseState : IGameState
{
    public void OnEnter(GameStateMachine context)
    {
        // �|�[�YUI��\������
        context.PauseCanvas.SetActive(true);

        // �|�[�Y����
        PauseUtility.Pause();
    }

    public void OnExecute(GameStateMachine context)
    {
        // �|�[�Y�����̓��͂�҂�
        if (InputDataManager.Instance.inputData.pause)
        {
            // �������g���X�^�b�N����~�낷
            context.PopState();
        }
    }

    public void OnExit(GameStateMachine context)
    {
        // ポーズを解除する
        PauseUtility.Unpause();

        // ポーズUIを非表示にする (ただし Setting モードでは ParameterEditor を表示し続けたいので残す)
        context.PauseCanvas.SetActive(context.StageGenerator.IsSettingMode);
    }

    public void OnSuspend() { }
    public void OnResume() { }
}
