using UnityEngine;

public class PlayerGroundChecker : MonoBehaviour
{
    [Tooltip("空中に出てからも接地扱いを続ける猶予（秒）")]
    private float coyoteTime = 0.1f;

    [Tooltip("接地とみなすレイヤー（地面・移動床など）")]
    private LayerMask groundMask;

    private float probeThickness = 0.04f;
    private float lateralMargin = 0f;
    private float timer;

    public bool isGroundedBuffered => timer > 0f;

    public void Initialize(float coyoteTime)
    {
        this.coyoteTime = coyoteTime;
        groundMask = LayerMask.GetMask("Platform");
        ResetTimer();
    }

    public void ResetTimer()
    {
        timer = Mathf.Infinity;
    }

    public void UpdateGroundedState(BoxCollider boxCollider)
    {
        // 接地判定の更新
        bool isGrounded = CheckGroundedWithThinBox(boxCollider);
        if (isGrounded)
        {
            // 接地中は猶予時間をリセット
            timer = coyoteTime;
        }
        else
        {
            // 非接地中は猶予時間を減少
            timer -= Time.fixedDeltaTime;
        }
    }

    public bool CheckGroundedWithThinBox(BoxCollider boxCollider)
    {
        // BoxCollider のワールド中心と半径（半サイズ）を取得
        // 注意：BoxCollider.size はローカル値。lossyScale を掛けてワールド化します。
        Vector3 worldCenter = transform.TransformPoint(boxCollider.center);

        Vector3 lossy = transform.lossyScale;
        lossy.x = Mathf.Abs(lossy.x);
        lossy.y = Mathf.Abs(lossy.y);
        lossy.z = Mathf.Abs(lossy.z);

        Vector3 half = Vector3.Scale(boxCollider.size * 0.5f, lossy);

        // “薄い箱”の半サイズを作る（Yだけ極薄）
        float halfYProbe = Mathf.Max(0.001f, probeThickness * 0.5f);
        Vector3 halfProbe = new Vector3(
            Mathf.Max(0.001f, half.x - lateralMargin),
            halfYProbe,
            Mathf.Max(0.001f, half.z - lateralMargin)
        );

        // 薄い箱の中心：底面から halfYProbe だけ下（キャラの up 方向を基準）
        Vector3 up = transform.up;
        Vector3 probeCenter = worldCenter - up * (half.y - halfYProbe/2f);

        // 回転はオブジェクトの回転を使用（傾き対応）
        Quaternion rot = transform.rotation;

        // トリガーは無視
        return Physics.CheckBox(
            probeCenter, halfProbe, rot, groundMask, QueryTriggerInteraction.Ignore
        );
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider boxCollider = null;
        if (!boxCollider) boxCollider = GetComponent<BoxCollider>();
        if (!boxCollider) return;

        Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
        Vector3 lossy = transform.lossyScale;
        lossy.x = Mathf.Abs(lossy.x);
        lossy.y = Mathf.Abs(lossy.y);
        lossy.z = Mathf.Abs(lossy.z);

        Vector3 half = Vector3.Scale(boxCollider.size * 0.5f, lossy);
        float halfYProbe = Mathf.Max(0.001f, probeThickness * 0.5f);
        Vector3 halfProbe = new Vector3(
            Mathf.Max(0.001f, half.x - lateralMargin),
            halfYProbe,
            Mathf.Max(0.001f, half.z - lateralMargin)
        );

        Vector3 up = transform.up;
        Vector3 probeCenter = worldCenter - up * (half.y - halfYProbe/2f);
        Quaternion rot = transform.rotation;

        // 可視化：薄い箱
        Gizmos.color = coyoteTime < 0f ? Color.green : Color.red;
        // Box をワイヤーで描く（簡易：回転対応のワイヤー描画）
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(probeCenter, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfProbe * 2f);
        Gizmos.matrix = prev;
    }

    public LayerMask GetGroundMask()
    {
        return groundMask;
    }
}
