using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable]
    public unsafe class UpdateDestinationValueAction : AIAction
    {
        // The key in the blackboard to set the destination
        public AIBlackboardValueKey DestinationKey;
        public AIBlackboardValueKey HarvestEnabledKey; // boolean key

        // The new value to set (can also be read from config/blackboard)
        public FPVector3 TargetPosition;

        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            // Get blackboard component
            if (!frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboard))
                return;

            // Set the new destination in the blackboard
            blackboard->Set(frame, DestinationKey.Key, TargetPosition);

            blackboard->Set(frame, HarvestEnabledKey.Key, true);

            // Optional: log for debugging
            Log.Debug($"Set new destination {TargetPosition} for entity {entity}");
        }
    }
}
