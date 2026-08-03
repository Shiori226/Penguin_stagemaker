using UnityEngine;

/// <summary>
/// Player に対して、壁に沿って滑りつつ壁から反射させる壁の衝突処理。
/// プレイヤーの移動速度は PlayerMover が管理しているため、物理マテリアルの反発には依存しない。
/// </summary>
public class WallController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float normalVelocityRetention = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float tangentVelocityRetention = 0.9f;

    [SerializeField, Min(0f)]
    private float collisionCooldown = 0.08f;

    [SerializeField, Min(0f)]
    private float separationDistance = 0.02f;

    private float nextCollisionTime;

    private void OnCollisionEnter(Collision collision)
    {
        ReflectPlayer(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        ReflectPlayer(collision);
    }

    private void ReflectPlayer(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) { return; }
        if (Time.time < nextCollisionTime) { return; }

        var player = collision.gameObject.GetComponent<PlayerController>();
        var mover = collision.gameObject.GetComponent<PlayerMover>();
        if (player == null || mover == null || collision.contactCount == 0) { return; }

        Vector3 velocity = mover.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.0001f) { return; }

        Vector3 normal = collision.GetContact(0).normal;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f) { return; }
        normal.Normalize();

        // Contact normal の向きは Collider の組み合わせで変わるため、
        // プレイヤーの進行方向と反対側を「壁から外向き」の法線として扱う。
        if (Vector3.Dot(velocity, normal) > 0f)
        {
            normal = -normal;
        }

        float normalSpeed = Vector3.Dot(velocity, normal);
        if (normalSpeed >= -0.01f) { return; }

        Vector3 normalVelocity = normal * normalSpeed;
        Vector3 tangentVelocity = velocity - normalVelocity;
        Vector3 reflectedVelocity = tangentVelocity * tangentVelocityRetention
            - normalVelocity * normalVelocityRetention;

        player.SetVelocity(reflectedVelocity);
        collision.gameObject.transform.position += normal * separationDistance;
        nextCollisionTime = Time.time + collisionCooldown;
    }
}
