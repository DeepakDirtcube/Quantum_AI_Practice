namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// Make a Scriptable Quantum Config via "Create ▸ Quantum ▸ Config ▸ Minion Wave Config".
    /// Assign this in RuntimeConfig (Project: Quantum -> RuntimeConfig asset).
    /// </summary>
    [System.Serializable]
    public class MinionWaveConfig
    {
        public int Count = 5;
        public FPVector3 SpawnPos = new FPVector3(0, 0, 0);
        public FPVector3 TargetPos = new FPVector3(12, 0, 0);
        public string NavmeshName = "Navmesh";
        public AssetRef<NavMeshAgentConfig> AgentConfig;
        public FP DefaultStoppingDistance = FP._1 / 2;
        public FP WaitAtEndpointsSeconds = FP._0; // <--- add this (0 = no wait)
    }

    // Extend your RuntimeConfig (Quantum auto-generates partial class).
    public partial class RuntimeConfig
    {
        public MinionWaveConfig MinionWave;
        public AssetRef<EntityView> MinionView; // pick your Entity View asset here
    }
}
