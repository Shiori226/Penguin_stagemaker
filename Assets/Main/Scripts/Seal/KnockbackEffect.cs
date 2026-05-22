using UnityEngine;

public class KnockbackEffect : MonoBehaviour, ISealEffect
{
    [SerializeField]
    private float knockbackForce = 5.0f;

    public void ApplyEffect(Collider other)
    {
        AudioManager.Instance.playerSe.Play(SeTypePlayer.Boing);

        // プレイヤーにノックバックを与える
        var player = other.GetComponent<PlayerController>();
        Vector3 direction = (other.transform.position - transform.position).normalized;
        direction.y = 0;
        player.SetVelocity(direction * knockbackForce);
    }
}
