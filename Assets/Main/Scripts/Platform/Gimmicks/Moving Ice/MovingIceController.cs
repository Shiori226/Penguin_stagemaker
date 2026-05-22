using UnityEngine;

public class MovingIceController : MonoBehaviour
{
    private enum State
    {
        None,
        FloatingUp,
        Moving,
        SinkingDown,
    }
    [SerializeField]
    private State currentState;
    private float elapsedTime = 0;
    private float topY;
    private float bottomY;

    [Header("Movement Settings")]
    private Vector3 moveVelocity;
    private Vector3 angularVelocity;
    private float lifeTime;

    [Header("FloatSink Settings")]
    [SerializeField, Min(0.0f)] private float floatSinkDistance = 1.0f;
    [SerializeField, Min(0.0f)] private float floatSinkDuration = 1.0f;

    private Collider iceCollider;

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        switch (currentState)
        {
            case State.None:
                gameObject.SetActive(false);
                break;
            case State.FloatingUp:
                UpdateFloating();
                break;
            case State.Moving:
                UpdateMoving();
                break;
            case State.SinkingDown:
                UpdateSinking();
                break;
        }
    }

    private void UpdateFloating()
    {
        // 浮いてくる
        float t = Mathf.Clamp01(elapsedTime / floatSinkDuration);

        // 位置更新 （下から上）
        var pos = transform.position;
        pos.y = Mathf.LerpUnclamped(bottomY, topY, t);
        transform.position = pos;

        if (elapsedTime >= floatSinkDuration)
        {
            NextState(State.Moving);
        }
    }
    private void UpdateMoving()
    {
        // 移動中
        transform.Translate(moveVelocity * Time.deltaTime, Space.World);
        transform.Rotate(angularVelocity * Time.deltaTime, Space.World);

        if(elapsedTime >= lifeTime)
        {
            NextState(State.SinkingDown);
        }
    }
    private void UpdateSinking()
    {
        // 沈んでいく
        float t = Mathf.Clamp01(elapsedTime / floatSinkDuration);

        // 位置更新 （上から下）
        var pos = transform.position;
        pos.y = Mathf.LerpUnclamped(topY, bottomY, t);
        transform.position = pos;

        if (elapsedTime >= floatSinkDuration)
        {
            NextState(State.None);
        }
    }

    private void NextState(State nextState)
    {
        currentState = nextState;
        elapsedTime = 0;
        if(nextState == State.Moving)
        {
            // 移動中は当たり判定を有効化
            iceCollider.enabled = true;
        }
        else
        {
            // それ以外は当たり判定を無効化
            iceCollider.enabled = false;
        }
    }

    public void Init(Vector3 spawnPosition, Vector3 moveVelocity, Vector3 angularVelocity, float lifeTime)
    {
        if (iceCollider == null) { iceCollider = GetComponent<Collider>(); }
        this.moveVelocity = moveVelocity;
        this.angularVelocity = angularVelocity;
        this.lifeTime = lifeTime;
        topY = spawnPosition.y;
        bottomY = topY - floatSinkDistance;
        spawnPosition.y = bottomY;
        transform.position = spawnPosition;
        NextState(State.FloatingUp);
    }
}
