#if UNITY_EDITOR
namespace CtrlAltDefuse.Gameplay
{
    public static class VictoryTest
    {
        public static void Trigger(GameManager round)
        {
            round.Defuse(Module.Wires);
            round.Defuse(Module.CodeLock);
            round.Defuse(Module.LeverPull);
        }
    }
}
#endif