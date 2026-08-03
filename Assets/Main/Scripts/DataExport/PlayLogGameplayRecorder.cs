using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// プレイヤーに発生するゲームプレイイベントを記録する。
/// 既存のゲームロジックと同じ衝突・トリガー通知を受け取り、ログだけを追加する。
/// </summary>
public sealed class PlayLogGameplayRecorder : MonoBehaviour
{
    private sealed class PendingWallReflection
    {
        public string targetId;
        public Vector3 position;
        public Vector3 velocityBefore;
        public Vector3 normal;
    }

    private readonly HashSet<int> reachedPlatformIds = new();
    private readonly List<PendingWallReflection> pendingWallReflections = new();

    private PlayerMover playerMover;
    private PlayerGroundChecker groundChecker;
    private bool wasTrialActive;
    private bool wasGrounded = true;
    private bool wasAirborne;
    private Vector3 previousPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null || player.GetComponent<PlayLogGameplayRecorder>() != null)
        {
            return;
        }

        player.gameObject.AddComponent<PlayLogGameplayRecorder>();
    }

    private void Awake()
    {
        playerMover = GetComponent<PlayerMover>();
        groundChecker = GetComponent<PlayerGroundChecker>();
        previousPosition = transform.position;
    }

    private void Update()
    {
        DataLogger logger = DataLogger.Instance;
        if (logger == null || !logger.IsTrialInProgress)
        {
            wasTrialActive = false;
            pendingWallReflections.Clear();
            reachedPlatformIds.Clear();
            previousPosition = transform.position;
            return;
        }

        if (!wasTrialActive)
        {
            wasTrialActive = true;
            wasGrounded = IsGrounded();
            wasAirborne = false;
            previousPosition = transform.position;
            reachedPlatformIds.Clear();
            pendingWallReflections.Clear();
            return;
        }

        bool grounded = IsGrounded();
        if (wasGrounded && !grounded)
        {
            wasAirborne = true;
            logger.RecordEvent(
                PlayLogEventTypes.FallDetected,
                actorId: "player",
                position: transform.position,
                velocityBefore: GetVelocity(),
                reason: "left_ice");
        }

        if (wasAirborne && Vector3.Distance(previousPosition, transform.position) > 1f)
        {
            logger.RecordEvent(
                PlayLogEventTypes.Respawn,
                actorId: "player",
                position: transform.position,
                reason: "respawn_position_changed");
            wasAirborne = false;
        }

        wasGrounded = grounded;
        previousPosition = transform.position;

        RecordPendingWallReflections(logger);
    }

    private void OnTriggerEnter(Collider other)
    {
        DataLogger logger = DataLogger.Instance;
        if (logger == null || !logger.IsTrialInProgress)
        {
            return;
        }

        SealController seal = other.GetComponentInParent<SealController>();
        if (seal != null)
        {
            logger.RecordEvent(
                PlayLogEventTypes.SealCollision,
                actorId: "player",
                targetId: GetStableId(seal.transform),
                position: transform.position);
            return;
        }

        CollectibleItem item = other.GetComponentInParent<CollectibleItem>();
        if (item != null)
        {
            string itemName = GetItemName(item);
            logger.RecordEvent(
                PlayLogEventTypes.FishCollected,
                actorId: "player",
                targetId: GetStableId(item.transform),
                position: transform.position,
                itemName: itemName);
            return;
        }

        if (other.CompareTag("Goal"))
        {
            logger.RecordEvent(
                PlayLogEventTypes.GoalEnter,
                actorId: "player",
                targetId: GetStableId(other.transform),
                position: transform.position);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        DataLogger logger = DataLogger.Instance;
        if (logger == null || !logger.IsTrialInProgress || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal;
        Vector3 velocity = GetVelocity();

        WallController wall = collision.collider.GetComponentInParent<WallController>();
        if (wall != null)
        {
            string targetId = GetStableId(wall.transform);
            logger.RecordEvent(
                PlayLogEventTypes.WallCollision,
                actorId: "player",
                targetId: targetId,
                position: contact.point,
                velocityBefore: velocity,
                normal: normal);

            pendingWallReflections.Add(new PendingWallReflection
            {
                targetId = targetId,
                position = contact.point,
                velocityBefore = velocity,
                normal = normal,
            });
            return;
        }

        ObstacleController obstacle = collision.collider.GetComponentInParent<ObstacleController>();
        if (obstacle != null)
        {
            logger.RecordEvent(
                PlayLogEventTypes.ObstacleCollision,
                actorId: "player",
                targetId: GetStableId(obstacle.transform),
                position: contact.point,
                velocityBefore: velocity,
                normal: normal);
            return;
        }

        PlatformController platform = collision.collider.GetComponentInParent<PlatformController>();
        if (platform != null && reachedPlatformIds.Add(platform.GetInstanceID()))
        {
            string targetId = GetStableId(platform.transform);
            logger.RecordEvent(
                PlayLogEventTypes.IceReached,
                actorId: "player",
                targetId: targetId,
                position: contact.point);

            if (platform.IsCheckPoint)
            {
                logger.RecordEvent(
                    PlayLogEventTypes.CheckpointReached,
                    actorId: "player",
                    targetId: targetId,
                    position: contact.point);
            }
        }
    }

    private void RecordPendingWallReflections(DataLogger logger)
    {
        if (pendingWallReflections.Count == 0)
        {
            return;
        }

        Vector3 velocityAfter = GetVelocity();
        foreach (PendingWallReflection pending in pendingWallReflections)
        {
            if ((velocityAfter - pending.velocityBefore).sqrMagnitude < 0.0001f)
            {
                continue;
            }

            logger.RecordEvent(
                PlayLogEventTypes.WallReflect,
                actorId: "player",
                targetId: pending.targetId,
                position: pending.position,
                velocityBefore: pending.velocityBefore,
                velocityAfter: velocityAfter,
                normal: pending.normal);
        }

        pendingWallReflections.Clear();
    }

    private bool IsGrounded()
    {
        return groundChecker != null && groundChecker.isGroundedBuffered;
    }

    private Vector3 GetVelocity()
    {
        return playerMover != null ? playerMover.velocity : Vector3.zero;
    }

    private static string GetItemName(CollectibleItem item)
    {
        FieldInfo field = typeof(CollectibleItem).GetField(
            "itemName", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(item) as string ?? item.name;
    }

    private static string GetStableId(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        var parts = new List<string>();
        Transform current = target;
        while (current != null)
        {
            parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
