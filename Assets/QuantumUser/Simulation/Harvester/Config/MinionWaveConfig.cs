namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine;


    /// <summary>
    /// Make a Scriptable Quantum Config via "Create ▸ Quantum ▸ Config ▸ Minion Wave Config".
    /// Assign this in RuntimeConfig (Project: Quantum -> RuntimeConfig asset).
    /// </summary>
    [System.Serializable]
    public class MinionWaveConfig
    {
        public int Count = 10;
        public FPVector3 SpawnPos = new FPVector3(0, 5, 0);
        public FPVector3 TargetPos = new FPVector3(12, 5, 0);
        public string NavmeshName = "Navmesh";
        public AssetRef<NavMeshAgentConfig> AgentConfig;
        // public AssetRef<EntityView> View;
        public AssetRef<EntityPrototype> MinionPrototype;
        public FP DefaultStoppingDistance = FP._1 / 2;
        public FP WaitAtEndpointsSeconds = FP._0;

        public FP InitialDelaySeconds = FP._1;     // ⏱️ Delay before first wave
        public FP WaveIntervalSeconds = FP._5;     // ⏱️ Time between waves
        public FP InitialSpawnWaitSeconds = FP._1; // ⏱️ Delay before first minion spawn
        public FP DefaultLifeAfterReturnSeconds = FP._5;
        public int MaxWaves = 0;

        [Header("Harvest Config")]

        public FP MaxHarvest = FP._10; // Maximum energy a minion can collect            // 0 = infinite
        public FP HarvestRate = FP._1; // Energy collected per second when harvesting  // 0 = no harvesting
        public bool IsHarvestTimeBased = false; // true: HarvestRate is seconds-to-full; false: units-per-second
    }
    // Extend your RuntimeConfig (Quantum auto-generates partial class).
    public partial class RuntimeConfig
    {
        public MinionWaveConfig MinionWave;
    }
}
