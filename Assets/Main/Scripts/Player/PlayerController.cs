using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMover), typeof(PlayerRespawnController), typeof(PlayerGroundChecker))]
public class PlayerController : MonoBehaviour
{
    // ===== プレイヤー設定 =====
    [SerializeField]
    private string playerName = "Player01";
    [SerializeField]
    private PlayerData playerData;

    // ===== プレイヤー状態管理 =====
    private enum PlayerState
    {
        Idle,
        Moving,
    }
    private PlayerState currentState = PlayerState.Idle;
    private bool isSpeedUp = false;
    private bool isFalled = false;

    private float coyoteTime = 0.1f;

    // ===== コンポーネント参照 =====
    private PlayerMover mover;
    private PlayerGroundChecker groundChecker;
    private PlayerRespawnController respawner;

    private BoxCollider boxCollider;
    private Animator animator;

    private GameStateMachine gameStateMachine;

    // ===== 落下時の処理 =====
    ScoreData courseOutData;

    public void Initialize()
    {
        playerData = DataManager.Instance.playerData[playerName];
        courseOutData = DataManager.Instance.scoreData["CourseOut"];

        mover = GetComponent<PlayerMover>();
        groundChecker = GetComponent<PlayerGroundChecker>();
        respawner = GetComponent<PlayerRespawnController>();

        boxCollider = GetComponent<BoxCollider>();
        animator = GetComponent<Animator>();
        
        groundChecker.Initialize(coyoteTime);
        respawner.Initialize(playerData);

        gameStateMachine = FindObjectOfType<GameStateMachine>();
    }

    // 移動処理
    public void Update()
    {
        Vector3 direction = InputDataManager.Instance.inputData.direction;

        // 入力がない場合はIdle状態へ
        currentState = direction.magnitude <= 0.1f ? PlayerState.Idle : PlayerState.Moving;
        if (isFalled) { currentState = PlayerState.Idle; }

        switch (currentState)
        {
            // 待機状態
            case PlayerState.Idle:
                animator.SetBool("IsMoving", false);
                mover.SetAcceleration(playerData.deceleration);
                break;
            // 移動状態
            case PlayerState.Moving:
                if (InputDataManager.Instance.inputData.submit)
                {
                    // 加速入力
                    RequestSpeedUp(playerData.speedUpDuration);
                }
                animator.SetBool("IsMoving", true);
                mover.SetAcceleration(playerData.acceleration * (isSpeedUp ? playerData.speedUpMultiplier : 1f));
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                break;
        }

        Vector3 target = direction * playerData.maxSpeed;
        Vector3 velocity = mover.CalcVelocity(target);
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    // 落下判定
    private void FixedUpdate()
    {
        groundChecker.UpdateGroundedState(boxCollider);
        if (!groundChecker.isGroundedBuffered)
        {
            RequestFalled(playerData.respawnDelay);
        }
    }

    // ゴール判定
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            currentState = PlayerState.Idle;
            Vector3 goalPos = other.transform.position;
            RequestGoal(goalPos);
            gameStateMachine.ChangeState(new GameClearState());
        }
    }

    //=================== 加速時処理 ===================
    Coroutine speedUpCoroutine;

    private void RequestSpeedUp(float dur)
    {
        if (speedUpCoroutine != null) { StopCoroutine(speedUpCoroutine); }
        speedUpCoroutine = StartCoroutine(SpeedUpCoroutine(dur));
    }

    private IEnumerator SpeedUpCoroutine(float dur)
    {
        animator.SetTrigger("Acceleration");
        AudioManager.Instance.accelPlayerSe.Play();
        isSpeedUp = true;
        yield return new WaitForSeconds(dur);
        isSpeedUp = false;
        speedUpCoroutine = null;
    }

    //================ 落下時処理 ===================
    Coroutine FallCoroutine;
    private void RequestFalled(float dur)
    {
        if (FallCoroutine != null) { return; }
        FallCoroutine = StartCoroutine(FalledCoroutine(dur));
    }

    private IEnumerator FalledCoroutine(float dur)
    {
        transform.SetParent(null);
        
        ScoreManager.Instance.AddCount("CourseOut");
        AudioManager.Instance.playerSe.Play(SeTypePlayer.DropWater);
        AudioManager.Instance.se.Play(SeTypeSystem.Miss);

        // スコアポップアップを生成
        float baseScore = courseOutData.baseScore;
        Color32 displayColor = courseOutData.displayColor;
        PopupTextSpawner.Instance.SpawnPopupText(transform, baseScore, displayColor);

        currentState = PlayerState.Idle;
        boxCollider.enabled = false;
        isFalled = true;
        yield return new WaitForSeconds(dur);
        currentState = PlayerState.Moving;
        boxCollider.enabled = true;
        isFalled = false;
        groundChecker.ResetTimer();
        respawner.Respawn();
        mover.Stop();
        FallCoroutine = null;
    }

    //================ ゴール処理 ===================
    Coroutine goalCoroutine;

    private void RequestGoal(Vector3 goalPos)
    {
        if (goalCoroutine != null) { return; }
        goalCoroutine = StartCoroutine(GoalCoroutine(goalPos));
    }

    private IEnumerator GoalCoroutine(Vector3 goalPos)
    {
        currentState = PlayerState.Idle;
        Vector3 direction = (goalPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        bool reached = false;
        while (!reached)
        {
            transform.Translate(direction * mover.velocity.magnitude * Time.deltaTime, Space.World);
            reached = Vector3.Distance(transform.position, goalPos) < 0.5f;
            yield return null;
        }

        gameObject.SetActive(false);
        goalCoroutine = null;
    }

    //================ 外部からの速度制御 ===================
    public void SetVelocity(Vector3 newVelocity)
    {
        mover.SetVelocity(newVelocity);
    }

    public void AddVelocity(Vector3 addVelocity)
    {
        mover.AddVelocity(addVelocity);
    }
}
