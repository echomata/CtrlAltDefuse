using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    [RequireComponent(typeof(Button))]
    internal sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float HoldSeconds = 3f;
        RectTransform track, fill;
        Button button;
        Action completed;
        bool holding;
        double pressTime;

        public static HoldButton Attach(Button button, Action completed)
        {
            var hold = button.gameObject.AddComponent<HoldButton>();
            hold.button = button;
            hold.completed = completed;
            hold.track = AddLine(button.transform, "Hold track", new Color32(106, 125, 141, 255));
            hold.track.anchorMin = Vector2.zero;
            hold.track.anchorMax = Vector2.right;
            hold.track.offsetMin = new Vector2(4f, 3f);
            hold.track.offsetMax = new Vector2(-4f, 7f);
            hold.fill = AddLine(hold.track, "Fill", new Color32(0, 232, 184, 255));
            hold.fill.anchorMin = Vector2.zero;
            hold.fill.anchorMax = Vector2.up;
            hold.fill.offsetMin = hold.fill.offsetMax = Vector2.zero;
            hold.enabled = false;
            hold.track.gameObject.SetActive(false);
            return hold;
        }

        static RectTransform AddLine(Transform parent, string name, Color color)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.layer = parent.gameObject.layer;
            var image = line.GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image.rectTransform;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
            pressTime = Time.unscaledTimeAsDouble;
            holding = true;
        }

        // MonitorInput dispatches release and exit in Update, before the hold can complete.
        void LateUpdate()
        {
            if (!holding) return;
            float progress = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - pressTime) / HoldSeconds));
            fill.anchorMax = new Vector2(progress, 1f);
            if (progress < 1f) return;
            holding = false;
            completed();
        }

        public void OnPointerUp(PointerEventData data) => Cancel();
        public void OnPointerExit(PointerEventData data) => Cancel();

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Cancel();
        }

        void OnEnable()
        {
            if (track) track.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            Cancel();
            if (track) track.gameObject.SetActive(false);
        }

        void Cancel()
        {
            holding = false;
            if (fill) fill.anchorMax = Vector2.up;
        }
    }
}
