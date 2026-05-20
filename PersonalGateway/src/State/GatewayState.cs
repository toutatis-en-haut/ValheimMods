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

        public static void Reset()
        {
            Phase = ArmingPhase.Idle;
            SelectedTrophy = null;
        }

        public static void Arm()
        {
            Phase = ArmingPhase.AwaitingTrophy;
            SelectedTrophy = null;
        }

        public static void SelectTrophy(ItemDrop.ItemData trophy)
        {
            SelectedTrophy = trophy;
            Phase = ArmingPhase.AwaitingDestination;
        }

        public static bool IsArmed => Phase != ArmingPhase.Idle;
    }
}
