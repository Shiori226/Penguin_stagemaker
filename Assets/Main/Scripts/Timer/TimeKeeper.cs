using UnityEngine;

public class TimeKeeper
{
    private float elapsedTime;
    private bool isRunning;
    private float limitTime;
    public bool IsTimeUp { get; private set; } = false;

    public TimeKeeper(float limitTime)
    {
        this.limitTime = limitTime;
        elapsedTime = 0f;
        isRunning = false;
        IsTimeUp = false;
    }

    public void StartTime()
    {
        isRunning = true;
    }

    public void StopTime()
    {
        isRunning = false;
    }

    public void UpdateTime(float deltaTime)
    {
        if (isRunning)
        {
            elapsedTime += deltaTime;
            if (elapsedTime >= limitTime)
            {
                elapsedTime = limitTime;
                isRunning = false; // タイマーが終了したら停止
                IsTimeUp = true; // タイムアップフラグを立てる
            }
        }
    }

    public float GetRemainingTime()
    {
        return limitTime - elapsedTime;
    }

    public float GetRemainingRatio()
    {
        return Mathf.Clamp01((limitTime - elapsedTime) / limitTime);
    }
}
