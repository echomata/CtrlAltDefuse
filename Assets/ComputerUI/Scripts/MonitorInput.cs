using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CtrlAltDefuse.Desktop
{
    [RequireComponent(typeof(Canvas), typeof(MonitorRaycaster))]
    public sealed class MonitorInput : MonoBehaviour
    {
        public RelayDesktop desktop;
        public RectTransform cursor;
        public GameObject roomCrosshair;
        [Min(.1f)] public float pointerSpeed = 1;
        public bool Engaged { get; private set; }

        RectTransform screen;
        MonitorRaycaster raycaster;
        MonitorRaycaster.MonitorPointer pointer;
        readonly List<RaycastResult> hits = new();
        Camera view;
        CameraController look;
        GameObject hover, pressed;
        Vector2 position;
        Vector3 roomPosition;
        Quaternion roomRotation;
        float roomNearClip;
        bool lookWasEnabled, crosshairWasActive;
        CursorLockMode savedLock;
        bool savedVisibility;

        void Start()
        {
            screen = (RectTransform)transform;
            raycaster = GetComponent<MonitorRaycaster>();
            view = GetComponent<Canvas>().worldCamera;
            if (!view) view = Camera.main;
            if (!view) return;
            GetComponent<Canvas>().worldCamera = view;
            look = view.GetComponent<CameraController>();
            CenterCursor();
            cursor.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            var mouse = Mouse.current;
            if (!view || mouse == null) return;
            if (!Engaged)
            {
                if (mouse.leftButton.wasPressedThisFrame || Keyboard.current?.eKey.wasPressedThisFrame == true)
                {
                    Vector2 aim = view.pixelRect.center;
                    float range = look ? look.interactDistance : 5f;
                    if (Vector3.Distance(view.transform.position, screen.position) <= range &&
                        Vector3.Dot(screen.position - view.transform.position, screen.forward) > 0 &&
                        view.WorldToScreenPoint(screen.position).z > 0 &&
                        RectTransformUtility.RectangleContainsScreenPoint(screen, aim, view))
                        Enter();
                }
                return;
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                Leave();
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * pointerSpeed;
            if (desktop.Hijacked && !desktop.Sleeping)
                delta = desktop.DistortPointer(delta, Time.unscaledDeltaTime);
            MoveCursor(position + delta);
            Dispatch(mouse);
        }

        public void Enter()
        {
            if (Engaged || !view || !EventSystem.current) return;
            Engaged = true;
            pointer = new MonitorRaycaster.MonitorPointer(EventSystem.current);
            roomPosition = view.transform.position;
            roomRotation = view.transform.rotation;
            roomNearClip = view.nearClipPlane;
            savedLock = Cursor.lockState;
            savedVisibility = Cursor.visible;
            lookWasEnabled = look && look.enabled;
            if (look) look.enabled = false;
            crosshairWasActive = roomCrosshair && roomCrosshair.activeSelf;
            if (roomCrosshair) roomCrosshair.SetActive(false);
            float height = screen.TransformVector(Vector3.up * screen.rect.height).magnitude;
            float width = screen.TransformVector(Vector3.right * screen.rect.width).magnitude;
            float distance = Mathf.Max(height, width / view.aspect) * .55f / Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            view.nearClipPlane = Mathf.Min(roomNearClip, distance * .25f);
            view.transform.SetPositionAndRotation(screen.position - screen.forward * distance, screen.rotation);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursor.gameObject.SetActive(true);
        }

        public void CenterCursor()
        {
            MoveCursor(Vector2.zero);
        }

        void MoveCursor(Vector2 next)
        {
            Rect area = ((RectTransform)transform).rect;
            position = new Vector2(Mathf.Clamp(next.x, area.xMin + 1, area.xMax - 2),
                Mathf.Clamp(next.y, area.yMin + 2, area.yMax - 1));
            cursor.anchoredPosition = position;
        }

        void Dispatch(Mouse mouse)
        {
            hits.Clear();
            pointer.position = view.WorldToScreenPoint(screen.TransformPoint(position));
            raycaster.Raycast(pointer, hits);
            var target = hits.Count > 0 ? hits[0].gameObject : null;
            pointer.pointerCurrentRaycast = hits.Count > 0 ? hits[0] : default;
            var nextHover = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(target);
            if (nextHover != hover)
            {
                if (hover) ExecuteEvents.Execute(hover, pointer, ExecuteEvents.pointerExitHandler);
                hover = nextHover;
                if (hover) ExecuteEvents.Execute(hover, pointer, ExecuteEvents.pointerEnterHandler);
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (target && wheel != 0)
            {
                pointer.scrollDelta = Vector2.up * Mathf.Clamp(wheel, -3, 3);
                ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.scrollHandler);
            }
            if (mouse.leftButton.wasPressedThisFrame && target)
            {
                target.GetComponentInParent<DesktopWindow>()?.Focus();
                pressed = ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerDownHandler);
            }
            if (mouse.leftButton.wasReleasedThisFrame && pressed)
            {
                ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
                if (pressed == ExecuteEvents.GetEventHandler<IPointerClickHandler>(target))
                {
                    ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerClickHandler);
                    desktop.PlayClick();
                }
                pressed = null;
            }
        }

        public void Leave()
        {
            if (!Engaged) return;
            Engaged = false;
            if (hover) ExecuteEvents.Execute(hover, pointer, ExecuteEvents.pointerExitHandler);
            if (pressed) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
            pressed = hover = null;
            if (view)
            {
                view.nearClipPlane = roomNearClip;
                view.transform.SetPositionAndRotation(roomPosition, roomRotation);
            }
            if (look) look.enabled = lookWasEnabled;
            if (roomCrosshair) roomCrosshair.SetActive(crosshairWasActive);
            cursor.gameObject.SetActive(false);
            Cursor.lockState = savedLock;
            Cursor.visible = savedVisibility;
        }

        void OnDisable() => Leave();
    }
}