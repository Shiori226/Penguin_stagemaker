using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [SerializeField]
    private bool isReached = false;
    public bool IsReached { get { return isReached; } }

    [SerializeField]
    private bool isCheckPoint = false;
    public bool IsCheckPoint { get { return isCheckPoint; } }

    private void Awake()
    {
        // Every ice platform is a checkpoint for the sliding penguin.
        // Keep the serialized field for compatibility with existing stages,
        // but normalize it so generated and hand-authored stages match.
        isCheckPoint = true;
        isReached = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            bool isFirstReach = !isReached;
            MarkReached(isFirstReach);

            if (isFirstReach)
            {
                PlayerRespawnController respawner = collision.gameObject.GetComponent<PlayerRespawnController>();
                if (respawner == null)
                {
                    respawner = collision.gameObject.GetComponentInParent<PlayerRespawnController>();
                }

                respawner?.RegisterCheckpoint(this);
            }
        }
    }

    public void ResetForNewPlay()
    {
        isCheckPoint = true;
        isReached = false;
        UpdateCheckpointUi(false);
    }

    public void MarkReached(bool playEffect)
    {
        isReached = true;
        UpdateCheckpointUi(true);

        if (playEffect)
        {
            PlayCheckpointEffect();
        }
    }

    public Vector3 GetCenterRespawnPosition(BoxCollider playerCollider)
    {
        Collider platformCollider = GetComponent<Collider>();
        Bounds platformBounds = platformCollider != null ? platformCollider.bounds : new Bounds(transform.position, Vector3.zero);
        bool hasBounds = platformBounds.size.sqrMagnitude > 0.0001f;

        Vector3 position = hasBounds ? platformBounds.center : transform.position;
        float platformTop = hasBounds ? platformBounds.max.y : transform.position.y;

        float playerHalfHeight = 0.15f;
        if (playerCollider != null)
        {
            playerHalfHeight = Mathf.Abs(playerCollider.size.y * playerCollider.transform.lossyScale.y) * 0.5f;
        }

        position.y = platformTop + playerHalfHeight + 0.05f;
        return position;
    }

    private void UpdateCheckpointUi(bool reached)
    {
        if (CheckPointController.checkPointDict != null &&
            CheckPointController.checkPointDict.TryGetValue(this, out var checkPointUiController))
        {
            checkPointUiController.SetStatus(reached);
        }
    }

    private void PlayCheckpointEffect()
    {
        CheckpointEffect effect = GetComponentInChildren<CheckpointEffect>();
        if (effect == null)
        {
            GameObject effectObject = new GameObject("CheckpointEffect");
            effectObject.transform.SetParent(transform, false);
            effectObject.transform.localPosition = Vector3.up * 0.55f;
            effect = effectObject.AddComponent<CheckpointEffect>();
        }

        effect.Play();
    }
}
