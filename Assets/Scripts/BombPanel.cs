using CtrlAltDefuse.Desktop;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CtrlAltDefuse.Gameplay
{
    [RequireComponent(typeof(CodeLock), typeof(CanvasGroup))]
    public sealed class BombPanel : MonoBehaviour
    {
        public Text countdown, strikes, label, entry, feedback, hint;
        public Button[] digits;
        public Button clear, submit;
        GameManager round;
        CodeLock codeLock;
        MonitorInput monitor;
        CameraController look;
        Camera view;
        CanvasGroup controls;
        Collider housing;
        GameObject crosshair;
        Vector3 deskPosition;
        Quaternion deskRotation;
        public bool Focused { get; private set; }
        bool showCrosshair;

        void Start()
        {
            codeLock = GetComponent<CodeLock>();
            round = codeLock.Round;
            if (!round)
            {
                enabled = false;
                return;
            }
            controls = GetComponent<CanvasGroup>();
            controls.interactable = false;
            housing = GetComponentInParent<Collider>();
            view = Camera.main;
            look = view.GetComponent<CameraController>();
            GetComponent<Canvas>().worldCamera = view;
            monitor = FindFirstObjectByType<MonitorInput>();
            crosshair = GameObject.Find("Crosshair");
            for (int i = 0; i < digits.Length; i++)
            {
                int digit = i;
                digits[i].onClick.AddListener(() => codeLock.PressDigit(digit));
            }
            clear.onClick.AddListener(codeLock.Clear);
            submit.onClick.AddListener(codeLock.Submit);
            label.text = codeLock.Label;
            RefreshDisplay();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (Time.timeScale == 0f || round.State != RoundState.Playing) return;
            if (!Focused)
            {
                if (monitor && monitor.Engaged) return;
                bool interact = Mouse.current?.leftButton.wasPressedThisFrame == true ||
                                keyboard?.eKey.wasPressedThisFrame == true;
                if (interact && Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f, .5f)),
                    out var hit, look.interactDistance) && hit.collider == housing) Focus();
                return;
            }
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Leave();
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) codeLock.Submit();
            else if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame) codeLock.Clear();
            else
            {
                for (int digit = 0; digit <= 9; digit++)
                {
                    Key numberRow = digit == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + digit - 1);
                    if (keyboard[numberRow].wasPressedThisFrame ||
                        keyboard[(Key)((int)Key.Numpad0 + digit)].wasPressedThisFrame)
                        codeLock.PressDigit(digit);
                }
            }
        }

        void Focus()
        {
            if (monitor) monitor.enabled = false;
            deskPosition = view.transform.position;
            deskRotation = view.transform.rotation;
            look.enabled = false;
            showCrosshair = crosshair && crosshair.activeSelf;
            if (crosshair) crosshair.SetActive(false);
            var panel = (RectTransform)transform;
            float width = panel.TransformVector(Vector3.right * panel.rect.width).magnitude;
            float height = panel.TransformVector(Vector3.up * panel.rect.height).magnitude;
            float distance = Mathf.Max(height, width / view.aspect) * .58f / Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            view.transform.SetPositionAndRotation(panel.position - panel.forward * distance, panel.rotation);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Focused = controls.interactable = true;
        }

        void Leave()
        {
            view.transform.SetPositionAndRotation(deskPosition, deskRotation);
            look.enabled = true;
            if (crosshair) crosshair.SetActive(showCrosshair);
            if (monitor) monitor.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Focused = controls.interactable = false;
        }

        void LateUpdate() => RefreshDisplay();

        void RefreshDisplay()
        {
            int seconds = Mathf.CeilToInt(round.SecondsLeft);
            countdown.text = $"{seconds / 60}:{seconds % 60:00}";
            strikes.text = $"STRIKES   {round.Strikes} / 3";
            entry.text = codeLock.Entry.PadRight(3, '-');
            feedback.text = codeLock.Feedback;
            hint.text = Focused ? "ESC   /   BACK TO DESK" : "CLICK OR E   /   INSPECT";
        }
    }
}