using System.Collections.Generic;
using UnityEngine;

namespace CtrlAltDefuse.Gameplay
{
    public enum Module { Wires, CodeLock, LeverPull }
    public enum RoundState { Playing, Won, Lost }

    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, Min(1)] float roundSeconds = 300;
        public float SecondsLeft { get; private set; }
        public int Strikes { get; private set; }
        public RoundState State { get; private set; }
        public bool IsPlaying => State == RoundState.Playing && Time.timeScale > 0f;
        readonly HashSet<Module> solved = new();

        void Awake() => SecondsLeft = roundSeconds;

        void Update()
        {
            if (State != RoundState.Playing) return;
            SecondsLeft = Mathf.Max(0, SecondsLeft - Time.deltaTime);
            if (SecondsLeft == 0) State = RoundState.Lost;
        }

        public void AddStrike()
        {
            if (State != RoundState.Playing) return;
            Strikes++;
            if (Strikes == 3) State = RoundState.Lost;
        }

        public void Defuse(Module module)
        {
            if (State != RoundState.Playing) return;
            solved.Add(module);
            if (solved.Count == 3) State = RoundState.Won;
        }
    }
}