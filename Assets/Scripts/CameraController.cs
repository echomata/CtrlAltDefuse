using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensitivity = 0.15f;
    public float smoothSpeed = 12f;

    [Header("Rotation Limits (Degrees)")]
    public float maxYaw = 35f;   // Left / Right limit
    public float maxPitch = 25f; // Up / Down limit

    [Header("Interaction")]
    public float interactDistance = 5f;
    public LayerMask interactableLayer = ~0; // Defaults to Everything

    private float startYaw;
    private float startPitch;
    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Store the camera's initial rotation as the center point
        Vector3 initialAngles = transform.localEulerAngles;
        startPitch = initialAngles.x;
        startYaw = initialAngles.y;

        // Lock and hide the system cursor so the UI crosshair takes over
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCameraLook();
    }

    private void HandleCameraLook()
    {
        if (Mouse.current == null) return;

        // Read mouse movement delta from the New Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        targetYaw += mouseDelta.x * sensitivity;
        targetPitch -= mouseDelta.y * sensitivity;

        // Clamp rotation so the player can only look around the desk
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

        // Smoothly interpolate rotation for a weighty, natural feel
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * smoothSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * smoothSpeed);

        transform.localEulerAngles = new Vector3(
            startPitch + currentPitch,
            startYaw + currentYaw,
            0f
        );
    }
    
}
