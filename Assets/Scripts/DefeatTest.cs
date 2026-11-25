#if UNITY_EDITOR
namespace CtrlAltDefuse.Gameplay
{
    public static class DefeatTest
    {
        public static void Trigger(GameManager round)
        {
            while (round.State == RoundState.Playing)
                round.AddStrike();
        }
    }
}
#endif