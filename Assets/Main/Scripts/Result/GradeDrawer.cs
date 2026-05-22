using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class GradeDrawer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    private Animator animator;
    private bool animEnded;

    private void OnEnable()
    {
        if (!animator) { animator = GetComponent<Animator>(); }
        animEnded = false;
    }

    /// <summary>
    /// アニメーションなしで即座に最終結果を表示。
    /// </summary>
    public void SetInstant(string gradeLabel, Color gradeColor)
    {
        if (!animator) { animator = GetComponent<Animator>(); }

        // 値を即座に設定
        labelText.text = gradeLabel;
        iconImage.color = gradeColor;

        // 実行中のコルーチンをすべて停止
        StopAllCoroutines();

        animator.Play("Display Grade", 0, 1.0f);
        animator.Update(0.0f); // 強制的に即時反映させる
        animator.enabled = false;
    }

    public IEnumerator DrawGrade(string gradeLabel, Color gradeColor)
    {
        // 引数から要素をセット
        labelText.text = gradeLabel;
        iconImage.color = gradeColor;

        animEnded = false;

        // アニメーションを再生
        animator.ResetTrigger("Display");
        animator.SetTrigger("Display");

        // アニメーションが終わるまで待つ
        yield return new WaitUntil(() => animEnded);

        // 効果音を再生
        AudioManager.Instance.se.Play(SeTypeSystem.DisplayScore);
    }

    // Animation Event で呼び出される関数
    public void OnAnimationFinished()
    {
        animEnded = true;
    }
}
