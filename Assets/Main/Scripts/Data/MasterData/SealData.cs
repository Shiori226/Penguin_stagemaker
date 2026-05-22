using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SealData
{
    [Header("移動設定")]
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>(); // 辿る座標
    [SerializeField, Min(0.01f)] private float smoothTime = 1.0f; // 各点への移動時間
    [SerializeField, Min(0f)] private float waitSeconds = 1f; // 各点での待機時間
    [SerializeField] private float turnSpeed = 360f; // 回転速度（度/秒）

    public List<Vector3> Waypoints => waypoints;
    public float SmoothTime => smoothTime;
    public float WaitSeconds => waitSeconds;
    public float TurnSpeed => turnSpeed;

    // ステージメーカーで動的に上書きするためのセッタ
    public void SetWaypoints(IEnumerable<Vector3> newWaypoints)
    {
        waypoints.Clear();
        if (newWaypoints != null) { waypoints.AddRange(newWaypoints); }
    }
    public void SetSmoothTime(float v) { smoothTime = Mathf.Max(0.01f, v); }
    public void SetWaitSeconds(float v) { waitSeconds = Mathf.Max(0f, v); }
}
