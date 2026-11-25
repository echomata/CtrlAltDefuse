using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class LoudAudioPopup
    {
        readonly DesktopWindow window;
        readonly Button mute;
        readonly AudioSource siren;
        readonly BlinkingDesktopTarget blinkingMute;
        float elapsed;
        public bool IsOpen => window.IsOpen;

        public LoudAudioPopup(RelayDesktop desktop, System.Action cleared)
        {
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Loud Audio";
            var frame = (RectTransform)window.transform;
            frame.anchoredPosition = ((RectTransform)desktop.manual.transform).anchoredPosition;
            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "RELAY Audio Control";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            frame.Find(desktop.guardTitle.name).GetComponent<Text>().text = "Alarm channel overridden";
            frame.Find(desktop.guardMessage.name).GetComponent<Text>().text =
                "Click the flashing Mute button to silence the alarm.";

            mute = frame.Find("Recalibrate").GetComponent<Button>();
            mute.transform.SetAsLastSibling();
            blinkingMute = new BlinkingDesktopTarget((RectTransform)mute.transform);
            mute.GetComponentInChildren<Text>(true).text = "Mute";
            mute.onClick = new Button.ButtonClickedEvent();
            mute.onClick.AddListener(() =>
            {
                if (!IsOpen || !mute.gameObject.activeInHierarchy) return;
                siren.Stop();
                window.Close();
                cleared();
            });

            siren = window.gameObject.AddComponent<AudioSource>();
            siren.playOnAwake = false;
            siren.loop = true;
            siren.clip = desktop.alarmClip;
            siren.outputAudioMixerGroup = desktop.clicks.outputAudioMixerGroup;
        }

        public void Show(bool muted)
        {
            elapsed = 0;
            window.Open();
            blinkingMute.Show();
            Tick(0);
            SetMuted(muted);
            if (siren.clip) siren.Play();
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            mute.image.color = Color.Lerp(new Color32(255, 184, 72, 255), Color.white, Mathf.PingPong(elapsed * 2, 1));
            blinkingMute.Tick(dt);
        }

        public void SetMuted(bool muted) => siren.mute = muted;

        public void KeepInFront()
        {
            var frame = window.transform;
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
