public interface IGameState
{
    void OnEnter(GameStateMachine context); // 状態に入った直後
    void OnExecute(GameStateMachine context); // 毎フレームの更新
    void OnExit(GameStateMachine context); // 状態を抜ける直前

    void OnSuspend(); // 一時停止の直前
    void OnResume(); // 一時停止の解除直後
}
