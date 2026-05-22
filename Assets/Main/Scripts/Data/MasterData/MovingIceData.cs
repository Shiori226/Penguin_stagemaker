using UnityEngine;

[System.Serializable]
public class MovingIceData
{
    [SerializeField]
    private float spawnSpan;
    [SerializeField]
    private Vector3 moveVelocity;
    [SerializeField]
    private Vector3 angularVelocity;
    [SerializeField]
    private float lifeTime;

    public float SpawnSpan => spawnSpan;
    public Vector3 MoveVelocity => moveVelocity;
    public Vector3 AngularVelocity => angularVelocity;
    public float LifeTime => lifeTime;
}
