using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class PopUpAd
    {
        [System.Serializable]
        internal struct Design
        {
            public string caption;
            public Texture2D poster;
        }

        sealed class AdWindow
        {
            public readonly DesktopWindow Window;
            readonly RectTransform frame;
            readonly CanvasGroup group;
            readonly Button close;
            readonly BlinkingDesktopTarget blinkingClose;

            public AdWindow(RelayDesktop desktop, Design design, System.Action<AdWindow> dismiss)
            {
                Window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
                Window.name = "Pop-up Ad - " + design.caption;
                frame = (RectTransform)Window.transform;
                group = Window.GetComponent<CanvasGroup>();
                var manual = (RectTransform)desktop.manual.transform;
                frame.anchorMin = manual.anchorMin;
                frame.anchorMax = manual.anchorMax;
                frame.pivot = manual.pivot;
                frame.sizeDelta = manual.sizeDelta;
                frame.anchoredPosition = manual.anchoredPosition;
                var size = frame.rect.size;
                ((RectTransform)frame.Find("Drop shadow")).sizeDelta = size;
                var backing = frame.Find("Outer frame").GetComponent<Image>();
                backing.rectTransform.sizeDelta = size;
                backing.raycastTarget = true;
                ((RectTransform)frame.Find("Bezel")).sizeDelta = size - new Vector2(4, 4);
                ((RectTransform)frame.Find("Light edge")).sizeDelta = new Vector2(size.x - 4, 2);
                Window.titleBar.rectTransform.sizeDelta = new Vector2(size.x - 10, 24);
                Window.titleBar.transform.Find("Guard").gameObject.SetActive(false);
                var caption = Window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>();
                caption.text = design.caption;
                caption.fontSize = 15;
                caption.rectTransform.anchoredPosition = new Vector2(6, 0);
                caption.rectTransform.sizeDelta = new Vector2(size.x - 22, 24);
                frame.Find("Guard").gameObject.SetActive(false);
                frame.Find(desktop.guardTitle.name).gameObject.SetActive(false);
                frame.Find(desktop.guardMessage.name).gameObject.SetActive(false);
                frame.Find("Recalibrate").gameObject.SetActive(false);

                var art = new GameObject("Advertisement", typeof(RectTransform), typeof(RawImage));
                art.layer = frame.gameObject.layer;
                var image = art.GetComponent<RawImage>();
                image.texture = design.poster;
                image.raycastTarget = false;
                var canvas = image.rectTransform;
                canvas.SetParent(frame, false);
                canvas.anchorMin = canvas.anchorMax = canvas.pivot = new Vector2(.5f, .5f);
                canvas.anchoredPosition = new Vector2(0, -13);
                Vector2 posterSize = new Vector2(design.poster.width, design.poster.height);
                canvas.sizeDelta = posterSize * Mathf.Min((size.x - 12) / posterSize.x, (size.y - 38) / posterSize.y);

                close = Window.titleBar.GetComponentInChildren<Button>(true);
                var closeRect = (RectTransform)close.transform;
                closeRect.SetParent(frame, false);
                blinkingClose = new BlinkingDesktopTarget(closeRect, 1f, 1f);
                close.onClick = new Button.ButtonClickedEvent();
                close.onClick.AddListener(() =>
                {
                    if (!Window.IsOpen || !close.gameObject.activeInHierarchy) return;
                    dismiss(this);
                });
            }

            public void Place(Vector2 position, bool onTop)
            {
                frame.anchoredPosition = position;
                group.interactable = onTop;
                if (onTop) blinkingClose.Show();
                else close.gameObject.SetActive(false);
            }

            public void Tick(float dt)
            {
                if (Window.IsOpen) blinkingClose.Tick(dt);
            }
        }

        readonly RelayDesktop desktop;
        readonly AdWindow[] ads;
        readonly System.Action dismissed;
        int openCount;
        public bool IsOpen => openCount > 0;

        public PopUpAd(RelayDesktop desktop, Design[] designs, System.Action dismissed)
        {
            this.desktop = desktop;
            this.dismissed = dismissed;
            ads = new AdWindow[designs.Length];
            for (int i = 0; i < designs.Length; i++)
                ads[i] = new AdWindow(desktop, designs[i], CloseOne);
        }

        void CloseOne(AdWindow ad)
        {
            if (!IsOpen || ads[openCount - 1] != ad) return;
            ad.Window.Close();
            ad.Window.gameObject.SetActive(false);
            openCount--;
            if (openCount == 0) dismissed();
            else ArrangeStack();
        }

        public void Show()
        {
            openCount = ads.Length;
            foreach (var ad in ads) ad.Window.OpenImmediately();
            ArrangeStack();
        }

        void ArrangeStack()
        {
            var manual = (RectTransform)desktop.manual.transform;
            var workspace = ((RectTransform)manual.parent).rect;
            // Fit the cascade in the space above and to the left of the manual.
            int gaps = Mathf.Max(1, ads.Length - 1);
            float left = manual.localPosition.x + manual.rect.xMin - workspace.xMin;
            float above = workspace.yMax - manual.localPosition.y - manual.rect.yMax;
            var step = new Vector2(-Mathf.Clamp(left / gaps, 0, 11), Mathf.Clamp(above / gaps, 0, 6));
            for (int i = 0; i < openCount; i++)
            {
                int depth = openCount - 1 - i;
                ads[i].Place(manual.anchoredPosition + depth * step, depth == 0);
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsOpen) ads[openCount - 1].Tick(deltaTime);
        }

        public void KeepInFront()
        {
            // If a desktop shortcut raises another window, raise the entire remaining stack in order.
            int front = Mathf.Max(desktop.manual.IsOpen ? desktop.manual.transform.GetSiblingIndex() : -1,
                desktop.guard.IsOpen ? desktop.guard.transform.GetSiblingIndex() : -1);
            if (ads[0].Window.transform.GetSiblingIndex() > front) return;
            for (int i = 0; i < openCount; i++) ads[i].Window.Focus();
        }
    }
}
