using StageMaker;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameStateMachine : MonoBehaviour
{
    private readonly Stack<IGameState> stateStack = new Stack<IGameState>();

    public CameraSwitcher CameraSwitcher { get; private set; }
    public PlayerCameraController PlayerCameraController { get; private set; }
    public CountDownController CountDownController { get; private set; }
    public PlayerController PlayerController { get; private set; }
    public StageGenerator StageGenerator { get; private set; }

    public GameObject GoalObject { get; private set; }

    public GameObject PlayingCanvas { get; private set; }
    public GameObject PauseCanvas {  get; private set; }
    public GameObject CountDownCanvas { get; private set; }
    public GameObject GameOverCanvas { get; private set; }

    private void Start()
    {
        Application.targetFrameRate = 60;

        GameObject cameras = GameObject.Find("Cameras");
        CameraSwitcher = cameras.GetComponent<CameraSwitcher>();

        PlayerCameraController = FindObjectOfType<PlayerCameraController>();

        CountDownController = FindObjectOfType<CountDownController>();

        PlayerController = FindObjectOfType<PlayerController>();
        PlayerController.enabled = false;

        StageGenerator = FindObjectOfType<StageGenerator>();

        GoalObject = GameObject.FindGameObjectWithTag("Goal");

        PlayingCanvas = GameObject.Find("PlayingCanvas");
        PauseCanvas = GameObject.Find("PauseCanvas");
        CountDownCanvas = GameObject.Find("CountDownCanvas");
        GameOverCanvas = GameObject.Find("GameOverCanvas");

        if (StageGenerator.IsTestPlay)
        {
            BuildTestPlayBackButton();
        }

        // 現在の Scene が Setting 用かどうかで遷移先を分岐
        if (StageGenerator.IsSettingMode) { ChangeState(new GamePlayingState()); }
        else { ChangeState(new GameCourseIntroState()); }
    }

    private void BuildTestPlayBackButton()
    {
        var canvasGo = new GameObject("TestPlayOverlay");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<GraphicRaycaster>();
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var (btnGo, btn, _) = StageMakerUIFactory.CreateButton(
            canvasGo, "BackToEditorButton",
            "< Back to Editor",
            Color.white,
            StageMakerUIFactory.IceText,
            new Vector2(260f, 68f));

        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, -16f);

        btn.onClick.AddListener(() =>
        {
            StageGenerator.SetTestPlay(false);
            AudioManager.Instance?.bgm.Stop();
            SceneManager.LoadScene("StageMaker");
        });
    }

    private void Update()
    {
        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnExecute(this);
        }
    }

    public void ChangeState(IGameState newState)
    {
        // CurrentState��null�łȂ��ꍇ�AExit���\�b�h���Ăяo���Č��݂̏�Ԃ��I������
        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnExit(this);
        }

        stateStack.Clear();

        stateStack.Push(newState);

        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnEnter(this);
        }
    }

    public void PushState(IGameState newState)
    {
        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnSuspend();
        }

        stateStack.Push(newState);

        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnEnter(this);
        }
    }

    public void PopState()
    {
        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnExit(this);
            stateStack.Pop();
        }

        if(stateStack.Count > 0)
        {
            stateStack.Peek().OnResume();
        }
    }
}
