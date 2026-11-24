using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class RelayDesktop : MonoBehaviour
    {
        const float HijackCycleSeconds = 2.8f;
        const float InversionStartSeconds = 1.55f;
        enum Virus { PopUpAd, LongEula, CursorHijack, LoudAudio, ScreenGlitch, CaptchaWall, CursorParticleTrail }

        public DesktopWindow manual, guard;
        public Button manualShortcut, manualTask, guardShortcut, calibrate, mute, power, wake;
        public Text guardTitle, guardMessage, muteLabel, statusLabel;
        public Image statusLamp;
        public GameObject standby;
        public AudioSource clicks;
        public MonitorInput input;
        [Header("Virus sequence")]
        [SerializeField, Min(1)] float virusGapSeconds = 10f;
        [Header("Pop-up Ad")]
        public AudioClip adChime;
        [Header("Loud Audio")]
        public AudioClip alarmClip;
        [Header("Screen Glitch")]
        public Texture2D signalNoise;
        public AudioClip staticCrackle;
        CursorTrailPopup trail;
        CaptchaWallPopup captcha;
        ScreenGlitchPopup glitch;
        LoudAudioPopup alarm;
        LongEulaPopup eula;
        PopUpAd ad;
        Button guardCloseButton;
        Virus nextVirus;
        bool PopupOpen => (ad?.IsOpen ?? false) || (eula?.IsOpen ?? false) || (alarm?.IsOpen ?? false)
            || (glitch?.IsOpen ?? false) || (captcha?.IsOpen ?? false) || (trail?.IsOpen ?? false);
        public bool Hijacked { get; private set; }
        public bool Sleeping => standby.activeSelf;
        public bool CursorInverted => Hijacked && !Sleeping && hijackPhase >= InversionStartSeconds;
        float timeRemaining;
        int displayedSeconds = -1;
        float hijackPhase;
        float surgeRemaining;
        Vector2 surgeVelocity;

        void Awake()
        {
            manualShortcut.onClick.AddListener(manual.Open);
            manualTask.onClick.AddListener(manual.Open);
            guardShortcut.onClick.AddListener(guard.Open);
            calibrate.onClick.AddListener(Recalibrate);
            mute.onClick.AddListener(ToggleMute);
            power.onClick.AddListener(() => ClickStandby(true));
            wake.onClick.AddListener(() => ClickStandby(false));
            Schedule(Virus.PopUpAd);
            manual.Open();
        }

        void Schedule(Virus virus)
        {
            nextVirus = virus;
            timeRemaining = virusGapSeconds;
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
                Virus.LoudAudio => "Alarm",
                Virus.ScreenGlitch => "Glitch",
                Virus.CaptchaWall => "Captcha",
                Virus.CursorParticleTrail => "Trail",
                _ => "Hijack"
            };
            statusLabel.text = $"{name} {seconds}s";
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        void Tick(float deltaTime)
        {
            if (!Sleeping && glitch != null && glitch.IsOpen) glitch.Tick(deltaTime);
            if (!input.Engaged || Sleeping) return;
            if (Hijacked)
            {
                hijackPhase = Mathf.Repeat(hijackPhase + deltaTime, HijackCycleSeconds);
                surgeRemaining -= deltaTime;
                if (surgeRemaining <= 0) StartPointerSurge();
                return;
            }

            if (ad != null && ad.IsOpen) ad.Tick(deltaTime);
            if (alarm != null && alarm.IsOpen) alarm.Tick(deltaTime);
            if (captcha != null && captcha.IsOpen)
            {
                captcha.ReadKeyboard();
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
                case Virus.LoudAudio: StartLoudAudio(); break;
                case Virus.ScreenGlitch: StartScreenGlitch(); break;
                case Virus.CaptchaWall: StartCaptchaWall(); break;
                case Virus.CursorParticleTrail: StartCursorParticleTrail(); break;
            }
        }

        void LateUpdate()
        {
            if (Hijacked && !Sleeping && guard.IsOpen &&
                guard.transform.GetSiblingIndex() != guard.transform.parent.childCount - 1) guard.Focus();
            if (ad != null && ad.IsOpen) ad.KeepInFront();
            if (eula != null && eula.IsOpen) eula.KeepInFront();
            if (alarm != null && alarm.IsOpen) alarm.KeepInFront();
            if (glitch != null && glitch.IsOpen) glitch.KeepInFront();
            if (captcha != null && captcha.IsOpen) captcha.KeepInFront();
            if (trail != null && (trail.IsOpen || trail.IsFading))
                trail.UpdateTrail(Time.unscaledDeltaTime, trail.IsOpen && input.Engaged && !Sleeping);
        }

        public void StartLongEula()
        {
            if (nextVirus != Virus.LongEula || Sleeping || Hijacked || PopupOpen) return;
            eula ??= new LongEulaPopup(this, () => Schedule(Virus.CursorHijack));
            eula.Show();
            UpdateStatus();
        }

        public void StartPopUpAd()
        {
            if (nextVirus != Virus.PopUpAd || Sleeping || Hijacked || PopupOpen) return;
            ad ??= new PopUpAd(this, () => Schedule(Virus.LongEula));
            ad.Show();
            UpdateStatus();
            if (adChime && !clicks.mute) clicks.PlayOneShot(adChime);
        }

        public void StartCursorHijack()
        {
            if (nextVirus != Virus.CursorHijack || Sleeping || Hijacked || PopupOpen) return;
            hijackPhase = 0f;
            StartPointerSurge();
            Hijacked = true;
            SetGuardClosable(false);
            UpdateStatus();
            guard.Open();
            PlayClick();
        }

        void SetGuardClosable(bool closable)
        {
            if (!guardCloseButton) guardCloseButton = guard.titleBar.GetComponentInChildren<Button>(true);
            guardCloseButton.gameObject.SetActive(closable);
        }

        void StartPointerSurge()
        {
            float angle = Random.Range(0f, 2 * Mathf.PI);
            surgeVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(45f, 95f);
            surgeRemaining = Random.Range(.3f, .55f);
        }

        internal Vector2 DistortPointer(Vector2 delta, float dt)
        {
            float gain = CursorInverted ? -1.05f : 1.1f;
            var wobble = new Vector2(Mathf.Sin(hijackPhase * 7), Mathf.Cos(hijackPhase * 9)) * 32f;
            return delta * gain + (surgeVelocity + wobble) * dt;
        }

        public void Recalibrate()
        {
            if (Hijacked)
            {
                Hijacked = false;
                SetGuardClosable(true);
                Schedule(Virus.LoudAudio);
            }
            input.CenterCursor();
            UpdateStatus();
            guard.Close();
        }

        internal void ClearCursorTrail()
        {
            if (trail == null) return;
            trail.Clear();
            input.CenterCursor();
            Schedule(Virus.PopUpAd);
        }

        public void StartLoudAudio()
        {
            if (nextVirus != Virus.LoudAudio || Sleeping || Hijacked || PopupOpen) return;
            alarm ??= new LoudAudioPopup(this, () => Schedule(Virus.ScreenGlitch));
            alarm.Show(clicks.mute);
            UpdateStatus();
        }

        public void StartScreenGlitch()
        {
            if (nextVirus != Virus.ScreenGlitch || Sleeping || Hijacked || PopupOpen) return;
            glitch ??= new ScreenGlitchPopup(this, SetStandby);
            glitch.Show();
            UpdateStatus();
        }

        public void StartCaptchaWall()
        {
            if (nextVirus != Virus.CaptchaWall || Sleeping || Hijacked || PopupOpen) return;
            captcha ??= new CaptchaWallPopup(this, () => Schedule(Virus.CursorParticleTrail));
            captcha.Show();
            UpdateStatus();
        }

        public void StartCursorParticleTrail()
        {
            if (nextVirus != Virus.CursorParticleTrail || Sleeping || Hijacked || PopupOpen) return;
            trail ??= new CursorTrailPopup(this);
            trail.Show();
            UpdateStatus();
        }

        void OnDisable() => captcha?.DetachKeyboard();

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
            alarm?.SetMuted(clicks.mute || Sleeping);
            muteLabel.text = clicks.mute ? "Unmute" : "Mute";
        }

        void ClickStandby(bool value)
        {
            if (glitch == null || !glitch.IsOpen) SetStandby(value);
        }

        void SetStandby(bool value)
        {
            if (Sleeping == value) return;
            standby.SetActive(value);
            alarm?.SetMuted(clicks.mute || value);
            if (glitch != null && glitch.IsOpen)
            {
                if (value) glitch.PowerOff();
                else
                {
                    glitch.Recover();
                    Schedule(Virus.CaptchaWall);
                }
            }
            input.CenterCursor();
        }

        public void PlayClick()
        {
            if (clicks.clip && !clicks.mute) clicks.PlayOneShot(clicks.clip);
        }
    }
}
