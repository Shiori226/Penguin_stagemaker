using System;
using UnityEngine;

[Serializable]
public class TimerData
{
    [Header("タイマー設定")]
    [Min(0.0f)]
    public float timeLimit = 80.0f;
    public float warningThreshold = 20f;
}
