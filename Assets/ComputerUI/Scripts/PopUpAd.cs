using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    // The desktop keeps one ad window and reuses it for each interruption.
    internal sealed class PopUpAd
    {
        readonly DesktopWindow window;
        readonly RectTransform frame, closeButton;
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
                "Double your desk storage today. This exclusive RELAY offer expires at the end of your shift.";
            frame.Find("Recalibrate").gameObject.SetActive(false);

            // Keep the Guard title bar's small, beveled X and its virtual-pointer hit target.
            var close = window.titleBar.GetComponentInChildren<Button>(true);
            closeButton = (RectTransform)close.transform;
            closeButton.SetParent(frame, false);
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(() =>
            {
                if (!window.IsOpen) return;
                window.Close();
                dismissed();
            });
        }

        public void Show()
        {
            const float inset = 8;
            closeButton.anchoredPosition = new Vector2(
                Random.Range(inset, frame.rect.width - closeButton.rect.width - inset),
                -Random.Range(inset, frame.rect.height - closeButton.rect.height - inset));
            window.Open();
        }

        public void KeepInFront()
        {
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
