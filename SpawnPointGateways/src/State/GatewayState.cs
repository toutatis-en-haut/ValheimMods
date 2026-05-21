using UnityEngine;

namespace SpawnPointGateways.State
{
    public enum ArmingPhase
    {
        Idle,
        AwaitingDestination
    }

    internal static class GatewayState
    {
        public static ArmingPhase Phase { get; private set; } = ArmingPhase.Idle;
        public static float PhaseEnteredTime { get; private set; }

        public static void Reset()
        {
            Phase = ArmingPhase.Idle;
            PhaseEnteredTime = Time.unscaledTime;
        }

        public static void ArmForDestination()
        {
            Phase = ArmingPhase.AwaitingDestination;
            PhaseEnteredTime = Time.unscaledTime;
        }

        public static bool IsArmed => Phase != ArmingPhase.Idle;
        public static float SecondsInPhase => Time.unscaledTime - PhaseEnteredTime;
    }
}
