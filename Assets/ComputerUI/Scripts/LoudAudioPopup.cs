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
        readonly RelayDesktop desktop;
        readonly System.Action cleared;
        readonly Text caption, mixerHint;
        readonly GameObject heading, message, icon;
        readonly RectTransform mixer;
        static readonly string[] ChannelNames = { "Desk", "Notifications", "Aux" };
        readonly Button[] channelButtons = new Button[ChannelNames.Length];
        readonly RectTransform[] levels = new RectTransform[ChannelNames.Length];
        int alarmChannel;
        float elapsed, startedAt;
        public bool IsOpen => window.IsOpen;

        public LoudAudioPopup(RelayDesktop desktop, System.Action cleared)
        {
            this.desktop = desktop;
            this.cleared = cleared;
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Loud Audio";
            var frame = (RectTransform)window.transform;
            frame.anchoredPosition = ((RectTransform)desktop.manual.transform).anchoredPosition;
            caption = window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>();
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            heading = frame.Find(desktop.guardTitle.name).gameObject;
            message = frame.Find(desktop.guardMessage.name).gameObject;
            icon = frame.Find("Guard").gameObject;
            heading.GetComponent<Text>().text = "Alarm channel overridden";
            message.GetComponent<Text>().text = "Click the flashing Mute button to open the sound mixer.";

            mute = frame.Find("Recalibrate").GetComponent<Button>();
            mute.transform.SetAsLastSibling();
            blinkingMute = new BlinkingDesktopTarget((RectTransform)mute.transform);
            mute.GetComponentInChildren<Text>(true).text = "Mute";
            mute.onClick = new Button.ButtonClickedEvent();
            mute.onClick.AddListener(OpenMixer);

            mixer = (RectTransform)new GameObject("Sound mixer", typeof(RectTransform)).transform;
            mixer.gameObject.layer = frame.gameObject.layer;
            mixer.SetParent(frame, false);
            mixer.anchorMin = Vector2.zero;
            mixer.anchorMax = Vector2.one;
            mixer.offsetMin = mixer.offsetMax = Vector2.zero;
            mixerHint = Object.Instantiate(desktop.guardMessage, mixer);
            Place(mixerHint.rectTransform, 16, 39, 415, 25);
            for (int i = 0; i < ChannelNames.Length; i++) BuildChannel(i);

            siren = window.gameObject.AddComponent<AudioSource>();
            siren.playOnAwake = false;
            siren.loop = true;
            siren.clip = desktop.alarmClip;
            siren.outputAudioMixerGroup = desktop.clicks.outputAudioMixerGroup;
        }

        public void Show(bool muted)
        {
            elapsed = 0;
            startedAt = Time.unscaledTime;
            alarmChannel = Random.Range(0, ChannelNames.Length);
            caption.text = "RELAY Audio Control";
            mixer.gameObject.SetActive(false);
            heading.SetActive(true);
            message.SetActive(true);
            icon.SetActive(true);
            window.Open();
            blinkingMute.Show();
            Tick(0);
            SetMuted(muted);
            if (siren.clip) siren.Play();
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            if (mixer.gameObject.activeSelf)
            {
                UpdateMeters();
                return;
            }
            mute.image.color = Color.Lerp(new Color32(255, 184, 72, 255), Color.white, Mathf.PingPong(elapsed * 2, 1));
            blinkingMute.Tick(dt);
        }

        public void SetMuted(bool muted) => siren.mute = muted;

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        Image Panel(string name, Transform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = mixer.gameObject.layer;
            var image = obj.GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        void BuildChannel(int index)
        {
            float top = 66 + 32 * index;
            var label = Object.Instantiate(desktop.guardMessage, mixer);
            label.text = ChannelNames[index];
            label.alignment = TextAnchor.MiddleLeft;
            Place(label.rectTransform, 16, top, 109, 27);

            var track = Panel(ChannelNames[index] + " meter", mixer, new Color32(32, 48, 57, 255));
            Place(track.rectTransform, 129, top + 6, 176, 15);
            var level = Panel("Signal", track.transform, new Color32(69, 219, 161, 255)).rectTransform;
            level.anchorMin = Vector2.zero;
            level.anchorMax = Vector2.one;
            level.offsetMin = level.offsetMax = Vector2.zero;
            levels[index] = level;

            var button = Object.Instantiate(mute, mixer);
            button.name = "Mute " + ChannelNames[index];
            Place((RectTransform)button.transform, 316, top, 114, 27);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => MuteChannel(index));
            channelButtons[index] = button;
        }

        void OpenMixer()
        {
            if (!IsOpen || !mute.gameObject.activeInHierarchy || desktop.Sleeping || !desktop.input.Engaged) return;
            mute.gameObject.SetActive(false);
            heading.SetActive(false);
            message.SetActive(false);
            icon.SetActive(false);
            caption.text = "RELAY Sound Mixer";
            mixerHint.text = "Mute the channel with the pulsing meter.";
            mixer.gameObject.SetActive(true);
            foreach (var button in channelButtons)
            {
                button.interactable = true;
                button.GetComponentInChildren<Text>().text = "Mute";
            }
            UpdateMeters();
        }

        void UpdateMeters()
        {
            // The alarm clip plays two alternating notes per loop. Use its playback
            // position before volume/mute so the attack of each note is visible.
            float loopSeconds = siren.clip ? siren.clip.length : 1f;
            float seconds = siren.isPlaying && siren.clip
                ? (float)siren.timeSamples / siren.clip.frequency
                : Time.unscaledTime - startedAt;
            float beat = Mathf.Repeat(seconds * 2f / loopSeconds, 1f);
            float pulse = Mathf.Lerp(1f, .07f, beat);
            for (int i = 0; i < levels.Length; i++)
            {
                float amount = i == alarmChannel ? pulse : .15f + .04f * i;
                levels[i].anchorMax = new Vector2(channelButtons[i].interactable ? amount : 0, 1);
            }
        }

        void MuteChannel(int index)
        {
            if (!IsOpen || !mixer.gameObject.activeInHierarchy || desktop.Sleeping || !desktop.input.Engaged) return;
            if (index != alarmChannel)
            {
                channelButtons[index].interactable = false;
                channelButtons[index].GetComponentInChildren<Text>().text = "Muted";
                mixerHint.text = "Alarm still active. Look for the pulsing meter.";
                UpdateMeters();
                return;
            }
            siren.Stop();
            window.Close();
            cleared();
        }

        public void KeepInFront()
        {
            var frame = window.transform;
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
