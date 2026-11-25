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
            public Vector2 desktopPosition;
        }

        sealed class AdWindow
        {
            public readonly DesktopWindow Window;
            readonly RectTransform frame;
            readonly BlinkingDesktopTarget blinkingClose;
            readonly Vector2 desktopPosition;

            public AdWindow(RelayDesktop desktop, Design design, System.Action dismissed)
            {
                desktopPosition = design.desktopPosition;
                Window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
                Window.name = "Pop-up Ad - " + design.caption;
                frame = (RectTransform)Window.transform;
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
                frame.sizeDelta = new Vector2(244, 130);
                ((RectTransform)frame.Find("Drop shadow")).sizeDelta = frame.sizeDelta;
                ((RectTransform)frame.Find("Outer frame")).sizeDelta = frame.sizeDelta;
                ((RectTransform)frame.Find("Bezel")).sizeDelta = frame.sizeDelta - new Vector2(4, 4);
                ((RectTransform)frame.Find("Light edge")).sizeDelta = new Vector2(240, 2);
                Window.titleBar.rectTransform.sizeDelta = new Vector2(234, 24);
                Window.titleBar.transform.Find("Guard").gameObject.SetActive(false);
                var caption = Window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>();
                caption.text = design.caption;
                caption.fontSize = 15;
                caption.rectTransform.anchoredPosition = new Vector2(6, 0);
                caption.rectTransform.sizeDelta = new Vector2(222, 24);
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
                canvas.anchorMin = canvas.anchorMax = canvas.pivot = Vector2.up;
                canvas.anchoredPosition = new Vector2(6, -32);
                canvas.sizeDelta = new Vector2(232, 92);

                var close = Window.titleBar.GetComponentInChildren<Button>(true);
                var closeRect = (RectTransform)close.transform;
                closeRect.SetParent(frame, false);
                blinkingClose = new BlinkingDesktopTarget(closeRect, 1f, 1f);
                close.onClick = new Button.ButtonClickedEvent();
                close.onClick.AddListener(() =>
                {
                    if (!Window.IsOpen || !close.gameObject.activeInHierarchy) return;
                    Window.Close();
                    dismissed();
                });
            }

            public void Show()
            {
                Vector2 travel = ((RectTransform)frame.parent).rect.size - frame.sizeDelta - new Vector2(16, 16);
                frame.anchoredPosition = Vector2.Scale(desktopPosition - new Vector2(.5f, .5f), travel);
                Window.Open();
                blinkingClose.Show();
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

        void CloseOne()
        {
            openCount--;
            if (openCount == 0) dismissed();
        }

        public void Show()
        {
            openCount = ads.Length;
            foreach (var ad in ads) ad.Show();
        }

        public void Tick(float deltaTime)
        {
            foreach (var ad in ads) ad.Tick(deltaTime);
        }

        public void KeepInFront()
        {
            int front = Mathf.Max(desktop.manual.IsOpen ? desktop.manual.transform.GetSiblingIndex() : -1,
                desktop.guard.IsOpen ? desktop.guard.transform.GetSiblingIndex() : -1);
            foreach (var ad in ads)
                if (ad.Window.IsOpen && ad.Window.transform.GetSiblingIndex() < front) ad.Window.Focus();
        }
    }
}
