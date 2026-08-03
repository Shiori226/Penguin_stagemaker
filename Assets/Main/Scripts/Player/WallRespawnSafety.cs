using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corrects a respawn that would place the player inside a wall.
/// </summary>
public class WallRespawnSafety : MonoBehaviour
{
    private const float SearchStep = 0.5f;
    private const int SearchRings = 6;
    private const float SupportRayHeight = 2f;
    private const float SupportRayDistance = 5f;

    private static readonly Vector3[] SearchDirections =
    {
        Vector3.right,
        (Vector3.right + Vector3.forward).normalized,
        Vector3.forward,
        (-Vector3.right + Vector3.forward).normalized,
        -Vector3.right,
        (-Vector3.right - Vector3.forward).normalized,
        -Vector3.forward,
        (Vector3.right - Vector3.forward).normalized,
    };

    private BoxCollider playerCollider;
    private PlayerRespawnController respawner;
    private bool wasColliderEnabled;

    private void Start()
    {
        playerCollider = GetComponent<BoxCollider>();
        respawner = GetComponent<PlayerRespawnController>();
        wasColliderEnabled = playerCollider != null && playerCollider.enabled;
    }

    private void LateUpdate()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<BoxCollider>();
        }

        if (playerCollider == null || !playerCollider.enabled)
        {
            wasColliderEnabled = false;
            return;
        }

        if (!wasColliderEnabled)
        {
            CorrectBlockedRespawn();
        }
        wasColliderEnabled = true;
    }

    private void CorrectBlockedRespawn()
    {
        if (respawner == null)
        {
            respawner = GetComponent<PlayerRespawnController>();
        }
        if (respawner == null || !OverlapsWall(transform.position)) { return; }

        if (TryGetSafeRespawnPosition(out Vector3 safePosition))
        {
            transform.SetPositionAndRotation(safePosition, Quaternion.identity);
        }
    }

    private bool TryGetSafeRespawnPosition(out Vector3 respawnPosition)
    {
        respawnPosition = default;
        var platforms = new List<PlatformController>();
        PlatformController[] allPlatforms = FindObjectsOfType<PlatformController>();
        foreach (PlatformController platform in allPlatforms)
        {
            if (!platform.IsReached) { continue; }
            if (respawner.GetRespawnMode() == RespawnMode.NearestCheckPoint && !platform.IsCheckPoint) { continue; }
            platforms.Add(platform);
        }

        while (platforms.Count > 0)
        {
            int closestIndex = 0;
            float closestDistance = SqrDistanceTo(platforms[0].transform.position);
            for (int i = 1; i < platforms.Count; i++)
            {
                float distance = SqrDistanceTo(platforms[i].transform.position);
                if (distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }

            PlatformController platform = platforms[closestIndex];
            platforms.RemoveAt(closestIndex);
            if (TryGetSafeRespawnPoint(platform, out respawnPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetSafeRespawnPoint(PlatformController platform, out Vector3 respawnPosition)
    {
        respawnPosition = default;
        var points = new List<Transform>();
        foreach (Transform point in platform.transform)
        {
            points.Add(point);
        }

        points.Sort((a, b) => SqrDistanceTo(a.position).CompareTo(SqrDistanceTo(b.position)));
        foreach (Transform point in points)
        {
            if (TryFindSafePositionAround(platform, point.position, out respawnPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindSafePositionAround(PlatformController platform, Vector3 origin, out Vector3 respawnPosition)
    {
        for (int ring = 0; ring <= SearchRings; ring++)
        {
            if (ring == 0)
            {
                if (IsSafeRespawnPosition(platform, origin))
                {
                    respawnPosition = origin;
                    return true;
                }
                continue;
            }

            float distance = ring * SearchStep;
            foreach (Vector3 direction in SearchDirections)
            {
                Vector3 candidate = origin + direction * distance;
                candidate.y = origin.y;
                if (IsSafeRespawnPosition(platform, candidate))
                {
                    respawnPosition = candidate;
                    return true;
                }
            }
        }

        respawnPosition = default;
        return false;
    }

    private bool IsSafeRespawnPosition(PlatformController platform, Vector3 position)
    {
        return HasPlatformSupport(platform, position) && !OverlapsWall(position);
    }

    private bool HasPlatformSupport(PlatformController platform, Vector3 position)
    {
        Vector3 rayOrigin = position + Vector3.up * SupportRayHeight;
        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                SupportRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.collider.GetComponentInParent<PlatformController>() == platform;
    }

    private bool OverlapsWall(Vector3 position)
    {
        Vector3 halfExtents = Vector3.Scale(playerCollider.size, transform.lossyScale) * 0.5f;
        Vector3 center = position + Quaternion.identity * Vector3.Scale(playerCollider.center, transform.lossyScale);
        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.identity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap.transform.IsChildOf(transform)) { continue; }
            if (overlap.GetComponentInParent<WallController>() != null) { return true; }
        }

        return false;
    }

    private float SqrDistanceTo(Vector3 position)
    {
        Vector3 delta = transform.position - position;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }
}
