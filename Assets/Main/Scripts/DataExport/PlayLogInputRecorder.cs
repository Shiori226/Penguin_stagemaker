using UnityEngine;

/// <summary>
/// ゲームプレイ中の論理入力の遷移を記録する。
/// WASD と矢印キーは Input Manager の同じ軸として扱い、物理キー名は保存しない。
/// </summary>
public sealed class PlayLogInputRecorder : MonoBehaviour
{
    private bool previousMoveLeft;
    private bool previousMoveRight;
    private bool previousMoveDown;
    private bool previousMoveUp;
    private bool previousAccelerate;
    private bool previousPause;
    private bool wasTrialActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if (FindObjectOfType<PlayLogInputRecorder>() != null)
        {
            return;
        }

        var recorderObject = new GameObject("PlayLogInputRecorder");
        DontDestroyOnLoad(recorderObject);
        recorderObject.AddComponent<PlayLogInputRecorder>();
    }

    private void Update()
    {
        bool isPaused = PauseUtility.IsPause;
        bool moveLeft = !isPaused && Input.GetAxisRaw("Horizontal") < -0.5f;
        bool moveRight = !isPaused && Input.GetAxisRaw("Horizontal") > 0.5f;
        bool moveDown = !isPaused && Input.GetAxisRaw("Vertical") < -0.5f;
        bool moveUp = !isPaused && Input.GetAxisRaw("Vertical") > 0.5f;
        bool accelerate = !isPaused && Input.GetButton("Submit");
        bool pause = Input.GetButton("Cancel");

        DataLogger logger = DataLogger.Instance;
        if (logger == null || !logger.IsTrialInProgress)
        {
            wasTrialActive = false;
            UpdatePreviousStates(moveLeft, moveRight, moveDown, moveUp, accelerate, pause);
            return;
        }

        if (!wasTrialActive)
        {
            RecordHeldInput("move_left", moveLeft);
            RecordHeldInput("move_right", moveRight);
            RecordHeldInput("move_down", moveDown);
            RecordHeldInput("move_up", moveUp);
            RecordHeldInput("accelerate", accelerate);
            RecordHeldInput("pause", pause);
            UpdatePreviousStates(moveLeft, moveRight, moveDown, moveUp, accelerate, pause);
            wasTrialActive = true;
            return;
        }

        RecordTransition("move_left", moveLeft, ref previousMoveLeft);
        RecordTransition("move_right", moveRight, ref previousMoveRight);
        RecordTransition("move_down", moveDown, ref previousMoveDown);
        RecordTransition("move_up", moveUp, ref previousMoveUp);
        RecordTransition("accelerate", accelerate, ref previousAccelerate);
        RecordTransition("pause", pause, ref previousPause);
    }

    private static void RecordHeldInput(string inputName, bool isHeld)
    {
        if (!isHeld)
        {
            return;
        }

        DataLogger.Instance.RecordEvent(
            PlayLogEventTypes.Input,
            actorId: "player",
            inputName: inputName,
            inputPhase: "held",
            inputValue: 1f);
    }

    private static void RecordTransition(string inputName, bool current, ref bool previous)
    {
        if (current != previous)
        {
            DataLogger.Instance.RecordEvent(
                PlayLogEventTypes.Input,
                actorId: "player",
                inputName: inputName,
                inputPhase: current ? "down" : "up",
                inputValue: current ? 1f : 0f);
        }

        previous = current;
    }

    private void UpdatePreviousStates(
        bool moveLeft,
        bool moveRight,
        bool moveDown,
        bool moveUp,
        bool accelerate,
        bool pause)
    {
        previousMoveLeft = moveLeft;
        previousMoveRight = moveRight;
        previousMoveDown = moveDown;
        previousMoveUp = moveUp;
        previousAccelerate = accelerate;
        previousPause = pause;
    }
}
