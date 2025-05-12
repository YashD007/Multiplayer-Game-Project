using UnityEngine;

public class AdvancedThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // Reference to the player

    [Header("Offset Settings")]
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f); // Where the camera rotates around
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.5f, -4f); // Offset from pivot (distance behind)

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 70f;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 10f;

    private float yaw;
    private float pitch;

    private Vector3 currentRotation;
    private Vector3 smoothVelocity;

    void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));

        yaw += input.x * mouseSensitivity;
        pitch += input.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void UpdateCameraPosition()
    {
        // Smoothly interpolate rotation
        Vector3 targetRotation = new Vector3(pitch, yaw, 0);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);

        Quaternion rotation = Quaternion.Euler(currentRotation);

        // Final camera position
        Vector3 targetPivotPosition = target.position + pivotOffset;
        Vector3 cameraPosition = targetPivotPosition + rotation * cameraOffset;

        transform.position = Vector3.Lerp(transform.position, cameraPosition, followSpeed * Time.deltaTime);
        transform.LookAt(targetPivotPosition);
    }
}
