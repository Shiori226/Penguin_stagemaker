using TMPro;
using UnityEngine;

public class PopupTextController : MonoBehaviour
{
    // ===== スコアポップアップの描画設定用の変数 =====

    private TMP_Text label;

    private Transform followTarget;
    private Vector3 offset = Vector3.up;

    private float baseYPosition = 0f;

    // 出現してから自動で表示になるまでの時間（寿命）
    private readonly float lifeTime = 0.80f;

    // ===== アニメーション用の変数 =====

    private float elapsed = 0f;

    [SerializeField]
    private float riseValue = 1f;

    [SerializeField]
    private float alphaEasePower = 2f;

    // ===== スケール変更用 =====

    private Camera targetCamera;
    private float initialFontSize = 1f;
    private Vector2 scaleClamp = new Vector2(4.0f, 8.0f);

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifeTime);

        UpdatePostion(t);
        UpdateColor(t);
        UpdateScale();
    }

    // ターゲットに対して上昇しながら追従する
    private void UpdatePostion(float t)
    {
        Vector3 pos = followTarget.position + offset;
        //pos.y = baseYPosition + (riseValue * t);
        pos.y += (riseValue * t);

        transform.position = pos;
    }

    // 徐々に透明化していく
    private void UpdateColor(float t)
    {
        // イージングをかけた進行度
        float easedAlpha = Mathf.Pow(t, alphaEasePower);
        // 現在の透明度
        float currentAlpha = Mathf.Lerp(1f, 0f, easedAlpha);

        Color c = label.color;
        c.a = currentAlpha;
        label.color = c;
    }

    // カメラからの距離に応じてスケールを調整する
    private void UpdateScale()
    {
        if(targetCamera == null) { return; }
        float distance = Vector3.Distance(targetCamera.transform.position, transform.position);
        float scaleFactor = Mathf.Clamp(distance, scaleClamp.x, scaleClamp.y); // 距離に基づくスケール調整
        label.fontSize = initialFontSize * scaleFactor; // フォントサイズも調整
    }

    public void Init(Transform targetTransform, string baseScoreText, Color32 baseScoreColor)
    {
        if (!label) { label = GetComponent<TMP_Text>(); }
        followTarget = targetTransform;

        label.text = baseScoreText;
        label.color = baseScoreColor;

        baseYPosition = (targetTransform.position + offset).y;

        targetCamera = Camera.main;

        elapsed = 0f;
    }

    private void OnEnable()
    {
        // 一定時間後に非表示にする
        Invoke(nameof(DisableSelf), lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
