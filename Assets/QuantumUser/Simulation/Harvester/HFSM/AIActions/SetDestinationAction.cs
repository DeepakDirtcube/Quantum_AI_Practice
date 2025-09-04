using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable]
    public unsafe class SetDestinationAction : AIAction
    {
        public string DestinationKey = "MoveToTarget"; // Use string directly

        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            // ✅ Get the blackboard component
            var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
            if (blackboard == null)
            {
                Log.Error($"Entity {entity} has no AIBlackboardComponent!");
                return;
            }

            // ✅ Get the target position from the blackboard using string key
            Log.Info("1111111111111111___");
            FPVector3 targetPosition = blackboard->GetVector3(frame, "MoveToTarget");
            // ✅ Get the NavMeshPathfinder component
            var pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
            if (pathfinder == null)
            {
                Log.Error($"Entity {entity} has no NavMeshPathfinder component!");
                return;
            }

            // ✅ Set target using the first available NavMesh
            foreach (var navMeshEntry in frame.Map.NavMeshes)
            {
                var navMesh = frame.FindAsset<NavMesh>((AssetRef)navMeshEntry.Value);
                if (navMesh != null)
                {
                    pathfinder->SetTarget(frame, targetPosition, navMesh);
                    break;
                }
            }

            Log.Debug($"[SetDestinationAction] Destination set to {targetPosition} for entity {entity}");
        }
    }
}
