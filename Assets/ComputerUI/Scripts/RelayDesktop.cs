using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class RelayDesktop : MonoBehaviour
    {
        const float HijackCycleSeconds = 6f;
        const float InversionStartSeconds = 4f;

        public DesktopWindow manual, guard;
        public Button manualShortcut, manualTask, guardShortcut, calibrate, mute, power, wake;
        public Text guardTitle, guardMessage, muteLabel, statusLabel;
        public Image statusLamp;
        public GameObject standby;
        public AudioSource clicks;
        public MonitorInput input;
        [Min(1)] public float firstHijackDelay = 20;
        [Min(1)] public float repeatDelay = 60;
        public bool Hijacked { get; private set; }
        public bool Sleeping => standby.activeSelf;
        public bool CursorInverted => Hijacked && !Sleeping && hijackPhase >= InversionStartSeconds;
        float timeRemaining;
        float hijackPhase;

        void Awake()
        {
            manualShortcut.onClick.AddListener(manual.Open);
            manualTask.onClick.AddListener(manual.Open);
            guardShortcut.onClick.AddListener(guard.Open);
            calibrate.onClick.AddListener(Recalibrate);
            mute.onClick.AddListener(ToggleMute);
            power.onClick.AddListener(() => SetStandby(true));
            wake.onClick.AddListener(() => SetStandby(false));
            timeRemaining = firstHijackDelay;
            UpdateStatus();
            manual.Open();
        }

        void Update()
        {
            if (!input.Engaged || Sleeping) return;
            if (Hijacked)
            {
                hijackPhase = Mathf.Repeat(hijackPhase + Time.unscaledDeltaTime, HijackCycleSeconds);
                return;
            }

            timeRemaining -= Time.unscaledDeltaTime;
            if (timeRemaining <= 0) StartCursorHijack();
        }

        public void StartCursorHijack()
        {
            hijackPhase = 0f;
            Hijacked = true;
            UpdateStatus();
            guard.Open();
            PlayClick();
        }

        public void Recalibrate()
        {
            Hijacked = false;
            timeRemaining = repeatDelay;
            input.CenterCursor();
            UpdateStatus();
            guard.Close();
        }

        void UpdateStatus()
        {
            guardTitle.text = Hijacked ? "Cursor hijack detected" : "Pointer is calibrated";
            guardMessage.text = Hijacked
                ? "Your pointer drifts and sometimes reverses direction. Use Recalibrate to restore control."
                : "Pointer tracking is stable. Recalibrate at any time to return the cursor to the center.";
            statusLabel.text = Hijacked ? "Interference" : "Online";
            statusLamp.color = Hijacked ? new Color32(247, 119, 75, 255) : new Color32(110, 223, 178, 255);
        }

        void ToggleMute()
        {
            clicks.mute = !clicks.mute;
            muteLabel.text = clicks.mute ? "Unmute" : "Mute";
        }

        void SetStandby(bool value)
        {
            standby.SetActive(value);
            input.CenterCursor();
        }

        public void PlayClick()
        {
            if (clicks.clip && !clicks.mute) clicks.PlayOneShot(clicks.clip);
        }
    }
}
