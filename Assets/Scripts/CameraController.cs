using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static IEnumerator MoveTo(Transform view, Vector3 position, Quaternion rotation, Action onArrival,
        Transform orbitCenter = null)
    {
        const float duration = .5f;
        Vector3 startPosition = view.position;
        Quaternion startRotation = view.rotation;
        // Blend the points being looked at while keeping the camera outside the case.
        float startRadius = orbitCenter ? Vector3.Distance(startPosition, orbitCenter.position) : 0f;
        float endRadius = orbitCenter ? Vector3.Distance(position, orbitCenter.position) : 0f;
        Vector3 startAim = startPosition + startRotation * Vector3.forward * startRadius;
        Vector3 endAim = position + rotation * Vector3.forward * endRadius;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            if (Time.timeScale == 0f) continue;
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            var direction = Quaternion.Slerp(startRotation, rotation, progress);
            var aim = Vector3.Lerp(startAim, endAim, progress);
            var radius = Mathf.Lerp(startRadius, endRadius, progress);
            view.SetPositionAndRotation(aim - direction * Vector3.forward * radius, direction);
        }
        onArrival();
    }

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