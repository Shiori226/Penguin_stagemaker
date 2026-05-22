using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera will follow")]
    [SerializeField]
    private Transform target;
    private Rigidbody targetRigidbody;

    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    [Tooltip("How quickly the camera moves to the target position (higher is snappier)")]
    public float smoothSpeed = 0.125f;
    private Vector3 offset;

    [Header("Zoom Settings")]
    [Tooltip("Field of view when the target is at minimum speed")]
    public float minFOV = 60f;
    [Tooltip("Field of view when the target is at maximum speed")]
    public float maxFOV = 80f;
    [Tooltip("Speed at which the camera reaches maxFOV")]
    public float maxSpeedForZoom = 20f;
    [Tooltip("Smoothing for field-of-view changes")]
    public float fovSmoothSpeed = 2f;

    private Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
        
        if (target == null)
        {
            Debug.LogError("CameraFollower: No target assigned.");
            return;
        }

        targetRigidbody = target.GetComponent<Rigidbody>();
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        // ���\�b�h������
        if (target == null) { return; }

        // 1. Smooth follow
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        smoothedPosition.y = offset.y; // Maintain consistent height
        transform.position = smoothedPosition;

        // 2. Dynamic zoom based on speed
        if (targetRigidbody != null)
        {
            float speed = targetRigidbody.linearVelocity.magnitude;
            float t = Mathf.Clamp01(speed / maxSpeedForZoom);
            float targetFOV = Mathf.Lerp(minFOV, maxFOV, t);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
        }
    }

    public void ChangeFollowTarget(Transform newTargetTransform)
    {
        target = newTargetTransform;
    }

    public void SetSmoothSpeed(float newSmoothSpeed)
    {
        smoothSpeed = newSmoothSpeed;
    }
}
