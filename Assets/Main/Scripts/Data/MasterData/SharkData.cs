using System;
using UnityEngine;

[Serializable]
public class SharkData
{
    [SerializeField]
    private bool isActive;
    [SerializeField]
    private float speed;

    public bool IsActive => isActive;
    public float Speed => speed;

    public SharkData(bool isActive, float speed)
    {
        this.isActive = isActive;
        this.speed = speed;
    }
}
