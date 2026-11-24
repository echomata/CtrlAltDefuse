using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class RelayDesktop : MonoBehaviour
    {
        const float HijackCycleSeconds = 6f;
        const float InversionStartSeconds = 4f;
        enum Virus { PopUpAd, LongEula, CursorHijack }

        public DesktopWindow manual, guard;
        public Button manualShortcut, manualTask, guardShortcut, calibrate, mute, power, wake;
        public Text guardTitle, guardMessage, muteLabel, statusLabel;
        public Image statusLamp;
        public GameObject standby;
        public AudioSource clicks;
        public MonitorInput input;
        [Min(1)] public float firstHijackDelay = 20;
        [Min(1)] public float repeatDelay = 60;
        [Header("Pop-up Ad")]
        [Min(1)] public float firstAdDelay = 8;
        [Min(1)] public float repeatAdDelay = 45;
        public AudioClip adChime;
        [Header("Long EULA")]
        [Min(1)] public float firstEulaDelay = 14;
        [Min(1)] public float repeatEulaDelay = 65;
        LongEulaPopup eula;
        PopUpAd ad;
        Virus nextVirus;
        bool repeating;
        bool PopupOpen => (ad?.IsOpen ?? false) || (eula?.IsOpen ?? false);
        public bool Hijacked { get; private set; }
        public bool Sleeping => standby.activeSelf;
        public bool CursorInverted => Hijacked && !Sleeping && hijackPhase >= InversionStartSeconds;
        float timeRemaining;
        int displayedSeconds = -1;
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
            Schedule(Virus.PopUpAd, firstAdDelay);
            manual.Open();
        }

        void Schedule(Virus virus, float delay)
        {
            nextVirus = virus;
            timeRemaining = delay;
            displayedSeconds = -1;
            UpdateStatus();
        }

        void ShowCountdown()
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            if (seconds == displayedSeconds) return;
            displayedSeconds = seconds;
            string name = nextVirus switch
            {
                Virus.PopUpAd => "Ad",
                Virus.LongEula => "EULA",
                _ => "Hijack"
            };
            statusLabel.text = $"{name} {seconds}s";
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        void Tick(float deltaTime)
        {
            if (!input.Engaged || Sleeping) return;
            if (Hijacked)
            {
                hijackPhase = Mathf.Repeat(hijackPhase + deltaTime, HijackCycleSeconds);
                return;
            }

            if (PopupOpen) return;
            timeRemaining -= deltaTime;
            if (timeRemaining > 0)
            {
                ShowCountdown();
                return;
            }
            switch (nextVirus)
            {
                case Virus.PopUpAd: StartPopUpAd(); break;
                case Virus.LongEula: StartLongEula(); break;
                case Virus.CursorHijack: StartCursorHijack(); break;
            }
        }

        void LateUpdate()
        {
            if (ad != null && ad.IsOpen) ad.KeepInFront();
            if (eula != null && eula.IsOpen) eula.KeepInFront();
        }

        public void StartLongEula()
        {
            if (nextVirus != Virus.LongEula || Sleeping || Hijacked || PopupOpen) return;
            eula ??= new LongEulaPopup(this, () =>
                Schedule(Virus.CursorHijack, repeating ? repeatDelay : firstHijackDelay));
            eula.Show();
            UpdateStatus();
        }

        public void StartPopUpAd()
        {
            if (nextVirus != Virus.PopUpAd || Sleeping || Hijacked || PopupOpen) return;
            ad ??= new PopUpAd(this, () =>
                Schedule(Virus.LongEula, repeating ? repeatEulaDelay : firstEulaDelay));
            ad.Show();
            UpdateStatus();
            if (adChime && !clicks.mute) clicks.PlayOneShot(adChime);
        }

        public void StartCursorHijack()
        {
            if (nextVirus != Virus.CursorHijack || Sleeping || Hijacked || PopupOpen) return;
            hijackPhase = 0f;
            Hijacked = true;
            UpdateStatus();
            guard.Open();
            PlayClick();
        }

        public void Recalibrate()
        {
            if (Hijacked)
            {
                Hijacked = false;
                repeating = true;
                Schedule(Virus.PopUpAd, repeatAdDelay);
            }
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
            bool interference = Hijacked || PopupOpen;
            if (interference) statusLabel.text = "Interference";
            else ShowCountdown();
            statusLamp.color = interference ? new Color32(247, 119, 75, 255) : new Color32(110, 223, 178, 255);
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
