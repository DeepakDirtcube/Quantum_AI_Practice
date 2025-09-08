using Photon.Deterministic;

namespace Quantum
{
    [System.Serializable]
    public unsafe class HarvestEnergyAction : AIAction
    {
        public AIBlackboardValueKey HarvestEnabledKey; // boolean key
        public AIBlackboardValueKey HarvestRateKey;    // FP key

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

            // Read harvesting rate from blackboard
            FP harvestRate = bb->GetFP(frame, HarvestRateKey.Key);

            // Update energy
            energy->CurrentEnergy += harvestRate * frame.DeltaTime;
            Log.Info($"Entity {entity} harvested {harvestRate * frame.DeltaTime}, total: {energy->CurrentEnergy}");

            // Clamp to max energy
            if (energy->CurrentEnergy > energy->MaxEnergy)
                energy->CurrentEnergy = energy->MaxEnergy;

            // Optional: debug log
        }
    }
}
