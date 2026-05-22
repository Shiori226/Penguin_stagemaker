using UnityEngine;

[RequireComponent (typeof(Animator))]
public class SealController : MonoBehaviour
{
    [SerializeField]
    private SealData sealData;

    [Min(0.0001f)] private float arriveDist = 0.01f; // 到着判定距離

    private enum State
    {
        Waiting,
        Turning,
        Moving,
    }

    private State currentState = State.Waiting;
    
    private int currentIndex = 0; // 現在の目的地
    private float waitTimer = 0; // 待機タイマー
    private Vector3 velocity; // SmoothDamp 用バッファ
    private Animator animator;
    private ISealEffect[] effects;

    private Vector3 initialPosition; // 初期位置
    private Quaternion initialRotation; // 初期回転

    private void Start()
    {
        animator = GetComponent<Animator>();
        effects = GetComponents<ISealEffect>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        EnterWaiting();
    }

    private void Update()
    {
        if (sealData.Waypoints == null || sealData.Waypoints.Count == 0) { return; }

        // 状態ごとの更新
        switch (currentState)
        {
            case State.Waiting:
                UpdateWaiting();
                break;
            case State.Turning:
                UpdateTurning();
                break;
            case State.Moving:
                UpdateMoving();
                break;
        }

        // アニメーションの更新
        // 今後アニメーションが増える場合はステートごとに分ける
        animator.SetBool("Move", currentState == State.Moving);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(other);
            }
 
            // アザラシのアニメーションを再生
            animator.SetTrigger("Bound");
        }
    }

    private void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= sealData.WaitSeconds)
        {
            UpdateIndex();
            waitTimer = 0.0f;
            currentState = State.Turning;
        }
    }

    private void UpdateTurning()
    {
        Vector3 target = sealData.Waypoints[currentIndex];
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        // ほぼ同じ位置にいる場合は回頭せずに移動へ
        if (direction.sqrMagnitude < 0.0001f)
        {
            currentState = State.Moving;
            return;
        }

        // 指定の方向へ向く
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, sealData.TurnSpeed * Time.deltaTime);

        // ほぼ同じ方向を向いたら次の状態へ
        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleDiff < 1f)
        {
            currentState = State.Moving;
        }
    }

    private void UpdateMoving()
    {
        Vector3 target = sealData.Waypoints[currentIndex];

        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, sealData.SmoothTime);

        float distance = Vector3.Distance(transform.position, target);
        if (distance <= arriveDist)
        {
            currentState = State.Waiting;
        }
    }

    private void EnterWaiting()
    {
        waitTimer = 0.0f;
        currentIndex = 0;
        velocity = Vector3.zero; // 念のため速度をリセット
        currentState = State.Waiting;
    }

    private void UpdateIndex()
    {
        if (sealData.Waypoints.Count <= 1) return;
        currentIndex++;
        if (currentIndex >= sealData.Waypoints.Count) currentIndex = 0;
    }

    /// <summary>
    /// ステージメーカーから経由地点 (begin → end → begin → ...) を動的に設定する。
    /// </summary>
    public void OverrideWaypoints(Vector3 begin, Vector3 end)
    {
        if (sealData == null) { sealData = new SealData(); }
        sealData.SetWaypoints(new[] { begin, end });
    }

    public void ResetTransform()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        EnterWaiting();

        if(animator != null)
        {
            animator.SetBool("Move", false); // アニメーションを初期状態に戻す
        }
    }
}
