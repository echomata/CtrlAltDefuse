using CtrlAltDefuse.Desktop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CtrlAltDefuse.Gameplay
{
    [DefaultExecutionOrder(-200)]
    public sealed class ScreenController : MonoBehaviour
    {
        const string MenuScene = "MainMenu";
        const string GameScene = "MainScene";

        [System.Serializable]
        sealed class ButtonRegions
        {
            [Tooltip("Normalized rectangle measured from the top left of the artwork.")]
            public Rect primary = new Rect(.375f, .56f, .25f, .075f);
            [Tooltip("Normalized rectangle measured from the top left of the artwork.")]
            public Rect secondary = new Rect(.375f, .655f, .25f, .075f);
        }

        [SerializeField] GameManager round;
        [SerializeField] ButtonRegions mainMenu = new();
        [SerializeField] ButtonRegions pause = new();
        [SerializeField] ButtonRegions victory = new();
        [SerializeField] ButtonRegions defeat = new();

        GameObject overlay;
        RawImage artwork;
        AspectRatioFitter frame;
        Text heading, detail, primaryLabel, secondaryLabel;
        Button primary, secondary;
        BombPanel bombPanel;
        MonitorInput monitor;
        CursorLockMode previousLock;
        bool previousCursor, previousAudioPause;
        bool frozen, resumeRequested, loading;
        int shownFrame;

#if UNITY_EDITOR
        static bool showHitboxes;
        GameObject primaryHitbox, secondaryHitbox;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHitboxes() => showHitboxes = false;

        void ToggleHitboxes()
        {
            showHitboxes = !showHitboxes;
            primaryHitbox.SetActive(showHitboxes);
            secondaryHitbox.SetActive(showHitboxes);
        }

        static GameObject CreateHitbox(Button button, Color tint)
        {
            var highlight = CreateRect("Test hitbox", button.transform).gameObject.AddComponent<Image>();
            highlight.color = tint;
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(showHitboxes);
            return highlight.gameObject;
        }
#endif

        void Start()
        {
            BuildOverlay();
            if (round)
            {
                bombPanel = FindAnyObjectByType<BombPanel>(FindObjectsInactive.Include);
                monitor = FindAnyObjectByType<MonitorInput>();
                return;
            }

            Show("MainMenu", "Ctrl+Alt+Defuse", "", mainMenu,
                "Play", () => LoadScene(GameScene), "Quit", Application.Quit);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (round && keyboard.f5Key.wasPressedThisFrame)
            {
                LoadScene(GameScene);
                return;
            }
#if UNITY_EDITOR
            if (keyboard.f7Key.wasPressedThisFrame)
                ToggleHitboxes();
#endif
            if (!round || frozen || round.State != RoundState.Playing) return;
#if UNITY_EDITOR
            if (keyboard.f8Key.wasPressedThisFrame) { VictoryTest.Trigger(round); return; }
            if (keyboard.f9Key.wasPressedThisFrame) { DefeatTest.Trigger(round); return; }
#endif

            bool atDesk = !(bombPanel && bombPanel.Focused) && !(monitor && (monitor.Engaged || monitor.ChangingView));
            if (keyboard.pKey.wasPressedThisFrame || (atDesk && keyboard.escapeKey.wasPressedThisFrame))
                PauseGame();
        }

        void PauseGame()
        {
            FreezeGameplay();
            Show("Pause", "Paused", "", pause, "Resume", () => resumeRequested = true,
                "Main Menu", () => LoadScene(MenuScene));
        }

        void LateUpdate()
        {
            if (!round || loading) return;
            var keyboard = Keyboard.current;
            if (round.State == RoundState.Playing)
            {
                bool resumeKey = keyboard != null && Time.frameCount > shownFrame &&
                    (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
                if (frozen && (resumeRequested || resumeKey))
                {
                    resumeRequested = false;
                    overlay.SetActive(false);
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                    RestoreGameplay();
                }
                return;
            }

            if (!frozen)
            {
                FreezeGameplay();
                bool won = round.State == RoundState.Won;
                string reason = round.Strikes >= 3 ? "Three strikes" : "Time ran out";
                Show(won ? "Victory" : "Defeat", won ? "Bomb defused" : "Round lost",
                    won ? "" : reason, won ? victory : defeat,
                    "Play Again", () => LoadScene(GameScene), "Main Menu", () => LoadScene(MenuScene));
            }
            else if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                LoadScene(GameScene);
            }
        }

        void FreezeGameplay()
        {
            previousLock = Cursor.lockState;
            previousCursor = Cursor.visible;
            previousAudioPause = AudioListener.pause;
            frozen = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        void RestoreGameplay()
        {
            if (!frozen) return;
            frozen = false;
            Time.timeScale = 1f;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursor;
        }

        void LoadScene(string scene)
        {
            if (loading) return;
            loading = true;
            RestoreGameplay();
            SceneManager.LoadScene(scene);
        }

        void OnDestroy() => RestoreGameplay();

        void Show(string imageName, string title, string subtitle, ButtonRegions regions,
            string firstLabel, UnityAction firstAction, string secondLabel, UnityAction secondAction)
        {
            var texture = Resources.Load<Texture2D>("ScreenArt/" + imageName);
            bool hasArtwork = texture != null;
            artwork.texture = texture;
            artwork.enabled = hasArtwork;
            frame.aspectRatio = hasArtwork ? (float)texture.width / texture.height : 16f / 9f;
            heading.text = hasArtwork ? "" : title;
            detail.text = hasArtwork ? "" : subtitle;
            ConfigureButton(primary, primaryLabel, regions.primary, firstLabel, firstAction, hasArtwork);
            ConfigureButton(secondary, secondaryLabel, regions.secondary, secondLabel, secondAction, hasArtwork);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            shownFrame = Time.frameCount;
            overlay.SetActive(true);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }

        void BuildOverlay()
        {
            overlay = new GameObject("Screen Overlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var backdrop = CreateRect("Backdrop", overlay.transform).gameObject.AddComponent<Image>();
            backdrop.color = round ? new Color(.04f, .06f, .07f, .96f) : new Color(.04f, .06f, .07f);
            var content = CreateRect("Artwork and buttons", overlay.transform);
            frame = content.gameObject.AddComponent<AspectRatioFitter>();
            frame.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            artwork = CreateRect("Artwork", content).gameObject.AddComponent<RawImage>();
            artwork.raycastTarget = false;
            heading = CreateText("Title", content, 86, new Color32(251, 195, 117, 255));
            SetRegion(heading.rectTransform, new Rect(.05f, .24f, .9f, .17f));
            detail = CreateText("Detail", content, 36, Color.white);
            SetRegion(detail.rectTransform, new Rect(.1f, .415f, .8f, .07f));
            primary = CreateButton("Primary action", content, out primaryLabel);
            secondary = CreateButton("Secondary action", content, out secondaryLabel);
#if UNITY_EDITOR
            primaryHitbox = CreateHitbox(primary, new Color(0f, 1f, 1f, .45f));
            secondaryHitbox = CreateHitbox(secondary, new Color(1f, .25f, .8f, .45f));
#endif
            overlay.SetActive(false);
        }

        static Button CreateButton(string name, Transform parent, out Text label)
        {
            var root = CreateRect(name, parent);
            var image = root.gameObject.AddComponent<Image>();
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = CreateText("Label", root, 36, Color.white);
            return button;
        }

        static void ConfigureButton(Button button, Text label, Rect region, string caption,
            UnityAction action, bool hasArtwork)
        {
            SetRegion((RectTransform)button.transform, region);
            button.image.color = hasArtwork ? Color.clear : new Color32(56, 104, 93, 255);
            button.transition = hasArtwork ? Selectable.Transition.None : Selectable.Transition.ColorTint;
            label.text = hasArtwork ? "" : caption;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static Text CreateText(string name, Transform parent, int size, Color color)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            SetRegion(rect, new Rect(0, 0, 1, 1));
            return rect;
        }

        static void SetRegion(RectTransform rect, Rect region)
        {
            rect.anchorMin = new Vector2(region.xMin, 1f - region.yMax);
            rect.anchorMax = new Vector2(region.xMax, 1f - region.yMin);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}