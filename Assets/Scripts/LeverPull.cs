using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Gameplay
{
    public sealed class LeverPull : MonoBehaviour
    {
        enum LightState { Red, Amber, Green }
        [SerializeField, Min(.1f)] float secondsPerColor = 1.5f;
        [SerializeField] Image indicator;
        [SerializeField] Text colorLabel, status;
        [SerializeField] Button lever;
        [SerializeField] RectTransform handle;
        GameManager round;
        LightState lightState;
        float elapsed;
        bool solved;

        void Start()
        {
            round = FindAnyObjectByType<GameManager>();
            lever.onClick.AddListener(Pull);
            ShowLight();
        }

        // Process clicks against the light shown on the previous rendered frame.
        void LateUpdate()
        {
            if (solved || !round.IsPlaying) return;
            elapsed += Time.deltaTime;
            if (elapsed < secondsPerColor) return;
            elapsed -= secondsPerColor;
            lightState = (LightState)(((int)lightState + 1) % 3);
            ShowLight();
        }

        void ShowLight()
        {
            colorLabel.text = lightState.ToString().ToUpperInvariant();
            indicator.color = lightState switch
            {
                LightState.Red => new Color32(247, 100, 93, 255),
                LightState.Amber => new Color32(249, 202, 89, 255),
                _ => new Color32(106, 207, 142, 255)
            };
        }

        void Pull()
        {
            if (solved || !round.IsPlaying) return;
            if (lightState == LightState.Green)
            {
                solved = true;
                lever.interactable = false;
                handle.localRotation = Quaternion.Euler(0, 0, -50);
                status.text = "DEFUSED";
                round.Defuse(Module.LeverPull);
            }
            else
            {
                status.text = "WRONG TIME";
                round.AddStrike();
            }
        }
    }
}