using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensitivity = 0.15f;
    public float smoothSpeed = 12f;

    [Header("Rotation Limits (Degrees)")]
    public float maxYaw = 35f;
    public float maxPitch = 25f;

    [Header("Interaction")]
    public float interactDistance = 5f;
    public LayerMask interactableLayer = ~0;

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

        Vector3 initialAngles = transform.localEulerAngles;
        startPitch = initialAngles.x;
        startYaw = initialAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        HandleCameraLook();
    }

    private void HandleCameraLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        targetYaw += mouseDelta.x * sensitivity;
        targetPitch -= mouseDelta.y * sensitivity;

        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * smoothSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * smoothSpeed);

        transform.localEulerAngles = new Vector3(
            startPitch + currentPitch,
            startYaw + currentYaw,
            0f
        );
    }
    
}