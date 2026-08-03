using System;
using UnityEngine;

/// <summary>
/// 試行中に発生した1件のイベント。
/// 時刻・フレーム・順序は DataLogger が記録時に付与する。
/// </summary>
[Serializable]
public sealed class PlayLogEvent
{
    public double time;
    public int frame;
    public int sequence;

    public string eventType;
    public string actorId;
    public string targetId;

    public Vector3? position;
    public Vector3? velocityBefore;
    public Vector3? velocityAfter;
    public Vector3? normal;

    public string inputName;
    public string inputPhase;
    public float? inputValue;
    public string itemName;
    public string reason;
    public string value;
}
