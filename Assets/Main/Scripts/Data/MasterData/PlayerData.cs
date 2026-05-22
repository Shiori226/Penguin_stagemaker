using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [Header("ˆÚ“®İ’è")]
    public float maxSpeed = 15.0f;
    public float acceleration = 1.5f;
    public float deceleration = 0.1f;
    public float speedUpDuration = 0.7f;
    public float speedUpMultiplier = 3.0f;

    [Header("ƒŠƒXƒ|[ƒ“İ’è")]
    public RespawnMode respawnMode = RespawnMode.NearestPlatform;
    public float respawnDelay = 1.0f;
}