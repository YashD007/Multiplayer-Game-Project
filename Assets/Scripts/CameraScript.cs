using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit Settings")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-30f, 75f);
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float rotationSmoothTime = 0.05f;

    [Header("Zoom & Collision")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cameraRadius = 0.25f;

    private float yaw = 0f;
    private float pitch = 20f;
    private float currentDistance;
    private float distanceVelocity;
    private Vector3 currentRotation;
    private Vector3 rotationSmoothVelocity;

    private void Start()
    {
        currentDistance = distance;
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateRotation();
        UpdatePosition();
    }

    private void HandleInput()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
    }

    private void UpdateRotation()
    {
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(currentRotation);
    }

    private void UpdatePosition()
    {
        Vector3 desiredCameraPos = target.position - transform.rotation * Vector3.forward * distance;

        // Camera collision check
        if (Physics.SphereCast(target.position, cameraRadius, (desiredCameraPos - target.position).normalized, out RaycastHit hit, distance, collisionMask))
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, hit.distance - cameraRadius, ref distanceVelocity, 0.05f);
        }
        else
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, distance, ref distanceVelocity, 0.1f);
        }

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        transform.position = target.position - transform.rotation * Vector3.forward * currentDistance;
    }
}
