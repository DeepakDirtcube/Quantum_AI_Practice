using System.Diagnostics;
using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable]
    public unsafe class ReachedDestinationDecision : HFSMDecision
    {
        // Blackboard key which stores current target position
        public AIBlackboardValueKey TargetPositionKey;

        // Optional: threshold distance to consider "reached"
        public FP ReachThreshold = FP._1_50; // 1 meter by default

        public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            // Get the Transform component of the entity
            if (!frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
                return false;

            // Get the Blackboard component
            if (!frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var bb))
                return false;

            // Read target position from blackboard
            FPVector3 targetPos = bb->GetVector3(frame, TargetPositionKey.Key);

            // Calculate squared distance
            FPVector3 delta = targetPos - transform->Position;
            FP distanceSquared = delta.SqrMagnitude;
            // FP distanceSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
            // Log.Info($"[ReachedDestinationDecision] Entity {distanceSquared} ");
            // Log.Info($"[ReachedDestinationDecision] Entity {ReachThreshold} ");

            // Check if within threshold
            return distanceSquared <= ReachThreshold;
        }
    }
}
