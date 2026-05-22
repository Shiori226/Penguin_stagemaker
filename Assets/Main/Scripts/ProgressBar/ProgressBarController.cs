using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ProgressBarController : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private Transform start;
    private Vector3 startPosition;

    [SerializeField] private Transform goal;
    private Vector3 goalPosition;

    private RectTransform rectTransform;
    private Slider slider;
    private float stageDistance;

    CheckPointController checkPointController;

    private void Start()
    {
        // Progress Bar の初期状態を生成
        InitProgressBar();
        checkPointController.Initialize();
        checkPointController.GenerateCheckPointUI(rectTransform, startPosition, stageDistance);

        // 開始時点の進捗率を Progress Bar に反映
        ApplyPlayerProgress();
    }

    private void LateUpdate()
    {
        ApplyPlayerProgress();
    }

    private void InitProgressBar()
    {
        if (!player) { player = GameObject.FindGameObjectWithTag("Player").transform; }
        if (!start) { start = GameObject.FindGameObjectWithTag("Start").transform; }
        if (!goal) { goal = GameObject.FindGameObjectWithTag("Goal").transform; }

        rectTransform = GetComponent<RectTransform>();
        slider = GetComponent<Slider>();
        checkPointController = GetComponentInChildren<CheckPointController>();

        // スタート地点・ゴール地点・ステージの長さをキャッシュ
        startPosition = start.position;
        goalPosition = goal.position;
        stageDistance = Vector3.Distance(startPosition, goalPosition);

        // スライダーの下限と上限を設定
        slider.minValue = 0.0f;
        slider.maxValue = 1.0f;
    }

    private void ApplyPlayerProgress()
    {
        // プレイヤの進んだ距離を取得し、進捗率に変換
        float playerCurrentDistance = Vector3.Distance(player.position, startPosition);
        float playerCurrentProgress = playerCurrentDistance / stageDistance;

        // プレイヤの進捗率を、Progress Bar（Slider）に反映
        slider.value = playerCurrentProgress;
    }
}
