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
        enum Face { Front, Left, Right, Top }
        [Tooltip("Front, left, right, top. Leave empty for the single-panel prototype.")]
        [SerializeField] RectTransform[] faces = System.Array.Empty<RectTransform>();
        Face currentFace;
        Face sideBeforeTop;
        Coroutine cameraMove;
        bool returningToDesk;
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
            controls.blocksRaycasts = controls.interactable = false;
            housing = GetComponentInParent<Collider>();
            view = Camera.main;
            look = view.GetComponent<CameraController>();
            foreach (var canvas in GetComponentsInChildren<Canvas>()) canvas.worldCamera = view;
            if (faces.Length > 0)
            {
                // Each face owns its input. The root must allow its children to receive clicks.
                controls.blocksRaycasts = controls.interactable = true;
                foreach (var face in faces)
                {
                    var group = face.GetComponent<CanvasGroup>();
                    group.blocksRaycasts = group.interactable = false;
                }
                controls = faces[0].GetComponent<CanvasGroup>();
            }
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
                if (monitor && (monitor.Engaged || monitor.ChangingView)) return;
                bool interact = Mouse.current?.leftButton.wasPressedThisFrame == true ||
                                keyboard?.eKey.wasPressedThisFrame == true;
                if (interact && Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f, .5f)),
                    out var hit, look.interactDistance) && hit.collider == housing) Focus();
                return;
            }
            if (returningToDesk || keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Leave();
            else if (faces.Length > 0 && NavigateFaces(keyboard)) return;
            else if (!controls.interactable || currentFace != Face.Front) return;
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
            Focused = true;
            if (monitor) monitor.enabled = false;
            deskPosition = view.transform.position;
            deskRotation = view.transform.rotation;
            look.enabled = false;
            showCrosshair = crosshair && crosshair.activeSelf;
            if (crosshair) crosshair.SetActive(false);
            MoveToFace(Face.Front, false);
        }

        bool NavigateFaces(Keyboard keyboard)
        {
            if (currentFace == Face.Top)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame)
                    MoveToFace(sideBeforeTop, true);
                return true;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                sideBeforeTop = currentFace;
                MoveToFace(Face.Top, true);
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame && currentFace != Face.Left)
            {
                MoveToFace(currentFace == Face.Front ? Face.Left : Face.Front, true);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame && currentFace != Face.Right)
            {
                MoveToFace(currentFace == Face.Front ? Face.Right : Face.Front, true);
            }
            else return false;

            return true;
        }

        void MoveToFace(Face face, bool orbit)
        {
            bool redirecting = cameraMove != null;
            if (redirecting) StopCoroutine(cameraMove);
            controls.blocksRaycasts = controls.interactable = false;
            currentFace = face;
            var panel = faces.Length > 0 ? faces[(int)face] : (RectTransform)transform;
            controls = panel.GetComponent<CanvasGroup>();
            float width = panel.TransformVector(Vector3.right * panel.rect.width).magnitude;
            float height = panel.TransformVector(Vector3.up * panel.rect.height).magnitude;
            float distance = Mathf.Max(height, width / view.aspect) * .58f / Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            cameraMove = StartCoroutine(CameraController.MoveTo(view.transform, panel.position - panel.forward * distance,
                panel.rotation, EnablePanel, orbit ? transform : null,
                duration: orbit ? .28f : .5f, easeIn: !redirecting));
        }

        void EnablePanel()
        {
            cameraMove = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            controls.blocksRaycasts = controls.interactable = true;
        }

        void Leave()
        {
            if (cameraMove != null) StopCoroutine(cameraMove);
            returningToDesk = true;
            controls.blocksRaycasts = controls.interactable = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cameraMove = StartCoroutine(CameraController.MoveTo(view.transform, deskPosition, deskRotation, RestoreDesk,
                faces.Length > 0 ? transform : null));
        }

        void RestoreDesk()
        {
            cameraMove = null;
            returningToDesk = false;
            if (look) look.enabled = true;
            if (crosshair) crosshair.SetActive(showCrosshair);
            if (monitor) monitor.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Focused = false;
        }

        void OnDisable()
        {
            if (!Focused) return;
            StopAllCoroutines();
            controls.blocksRaycasts = controls.interactable = false;
            if (view) view.transform.SetPositionAndRotation(deskPosition, deskRotation);
            RestoreDesk();
        }

        void LateUpdate() => RefreshDisplay();

        void RefreshDisplay()
        {
            int seconds = Mathf.CeilToInt(round.SecondsLeft);
            countdown.text = $"{seconds / 60}:{seconds % 60:00}";
            strikes.text = $"STRIKES   {round.Strikes} / 3";
            entry.text = codeLock.Entry.PadRight(3, '-');
            feedback.text = codeLock.Feedback;
            if (hint)
            {
                hint.text = Focused ? "" : "CLICK OR E  /  INSPECT";
                if (Focused && faces.Length == 0) hint.text = "ESC  /  DESK";
            }
        }
    }
}