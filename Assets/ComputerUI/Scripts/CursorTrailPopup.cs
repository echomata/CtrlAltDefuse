using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class CursorTrailPopup
    {
        const int CopyCount = 5;
        const float Lifetime = 5f;
        const float ClearFadeSeconds = 5f;
        const float Spacing = 9f;
        const float Inset = 24f;
        static readonly Vector2[] CopyAnchors =
        {
            new(0f, 0f), new(1f, 0f), new(0f, 1f), new(1f, 1f), new(.5f, .5f)
        };
        readonly RectTransform cursor, layer;
        readonly DesktopWindow[] windows = new DesktopWindow[CopyCount];
        readonly RawImage[] particles = new RawImage[64];
        readonly float[] remaining = new float[64];
        readonly Vector2[] velocity = new Vector2[64];
        static readonly Color32[] Colors = { new(245, 101, 156, 255), new(54, 233, 212, 255), new(255, 206, 75, 255) };
        Vector2 lastPosition;
        int next;
        public bool IsOpen
        {
            get
            {
                for (int i = 0; i < windows.Length; i++)
                    if (windows[i].IsOpen) return true;
                return false;
            }
        }
        public bool IsFading { get; private set; }

        public CursorTrailPopup(RelayDesktop desktop)
        {
            cursor = desktop.input.cursor;
            for (int i = 0; i < windows.Length; i++)
            {
                var window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
                windows[i] = window;
                window.name = i == windows.Length - 1 ? "Cursor Particle Trail" : $"Cursor Particle Trail {i + 1}";
                var frame = (RectTransform)window.transform;
                Vector2 anchor = CopyAnchors[i];
                frame.anchorMin = frame.anchorMax = frame.pivot = anchor;
                frame.anchoredPosition = new Vector2(
                    Mathf.Lerp(Inset, -Inset, anchor.x),
                    Mathf.Lerp(Inset, -Inset, anchor.y));
                frame.Find(desktop.guardTitle.name).GetComponent<Text>().text = "Pointer trail detected";
                frame.Find(desktop.guardMessage.name).GetComponent<Text>().text =
                    "Your pointer is leaving copies across the manual. Recalibrate every copy to clear the screen.";
                window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
                var button = frame.Find("Recalibrate").GetComponent<Button>();
                var label = button.GetComponentInChildren<Text>(true);
                label.text = "Recalibrate Cursor";
                label.resizeTextForBestFit = true;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    if (!window.IsOpen) return;
                    window.Close();
                    if (!IsOpen) desktop.ClearCursorTrail();
                });
            }

            var surface = new GameObject("Pointer trail particles", typeof(RectTransform), typeof(RectMask2D));
            surface.layer = cursor.gameObject.layer;
            layer = (RectTransform)surface.transform;
            layer.SetParent(windows[0].transform.parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            var pointerImage = cursor.GetComponent<RawImage>();
            for (int i = 0; i < particles.Length; i++)
            {
                var image = Object.Instantiate(pointerImage, layer);
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = layer.pivot;
                image.raycastTarget = false;
                image.gameObject.SetActive(false);
                particles[i] = image;
            }
        }

        public void Show()
        {
            IsFading = false;
            layer.gameObject.SetActive(true);
            for (int i = 0; i < windows.Length; i++) windows[i].Open();
            layer.SetAsLastSibling();
            next = 0;
            lastPosition = layer.InverseTransformPoint(cursor.position);
            Emit(lastPosition);
        }

        // Sample after MonitorInput moves the real virtual cursor for this frame.
        public void UpdateTrail(float dt, bool tracking)
        {
            bool open = IsOpen;
            if (open && !WindowsInFront())
                for (int i = 0; i < windows.Length; i++)
                    if (windows[i].IsOpen) windows[i].Focus();
            if ((open || IsFading) && layer.GetSiblingIndex() != layer.parent.childCount - 1)
                layer.SetAsLastSibling();

            for (int i = 0; i < particles.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] -= dt;
                var image = particles[i];
                if (remaining[i] <= 0) image.gameObject.SetActive(false);
                else
                {
                    image.rectTransform.anchoredPosition += velocity[i] * dt;
                    image.canvasRenderer.SetAlpha(Mathf.Clamp01(remaining[i] / .5f));
                }
            }
            if (IsFading)
            {
                if (AnyParticlesAlive()) return;
                IsFading = false;
                layer.gameObject.SetActive(false);
                return;
            }
            Vector2 position = layer.InverseTransformPoint(cursor.position);
            if (tracking)
            {
                float distance = Vector2.Distance(lastPosition, position);
                if (distance < Spacing) return;
                int count = Mathf.Min(particles.Length, Mathf.FloorToInt(distance / Spacing));
                for (int i = 1; i <= count; i++)
                    Emit(Vector2.Lerp(lastPosition, position, (float)i / count));
            }
            lastPosition = position;
        }

        bool WindowsInFront()
        {
            var parent = windows[0].transform.parent;
            int index = parent.childCount - 2;
            for (int i = windows.Length - 1; i >= 0; i--)
            {
                if (!windows[i].IsOpen) continue;
                if (windows[i].transform.GetSiblingIndex() != index) return false;
                index--;
            }
            return true;
        }

        bool AnyParticlesAlive()
        {
            for (int i = 0; i < particles.Length; i++)
                if (remaining[i] > 0) return true;
            return false;
        }

        void Emit(Vector2 position)
        {
            var image = particles[next];
            image.color = Colors[next % Colors.Length];
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.localScale = Vector3.one * Random.Range(.9f, 1.6f);
            image.gameObject.SetActive(true);
            image.canvasRenderer.SetAlpha(1f);
            remaining[next] = Lifetime;
            velocity[next] = new Vector2(Random.Range(-18f, 18f), Random.Range(-22f, 12f));
            next = (next + 1) % particles.Length;
        }

        public void Clear()
        {
            IsFading = true;
            for (int i = 0; i < particles.Length; i++)
                if (remaining[i] > 0) remaining[i] = ClearFadeSeconds;
        }
    }
}
