using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class LongEulaPopup
    {
        const string Terms =
            "RELAY DESKTOP END USER LICENSE AGREEMENT\nOperator copy  /  Revision 4.2\n\n" +
            "1. Access to the handbook\nThis terminal grants the operator on duty a license to consult one electronic copy of the field manual.\n\n" +
            "2. Assigned equipment\nThe license covers the desk, its monitor and the pointer currently attached to it. Each replacement pointer requires a separate inventory entry.\n\n" +
            "3. Printed copies\nFile each printed page in the station binder. Return the binder to the archive before requesting a replacement binder.\n\n" +
            "4. Service requests\nInclude the station number, shift number and service ticket number on every request for a service ticket number.\n\n" +
            "5. Software updates\nThe desktop may request acknowledgment of revised terms during an active shift. The station clock continues during review.\n\n" +
            "6. Shift handover\nThe departing operator must leave the handbook open to the handover sheet. If the handover sheet is already open, record who opened it. Both operators must initial the entry using the same pencil.\n\n" +
            "7. Station clock\nRequests to correct the time must include the time shown on the station clock when the error was first observed. The clock office accepts submissions between nine and five according to that same clock.\n\n" +
            "8. Notification sounds\nEach chime is assigned to a desk. If your desk produces the chime belonging to another desk, ask the other operator to describe your chime before filing for an exchange.\n\n" +
            "9. Archive labels\nMark each retired binder with its shelf and volume number. If a shelf has been retired, use the number of the shelf holding the record of its retirement. Leave room on the label for a second shelf number.\n\n" +
            "10. Screen cleaning\nSign out one cloth from the supply cabinet. After cleaning the monitor, inspect the display for a satisfactory reflection of the cloth. Return it folded with the inventory stamp facing up.\n\n" +
            "11. Lost property\nA pencil found beneath a terminal belongs to the last operator who signed for its eraser. Place disputed pencils in the tray by the printer until the stationery clerk finishes the monthly count.\n\n" +
            "12. Records retention\nKeep a copy of each completed retention form for as long as the document named on that form is kept. When the document is destroyed, attach the receipt to the copy marked for destruction.\n\n" +
            "13. End of shift\nLeave the chair facing the desk and the pointer in its cradle. Check that the next operator can reach the handbook from the chair without moving either piece of furniture.\n\n" +
            "14. Acknowledgment\nYou have reached the end of this agreement. Select Accept to return to the field manual.";

        readonly DesktopWindow window;
        readonly ScrollRect scroll;
        readonly Button accept;
        readonly Text hint;
        public bool IsOpen => window.IsOpen;

        public LongEulaPopup(RelayDesktop desktop, System.Action accepted)
        {
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Long EULA";
            var frame = (RectTransform)window.transform;
            frame.sizeDelta = new Vector2(368, 424);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            // The Guard prefab uses fixed-size artwork for its beveled window frame.
            ((RectTransform)frame.Find("Drop shadow")).sizeDelta = frame.sizeDelta;
            ((RectTransform)frame.Find("Outer frame")).sizeDelta = frame.sizeDelta;
            ((RectTransform)frame.Find("Bezel")).sizeDelta = frame.sizeDelta - new Vector2(4, 4);
            ((RectTransform)frame.Find("Light edge")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frame.rect.width - 4);
            window.titleBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frame.rect.width - 10);
            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "RELAY License Agreement";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            frame.Find("Guard").gameObject.SetActive(false);
            frame.Find(desktop.guardTitle.name).gameObject.SetActive(false);

            hint = frame.Find(desktop.guardMessage.name).GetComponent<Text>();
            hint.rectTransform.anchoredPosition = new Vector2(16, 38 - frame.rect.height);
            hint.rectTransform.sizeDelta = new Vector2(frame.rect.width - 160, 27);
            hint.alignment = TextAnchor.MiddleLeft;
            accept = frame.Find("Recalibrate").GetComponent<Button>();
            ((RectTransform)accept.transform).anchoredPosition = new Vector2(frame.rect.width - 129, 38 - frame.rect.height);
            accept.GetComponentInChildren<Text>(true).text = "Accept";
            accept.onClick = new Button.ButtonClickedEvent();
            accept.onClick.AddListener(() =>
            {
                if (!window.IsOpen || !accept.interactable) return;
                window.Close();
                accepted();
            });

            var panel = new GameObject("Agreement", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            panel.layer = frame.gameObject.layer;
            var viewport = (RectTransform)panel.transform;
            viewport.SetParent(frame, false);
            viewport.anchorMin = viewport.anchorMax = viewport.pivot = Vector2.up;
            viewport.anchoredPosition = new Vector2(15, -43);
            viewport.sizeDelta = frame.sizeDelta - new Vector2(30, 88);
            panel.GetComponent<Image>().color = new Color32(246, 247, 240, 255);

            var document = Object.Instantiate(hint, viewport);
            document.name = "License terms";
            document.text = Terms;
            document.alignment = TextAnchor.UpperLeft;
            document.raycastTarget = false;
            var content = document.rectTransform;
            content.sizeDelta = new Vector2(viewport.rect.width - 16, 0);
            content.anchoredPosition = new Vector2(8, 0);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(document.preferredHeight) + 6);
            scroll = panel.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18;
            scroll.onValueChanged.AddListener(OnScroll);
        }

        public void Show()
        {
            window.Open();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            hint.text = "Scroll to the end to accept.";
            accept.interactable = false;
        }

        void OnScroll(Vector2 position)
        {
            if (!IsOpen || accept.interactable || position.y > .001f) return;
            accept.interactable = true;
            hint.text = "End of agreement.";
        }

        public void KeepInFront()
        {
            if (window.transform.GetSiblingIndex() != window.transform.parent.childCount - 1) window.Focus();
        }
    }
}
