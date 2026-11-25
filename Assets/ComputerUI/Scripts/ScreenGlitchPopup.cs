using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class ScreenGlitchPopup
    {
        readonly RelayDesktop desktop;
        readonly DesktopWindow window;
        readonly RawImage interference;
        readonly AudioSource crackle;
        readonly HoldButton popupPowerHold, taskbarPowerHold, wakeHold;
        readonly Text wakeLabel;
        readonly string normalWakeLabel;
        float burstRemaining, nextBurst;
        public bool IsOpen => window.IsOpen;

        public ScreenGlitchPopup(RelayDesktop desktop, System.Action<bool> setStandby)
        {
            this.desktop = desktop;
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Screen Glitch";
            var frame = (RectTransform)window.transform;
            frame.anchoredPosition = ((RectTransform)desktop.manual.transform).anchoredPosition;
            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "RELAY Display Control";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            frame.Find(desktop.guardTitle.name).GetComponent<Text>().text = "Display signal corrupted";
            frame.Find(desktop.guardMessage.name).GetComponent<Text>().text =
                "Hold Power Off for 3 seconds, then hold Wake for 3 seconds to restore the display.";
            var power = frame.Find("Recalibrate").GetComponent<Button>();
            power.GetComponentInChildren<Text>(true).text = "Power Off";
            power.onClick = new Button.ButtonClickedEvent();
            popupPowerHold = HoldButton.Attach(power, () => setStandby(true));
            taskbarPowerHold = HoldButton.Attach(desktop.power, () => setStandby(true));
            wakeHold = HoldButton.Attach(desktop.wake, () => setStandby(false));
            wakeLabel = desktop.wake.GetComponentInChildren<Text>(true);
            normalWakeLabel = wakeLabel.text;

            // Composite static on the desktop canvas. Standby and the virtual pointer stay above it.
            var layer = new GameObject("Display interference", typeof(RectTransform), typeof(RawImage), typeof(AudioSource));
            layer.layer = desktop.standby.layer;
            interference = layer.GetComponent<RawImage>();
            var rect = interference.rectTransform;
            rect.SetParent(desktop.standby.transform.parent, false);
            rect.SetSiblingIndex(desktop.standby.transform.GetSiblingIndex());
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            interference.raycastTarget = false;
            interference.texture = desktop.signalNoise;
            crackle = layer.GetComponent<AudioSource>();
            crackle.playOnAwake = false;
            crackle.clip = desktop.staticCrackle;
            crackle.outputAudioMixerGroup = desktop.clicks.outputAudioMixerGroup;
        }

        public void Show()
        {
            window.Open();
            popupPowerHold.enabled = taskbarPowerHold.enabled = true;
            interference.gameObject.SetActive(true);
            nextBurst = 0;
            Tick(0);
        }

        public void Tick(float dt)
        {
            nextBurst -= dt;
            burstRemaining -= dt;
            if (nextBurst <= 0)
            {
                burstRemaining = .24f;
                nextBurst = Random.Range(1.1f, 2.1f);
                interference.uvRect = new Rect(Random.value, Random.value, 3, 3);
                if (crackle.clip && !desktop.clicks.mute) crackle.Play();
            }
            float strength = Mathf.Clamp01(burstRemaining / .24f);
            interference.color = new Color(.85f, .95f, 1f, .08f + .46f * strength);
        }

        public void PowerOff()
        {
            interference.gameObject.SetActive(false);
            popupPowerHold.enabled = taskbarPowerHold.enabled = false;
            wakeLabel.text = "Hold to wake (3s)";
            wakeHold.enabled = true;
        }

        public void Recover()
        {
            wakeHold.enabled = false;
            wakeLabel.text = normalWakeLabel;
            window.Close();
        }

        public void KeepInFront()
        {
            var frame = window.transform;
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
