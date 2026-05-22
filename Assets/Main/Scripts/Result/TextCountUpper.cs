using System.Collections;
using UnityEngine;
using TMPro;

public class TextCountUpper : MonoBehaviour
{
    [SerializeField] TMP_Text countUpText;
    [SerializeField] bool unscaled = true;
    private Coroutine running;

    /// <summary>
    /// カウントアップを即座に停止し、最終値を設定。
    /// </summary>
    /// <param name="finalValue">最終的な値</param>
    public void SkipTo(float finalValue)
    {
        // 実行中のカウントアップコルーチンがあれば停止
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        // テキストに最終値を設定
        if (countUpText != null)
        {
            countUpText.text = finalValue.ToString("0");
        }
    }

    /// 現在表示中の数値から to までカウント
    public IEnumerator CountTo(float to, float seconds)
    {
        if (countUpText == null) { yield break; }

        float.TryParse(countUpText.text, out float from);

        // すでに走っているものがあれば止める（排他）
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        // 内部実行を開始し、その完了を待つ
        running = StartCoroutine(CountUp(from, to, seconds));
        yield return running;
        running = null;
    }

    private IEnumerator CountUp(float from, float to, float duration)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float v = Mathf.Lerp(from, to, t);
            countUpText.text = v.ToString("0"); // 小数点不要なら "0"、欲しければ "0.00" などに
            yield return null;
        }

        countUpText.text = to.ToString("0");
    }
}