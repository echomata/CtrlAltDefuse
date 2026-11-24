using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class CaptchaWallPopup
    {
        const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        const int Length = 10;
        readonly RelayDesktop desktop;
        readonly System.Action cleared;
        readonly DesktopWindow window;
        readonly Text challenge, prompt, entry;
        Keyboard keyboard;
        string code, answer;
        public bool IsOpen => window.IsOpen;
        bool CanType => IsOpen && desktop.isActiveAndEnabled && desktop.input.Engaged && !desktop.Sleeping;

        public CaptchaWallPopup(RelayDesktop desktop, System.Action cleared)
        {
            this.desktop = desktop;
            this.cleared = cleared;
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Captcha Wall";
            var frame = (RectTransform)window.transform;
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "RELAY Access Check";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

            // Cover the manual until verification succeeds. The taskbar stays available.
            var cover = Panel("Locked desktop", frame, new Color32(54, 72, 86, 255));
            cover.sizeDelta = ((RectTransform)frame.parent).rect.size;
            cover.SetAsFirstSibling();
            challenge = frame.Find(desktop.guardTitle.name).GetComponent<Text>();
            prompt = frame.Find(desktop.guardMessage.name).GetComponent<Text>();
            var field = Panel("Answer field", frame, new Color32(246, 247, 240, 255));
            field.anchorMin = field.anchorMax = field.pivot = Vector2.up;
            field.anchoredPosition = new Vector2(84, -121);
            field.sizeDelta = new Vector2(214, 36);
            entry = Object.Instantiate(prompt, field);
            entry.name = "Typed answer";
            entry.rectTransform.anchoredPosition = new Vector2(8, 0);
            entry.rectTransform.sizeDelta = new Vector2(198, 36);
            entry.alignment = TextAnchor.MiddleLeft;
            entry.raycastTarget = false;

            var submit = frame.Find("Recalibrate").GetComponent<Button>();
            submit.GetComponentInChildren<Text>(true).text = "Submit";
            submit.onClick = new Button.ButtonClickedEvent();
            submit.onClick.AddListener(Submit);
        }

        static RectTransform Panel(string name, RectTransform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            obj.GetComponent<Image>().color = color;
            return rect;
        }

        public void Show()
        {
            var characters = new char[Length];
            for (int i = 0; i < Length; i++) characters[i] = Alphabet[Random.Range(0, Alphabet.Length)];
            code = new string(characters);
            challenge.text = code;
            prompt.text = $"Type the {Length} characters above, then submit.";
            SetAnswer("");
            window.Open();
            BindKeyboard(Keyboard.current);
        }

        void SetAnswer(string value)
        {
            answer = value;
            entry.text = answer.PadRight(Length, '_');
        }

        void TypeCharacter(char character)
        {
            if (!CanType || answer.Length >= Length) return;
            char upper = char.ToUpperInvariant(character);
            if (upper >= 'A' && upper <= 'Z' || upper >= '0' && upper <= '9')
                SetAnswer(answer + upper);
        }

        public void ReadKeyboard()
        {
            BindKeyboard(Keyboard.current);
            if (keyboard == null) return;
            if (keyboard.backspaceKey.wasPressedThisFrame && answer.Length > 0)
                SetAnswer(answer.Substring(0, answer.Length - 1));
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                Submit();
        }

        void Submit()
        {
            if (!CanType) return;
            if (answer != code)
            {
                prompt.text = $"Code mismatch. Type all {Length} characters again.";
                SetAnswer("");
                return;
            }
            DetachKeyboard();
            window.Close();
            cleared();
        }

        void BindKeyboard(Keyboard device)
        {
            if (keyboard == device) return;
            if (keyboard != null) keyboard.onTextInput -= TypeCharacter;
            keyboard = device;
            if (keyboard != null) keyboard.onTextInput += TypeCharacter;
        }

        public void DetachKeyboard() => BindKeyboard(null);

        public void KeepInFront()
        {
            var frame = window.transform;
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
