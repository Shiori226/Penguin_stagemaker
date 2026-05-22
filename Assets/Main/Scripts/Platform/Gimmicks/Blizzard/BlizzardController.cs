using UnityEngine;

public class BlizzardController : MonoBehaviour
{
    [SerializeField]
    private BlizzardData blizzardData;

    private ParticleSystem particle;

    private void OnValidate()
    {
        // エディタ上でインスペクターの値の変更をリアルタイムに反映（開発用）
        ApplyWind();
    }

    private void Start()
    {
        ApplyWind();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var playerController = collision.gameObject.GetComponent<PlayerController>();
            Vector3 addedVelocity = blizzardData.ParticleWindVelocity.normalized * blizzardData.PlayerPushValue;
            playerController.AddVelocity(addedVelocity);
        }
    }

    /// <summary>
    /// ステージメーカーから風向き / 強さ / エリアを上書きするためのエントリポイント。
    /// </summary>
    public void OverrideWind(Vector3 windVelocity, float pushValue, Vector3 areaScale)
    {
        if (blizzardData == null) { blizzardData = new BlizzardData(); }
        blizzardData.SetParticleWindVelocity(windVelocity);
        blizzardData.SetPlayerPushValue(pushValue);
        if (areaScale.sqrMagnitude > 0.0001f)
        {
            blizzardData.SetWindAreaScale(areaScale);
        }
        ApplyWind();
    }

    private void ApplyWind()
    {
        if (!particle) { particle = GetComponentInChildren<ParticleSystem>(); }
        if (!particle) { return; }

        var main = particle.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // 風向の単位ベクトル
        Vector3 windVel = blizzardData.ParticleWindVelocity;
        Vector3 windDir = windVel.sqrMagnitude > 0.0001f ? windVel.normalized : Vector3.forward;

        // パーティクル発生エリアを「風上側」にオフセットして、
        // 中央から出るのではなく逆側 → 中央 → 風下 と流れて見えるようにする。
        var shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = blizzardData.WindAreaScale;
        const float UpwindOffset = 1.5f;
        shape.position = -windDir * UpwindOffset;

        var velocity = particle.velocityOverLifetime;
        velocity.x = windVel.x;
        velocity.y = windVel.y;
        velocity.z = windVel.z;
        velocity.space = ParticleSystemSimulationSpace.Local;
    }
}
