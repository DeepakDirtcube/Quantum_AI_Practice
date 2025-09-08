using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable]
    public unsafe class HarvestEnergyAction : AIAction
    {
        public AIBlackboardValueKey HarvestEnabledKey; // boolean key
        public AIBlackboardValueKey HarvestRateKey;    // FP key
        public AIBlackboardValueKey DestinationKey;    // FPVector3 key (optional, falls back to "Destination")
        public AIBlackboardValueKey HarvestInitialDistanceKey; // FP key to cache initial distance
        public AIBlackboardValueKey HarvestAccumulatedKey;     // FP key to accumulate fractional time-based gain

        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            // Get the minion's energy component
            if (!frame.Unsafe.TryGetPointer<HarvesterEnergy>(entity, out var energy))
                return;

            // Get the blackboard component
            if (!frame.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var bb))
                return;

            if (string.IsNullOrEmpty(HarvestEnabledKey.Key))
            {
                Log.Error("[HarvestEnergyAction] HarvestEnabledKey is not set.");
                return;
            }

            if (string.IsNullOrEmpty(HarvestRateKey.Key))
            {
                Log.Error("[HarvestEnergyAction] HarvestRateKey is not set.");
                return;
            }
            // Check if harvesting is enabled
            bool harvestEnabled = bb->GetBoolean(frame, HarvestEnabledKey.Key);
            if (!harvestEnabled) return;

            // Resolve parameter from blackboard (preferred) or component fallback
            FP param = FP._0;
            if (!string.IsNullOrEmpty(HarvestRateKey.Key))
            {
                param = bb->GetFP(frame, HarvestRateKey.Key);
            }
            else
            {
                param = energy->HarvestParam;
            }

            // Two modes:
            // 1) Time-based: harvestRate > 0 -> accumulate per second (existing behavior)
            // 2) Distance-based: harvestRate == 0 -> accumulate based on progress towards Destination
            if (param > FP._0)
            {
                // Non-zero parameter: choose based on component flag
                if (energy->MaxEnergy <= 0)
                    return;

                FP perTick;
                if (energy->UseTimeMode)
                {
                    // Time-based: param is seconds-to-full
                    FP secondsToFull = param;
                    perTick = (FP)energy->MaxEnergy * frame.DeltaTime / secondsToFull;
                }
                else
                {
                    // Rate-based: param is units per second
                    perTick = param * frame.DeltaTime;
                }

                // Accumulate fractional progress in blackboard
                FP acc = FP._0;
                bool hasAccKey = !string.IsNullOrEmpty(HarvestAccumulatedKey.Key);
                if (hasAccKey)
                {
                    acc = bb->GetFP(frame, HarvestAccumulatedKey.Key);
                }

                acc += perTick;
                int inc = FPMath.FloorToInt(acc); // consume full integer units only
                if (inc > 0)
                {
                    acc -= (FP)inc;

                    int newValue = energy->CurrentEnergy + inc;
                    if (energy->MaxEnergy > 0 && newValue > energy->MaxEnergy)
                        newValue = energy->MaxEnergy;
                    energy->CurrentEnergy = newValue;
                }

                if (hasAccKey)
                {
                    bb->Set(frame, HarvestAccumulatedKey.Key, acc);
                }

                // Optional: debug
                // Log.Debug($"[HarvestEnergyAction] (+param mode={(energy->UseTimeMode?"time":"rate")}) acc={acc} total={energy->CurrentEnergy}");
                return;
            }

            // Distance-based accumulation: map path progress to energy [0..MaxEnergy]
            // Needs Transform and a Destination in blackboard
            if (!frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
                return;

            // Resolve destination from blackboard
            FPVector3 destination;
            if (!string.IsNullOrEmpty(DestinationKey.Key))
            {
                destination = bb->GetVector3(frame, DestinationKey.Key);
            }
            else
            {
                destination = bb->GetVector3(frame, "Destination");
            }

            // Current remaining distance to destination
            FPVector3 toTarget = destination - transform->Position;
            FP currentDistance = FPMath.Sqrt(toTarget.SqrMagnitude);

            // Initialize or read the initial distance (distance when entering harvesting)
            FP initialDistance = currentDistance;
            if (!string.IsNullOrEmpty(HarvestInitialDistanceKey.Key))
            {
                // Try to read cached initial distance; if zero, set it to current
                FP cached = bb->GetFP(frame, HarvestInitialDistanceKey.Key);
                if (cached > FP._0)
                {
                    initialDistance = cached;
                }
                else
                {
                    bb->Set(frame, HarvestInitialDistanceKey.Key, currentDistance);
                }
            }

            // Handle edge-case: if initial distance is ~0, treat as reached
            if (initialDistance <= FP._0)
            {
                if (energy->MaxEnergy > 0)
                    energy->CurrentEnergy = energy->MaxEnergy;
                return;
            }

            // Map progress over (initialDistance -> targetDistance)
            FP targetDistance = FP._0;
            if (frame.Unsafe.TryGetPointer<Minion>(entity, out var minion))
            {
                targetDistance = minion->StoppingDistance;
            }

            // If we effectively started at (or inside) targetDistance, treat as full
            if (initialDistance <= targetDistance)
            {
                if (energy->MaxEnergy > 0)
                    energy->CurrentEnergy = energy->MaxEnergy;
                return;
            }

            FP denom = initialDistance - targetDistance;
            FP progress = (denom > FP._0) ? (initialDistance - currentDistance) / denom : FP._1;
            if (progress < FP._0) progress = FP._0;
            if (progress > FP._1) progress = FP._1;

            FP desiredEnergyFP = (energy->MaxEnergy > 0) ? (progress * (FP)energy->MaxEnergy) : (FP)energy->CurrentEnergy;
            int desiredEnergy = FPMath.RoundToInt(desiredEnergyFP); // deterministic rounding

            // Apply monotonically (don't decrease energy if moving away)
            if (energy->MaxEnergy > 0)
            {
                if (desiredEnergy > energy->CurrentEnergy)
                {
                    if (desiredEnergy > energy->MaxEnergy)
                        desiredEnergy = energy->MaxEnergy;
                    energy->CurrentEnergy = desiredEnergy;
                }
            }

            // Optional: debug
            // Log.Debug($"[HarvestEnergyAction] (+distance) Entity {entity} progress={progress}, energy={energy->CurrentEnergy}");
        }
    }
}
