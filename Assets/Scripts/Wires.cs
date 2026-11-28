using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Gameplay
{
    public sealed class Wires : MonoBehaviour
    {
        public enum WireColor { Red, Blue, Yellow, Green, White }

        [System.Serializable]
        struct WireSlot
        {
            public Button button;
            public Image stripe;
            public Renderer[] cablePieces;
            public GameObject cutSection;
            public Text label;
        }

        static readonly Color32[] Palette =
        {
            new(247, 100, 93, 255), new(92, 174, 246, 255), new(249, 202, 89, 255),
            new(106, 207, 142, 255), new(228, 237, 240, 255)
        };

        [SerializeField] WireSlot[] slots;
        [SerializeField] Text status;
        GameManager round;
        int correctWire;
        bool solved;

        void Start()
        {
            round = FindAnyObjectByType<GameManager>();
            var colors = new WireColor[Random.Range(4, 6)];
            var first = (RectTransform)slots[0].button.transform;
            var second = (RectTransform)slots[1].button.transform;
            var centerOffset = (second.anchoredPosition - first.anchoredPosition) * ((slots.Length - colors.Length) * .5f);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                slot.button.gameObject.SetActive(i < colors.Length);
                if (i >= colors.Length) continue;
                ((RectTransform)slot.button.transform).anchoredPosition += centerOffset;

                colors[i] = (WireColor)Random.Range(0, Palette.Length);
                slot.stripe.color = Palette[(int)colors[i]];
                var tint = new MaterialPropertyBlock();
                tint.SetColor("_BaseColor", Palette[(int)colors[i]]);
                tint.SetColor("_EmissionColor", ((Color)Palette[(int)colors[i]]).linear * .6f);
                if (slot.cablePieces != null)
                    foreach (var piece in slot.cablePieces) piece.SetPropertyBlock(tint);
                slot.label.text = colors[i].ToString().ToUpperInvariant();
                int index = i;
                slot.button.onClick.AddListener(() => Cut(index));
            }
            correctWire = FindCorrectWire(colors);
        }

        public static int FindCorrectWire(WireColor[] colors)
        {
            var counts = new int[Palette.Length];
            foreach (var color in colors) counts[(int)color]++;

            // The manual always reads left to right, even after a wrong cut.
            for (int i = 0; i < colors.Length; i++)
            {
                int count = counts[(int)colors[i]];
                if (colors.Length == 4 && count > 1) return i;
                if (colors.Length == 5 && count == 1) return i;
            }
            return colors.Length == 4 ? 1 : 3;
        }

        void Cut(int index)
        {
            var slot = slots[index];
            if (solved || !slot.button.interactable || !round.IsPlaying) return;
            slot.button.interactable = false;
            slot.stripe.enabled = false;
            if (slot.cutSection) slot.cutSection.SetActive(false);
            slot.label.text += "\nCUT";
            if (index == correctWire)
            {
                solved = true;
                status.text = "DEFUSED";
                foreach (var wire in slots) wire.button.interactable = false;
                round.Defuse(Module.Wires);
            }
            else
            {
                status.text = "WRONG CUT";
                round.AddStrike();
            }
        }
    }
}