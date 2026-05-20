using UnityEngine;

namespace PersonalGateway.State
{
    public enum ArmingPhase
    {
        Idle,
        AwaitingTrophy,
        AwaitingDestination
    }

    internal static class GatewayState
    {
        public static ArmingPhase Phase { get; private set; } = ArmingPhase.Idle;
        public static ItemDrop.ItemData SelectedTrophy { get; private set; }
        public static float PhaseEnteredTime { get; private set; }

        public static void Reset()
        {
            Phase = ArmingPhase.Idle;
            SelectedTrophy = null;
            PhaseEnteredTime = Time.unscaledTime;
        }

        public static void Arm()
        {
            Phase = ArmingPhase.AwaitingTrophy;
            SelectedTrophy = null;
            PhaseEnteredTime = Time.unscaledTime;
        }

        public static void SelectTrophy(ItemDrop.ItemData trophy)
        {
            SelectedTrophy = trophy;
            Phase = ArmingPhase.AwaitingDestination;
            PhaseEnteredTime = Time.unscaledTime;
        }

        public static bool IsArmed => Phase != ArmingPhase.Idle;
        public static float SecondsInPhase => Time.unscaledTime - PhaseEnteredTime;
    }
}
