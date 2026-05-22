using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CountDownController : MonoBehaviour
{
    private Image countDownImage;

    [SerializeField]
    public List<Sprite> countDownSprites = new List<Sprite>();

    private float timer = 0f;
    public bool IsCountDownFinished { get; private set; } = false;
    public bool IsInitialized { get; private set; } = false;

    public void InitializeState()
    {
        IsCountDownFinished = false;
        timer = countDownSprites.Count;

        countDownImage = GetComponent<Image>();
        countDownImage.enabled = true;
        AudioManager.Instance.se.Play(SeTypeSystem.CountDownBeep);
        countDownImage.sprite = countDownSprites[(int)timer - 1];
        IsInitialized = true;
    }

    public void SetCountDown(int count)
    {
        if (count < 0 || count >= countDownSprites.Count)
        {
            IsCountDownFinished = true;
            return;
        }
        countDownImage.sprite = countDownSprites[count];
    }

    public void UpdateCountDown()
    {
        timer -= Time.deltaTime;
        int count = Mathf.CeilToInt(timer) - 1;
        SetCountDown(count);
    }

    public void FinishCountDown()
    {
        countDownImage.enabled = false;
    }
}
