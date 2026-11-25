using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class RelayDesktop : MonoBehaviour
    {
        const float HijackCycleSeconds = 2.8f;
        const float InversionStartSeconds = 1.55f;
        enum Virus { PopUpAd, LongEula, CursorHijack, LoudAudio, ScreenGlitch, CaptchaWall, CursorParticleTrail, WrongLanguage }

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
        [SerializeField] PopUpAd.Design[] adDesigns;
        public AudioClip adChime;
        [Header("Loud Audio")]
        public AudioClip alarmClip;
        [Header("Screen Glitch")]
        public Texture2D signalNoise;
        public AudioClip staticCrackle;
        [Header("Wrong Language")]
        [SerializeField] Texture2D reloadIcon;
        WrongLanguagePopup language;
        CursorTrailPopup trail;
        CaptchaWallPopup captcha;
        ScreenGlitchPopup glitch;
        LoudAudioPopup alarm;
        LongEulaPopup eula;
        PopUpAd ad;
        DesktopWindow upperCalibration, lowerCalibration;
        Virus nextVirus;
        bool PopupOpen => (ad?.IsOpen ?? false) || (eula?.IsOpen ?? false) || (alarm?.IsOpen ?? false)
            || (glitch?.IsOpen ?? false) || (captcha?.IsOpen ?? false) || (trail?.IsOpen ?? false)
            || (language?.IsOpen ?? false);
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
            guardShortcut.onClick.AddListener(OpenGuard);
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
                Virus.WrongLanguage => "Locale",
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
                case Virus.WrongLanguage: StartWrongLanguage(); break;
            }
        }

        void LateUpdate()
        {
            if (Hijacked && !Sleeping && manual.IsOpen)
            {
                KeepAboveManual(upperCalibration);
                KeepAboveManual(lowerCalibration);
            }
            if (ad != null && ad.IsOpen) ad.KeepInFront();
            if (eula != null && eula.IsOpen) eula.KeepInFront();
            if (alarm != null && alarm.IsOpen) alarm.KeepInFront();
            if (glitch != null && glitch.IsOpen) glitch.KeepInFront();
            if (captcha != null && captcha.IsOpen) captcha.KeepInFront();
            if (trail != null && (trail.IsOpen || trail.IsFading))
                trail.UpdateTrail(Time.unscaledDeltaTime, trail.IsOpen && input.Engaged && !Sleeping);
            if (language != null && language.IsOpen) language.KeepInFront();
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
            ad ??= new PopUpAd(this, adDesigns, () => Schedule(Virus.LongEula));
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
            guard.Close();
            if (!upperCalibration)
                upperCalibration = CreateCalibration("Pointer tracking", Vector2.up, new Vector2(14, -14));
            if (!lowerCalibration)
                lowerCalibration = CreateCalibration("Pointer direction", Vector2.right, new Vector2(-14, 14));
            upperCalibration.Open();
            lowerCalibration.Open();
            UpdateStatus();
            PlayClick();
        }

        DesktopWindow CreateCalibration(string channel, Vector2 anchor, Vector2 offset)
        {
            var window = Instantiate(guard, guard.transform.parent);
            window.name = channel;
            var frame = (RectTransform)window.transform;
            frame.anchorMin = frame.anchorMax = frame.pivot = anchor;
            frame.anchoredPosition = offset;
            frame.Find(guardTitle.name).GetComponent<Text>().text = channel;
            frame.Find(guardMessage.name).GetComponent<Text>().text =
                "Click Recalibrate in both windows to restore cursor control.";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            var button = frame.Find("Recalibrate").GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                if (!Hijacked || Sleeping || !window.IsOpen) return;
                window.Close();
                if (upperCalibration.IsOpen || lowerCalibration.IsOpen) return;
                Hijacked = false;
                input.CenterCursor();
                Schedule(Virus.LoudAudio);
            });
            return window;
        }

        void KeepAboveManual(DesktopWindow window)
        {
            if (window.IsOpen && window.transform.GetSiblingIndex() < manual.transform.GetSiblingIndex())
                window.Focus();
        }

        void OpenGuard()
        {
            if (Hijacked) (upperCalibration.IsOpen ? upperCalibration : lowerCalibration).Focus();
            else guard.Open();
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
                OpenGuard();
                return;
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
            Schedule(Virus.WrongLanguage);
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

        public void StartWrongLanguage()
        {
            if (nextVirus != Virus.WrongLanguage || Sleeping || Hijacked || PopupOpen) return;
            language ??= new WrongLanguagePopup(this, reloadIcon, () => Schedule(Virus.PopUpAd));
            language.Show();
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
            if (language != null && language.IsOpen)
            {
                guardTitle.text = "Puntero calibrado";
                guardMessage.text = "El puntero funciona bien. Pulsa Recalibrar para centrarlo.";
                statusLabel.text = "Interferencia";
            }
            else if (interference) statusLabel.text = "Interference";
            else ShowCountdown();
            RefreshMuteLabel();
            statusLamp.color = interference ? new Color32(247, 119, 75, 255) : new Color32(110, 223, 178, 255);
        }

        void ToggleMute()
        {
            clicks.mute = !clicks.mute;
            alarm?.SetMuted(clicks.mute || Sleeping);
            RefreshMuteLabel();
        }

        void RefreshMuteLabel()
        {
            muteLabel.text = language != null && language.IsOpen
                ? (clicks.mute ? "Sonido" : "Silenciar")
                : (clicks.mute ? "Unmute" : "Mute");
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
