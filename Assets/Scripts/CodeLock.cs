using UnityEngine;

namespace CtrlAltDefuse.Gameplay
{
    public sealed class CodeLock : MonoBehaviour
    {
        // The eight letter groups appear in the same order in the PC manual.
        static readonly string[] LetterGroups = { "ABC", "DEF", "GHI", "JKL", "MNO", "PQRS", "TUV", "WXYZ" };
        static readonly string[] Labels = { "CAT", "JAM", "ZIP", "FOX", "BEE", "MAP" };

        public string Label { get; private set; }
        public string Entry { get; private set; } = "";
        public bool Solved { get; private set; }
        public string Feedback { get; private set; } = "ENTER 3 DIGITS";
        public GameManager Round { get; private set; }
        string answer;

        void Awake()
        {
            Round = FindAnyObjectByType<GameManager>();
            Label = Labels[Random.Range(0, Labels.Length)];
            answer = Encode(Label);
        }

        public static string Encode(string label)
        {
            string code = "";
            foreach (char letter in label)
            {
                int group = System.Array.FindIndex(LetterGroups, letters => letters.Contains(letter.ToString()));
                if (group < 0) throw new System.ArgumentException("Expected an uppercase letter.", nameof(label));
                code += (group + 2).ToString();
            }
            return code;
        }

        public void PressDigit(int digit)
        {
            if (Solved || !Round || !Round.IsPlaying || Entry.Length == 3 || digit < 0 || digit > 9) return;
            Entry += digit.ToString();
            Feedback = Entry.Length == 3 ? "PRESS ENTER" : "ENTER 3 DIGITS";
        }

        public void Clear()
        {
            if (Solved || !Round || !Round.IsPlaying) return;
            Entry = "";
            Feedback = "ENTER 3 DIGITS";
        }

        public void Submit()
        {
            if (Solved || !Round || !Round.IsPlaying || Entry.Length != 3) return;
            if (Entry == answer)
            {
                Solved = true;
                Feedback = "MODULE DEFUSED";
                Round.Defuse(Module.CodeLock);
            }
            else
            {
                Entry = "";
                Feedback = "WRONG CODE";
                Round.AddStrike();
            }
        }
    }
}