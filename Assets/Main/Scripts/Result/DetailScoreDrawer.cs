using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator), typeof(TextCountUpper))]
public class DetailScoreDrawer : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text scoreText;

    private Animator animator;
    private bool animEnded;

    private TextCountUpper countUpper;

    private void OnEnable()
    {
        if (!animator) { animator = GetComponent<Animator>(); }
        animEnded = false;

        if (!countUpper) { countUpper = GetComponent<TextCountUpper>(); }
    }

    /// <summary>
    /// アニメーションなしで即座に最終結果を表示。
    /// </summary>
    public void SetInstant(string label, Sprite icon, int count, float score)
    {
        // コンポーネントがなければ取得
        if (!animator) { animator = GetComponent<Animator>(); }
        if (!countUpper) { countUpper = GetComponent<TextCountUpper>(); }

        // 値を即座に設定
        labelText.text = label;
        iconImage.sprite = icon;
        countText.text = count.ToString();

        // 実行中のコルーチン（DrawDetailScoreなど）をすべて停止
        StopAllCoroutines();

        // カウントアップをスキップして最終値を設定
        countUpper.SkipTo(score);

        animator.Play("Display", 0, 1.0f);
        animator.Update(0.0f); // 強制的に即時反映させる
        animator.enabled = false;
    }

    public IEnumerator DrawDetailScore(string label, Sprite icon, int count, float score)
    {
        // 引数から要素をセット
        labelText.text = label;
        iconImage.sprite = icon;
        countText.text = count.ToString();
        scoreText.text = "0"; // 初期値は0（演出中に目的の値へカウントアップする）

        animEnded = false;

        // アニメーションを再生
        animator.ResetTrigger("Display");
        animator.SetTrigger("Display");

        // アニメーションが終わるまで待つ
        yield return new WaitUntil(() => animEnded);

        // 効果音を再生
        AudioManager.Instance.se.Play(SeTypeSystem.DisplayScore);

        // アニメーションが終わったらスコアカウントアップ処理（本来は第1引数に実際のスコアを設定）
        StartCoroutine(countUpper.CountTo(score, 1.0f));
    }

    // Animation Event で呼び出される関数
    public void OnAnimationFinished()
    {
        animEnded = true;
    }
}
