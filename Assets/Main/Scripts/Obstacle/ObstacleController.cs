using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [SerializeField]
    private float knockbackForce = 3.0f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // プレイヤーにノックバックを与える
            var player = collision.gameObject.GetComponent<PlayerController>();
            Vector3 direction = (collision.transform.position - transform.position).normalized;
            direction.y = 0;
            player.SetVelocity(direction * knockbackForce);
        }
    }
}

