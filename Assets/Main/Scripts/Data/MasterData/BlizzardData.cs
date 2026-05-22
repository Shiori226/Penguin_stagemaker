using UnityEngine;

[System.Serializable]
public class BlizzardData
{
    [SerializeField]
    private float playerPushValue;
    [SerializeField]
    private Vector3 particleWindVelocity;
    [SerializeField]
    private Vector3 windAreaScale;

    public float PlayerPushValue => playerPushValue;
    public Vector3 ParticleWindVelocity => particleWindVelocity;
    public Vector3 WindAreaScale => windAreaScale;

    // 実行時に上書き可能なセッタ (Stage Maker で配置するときに使う)
    public void SetParticleWindVelocity(Vector3 v) { particleWindVelocity = v; }
    public void SetPlayerPushValue(float v) { playerPushValue = v; }
    public void SetWindAreaScale(Vector3 v) { windAreaScale = v; }
}
