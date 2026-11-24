using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    // The desktop keeps one ad window and reuses it for each interruption.
    internal sealed class PopUpAd
    {
        readonly DesktopWindow window;
        readonly RectTransform frame;
        readonly BlinkingDesktopTarget blinkingClose;
        public bool IsOpen => window.IsOpen;

        public PopUpAd(RelayDesktop desktop, System.Action dismissed)
        {
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Pop-up Ad";
            frame = (RectTransform)window.transform;
            var manualFrame = (RectTransform)desktop.manual.transform;
            frame.anchoredPosition = manualFrame.anchoredPosition;

            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "Sponsored message";
            frame.Find(desktop.guardTitle.name).GetComponent<Text>().text = "You won a free upgrade!";
            frame.Find(desktop.guardMessage.name).GetComponent<Text>().text =
                "Double your desk storage today! Catch the blinking X to dismiss this offer.";
            frame.Find("Recalibrate").gameObject.SetActive(false);

            // Keep the Guard title bar's small, beveled X and its virtual-pointer hit target.
            var close = window.titleBar.GetComponentInChildren<Button>(true);
            var closeButton = (RectTransform)close.transform;
            closeButton.SetParent(frame, false);
            blinkingClose = new BlinkingDesktopTarget(closeButton, 1f, 1f);
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(() =>
            {
                if (!window.IsOpen || !close.gameObject.activeInHierarchy) return;
                window.Close();
                dismissed();
            });
        }

        public void Show()
        {
            window.Open();
            blinkingClose.Show();
        }

        public void Tick(float deltaTime) => blinkingClose.Tick(deltaTime);

        public void KeepInFront()
        {
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
