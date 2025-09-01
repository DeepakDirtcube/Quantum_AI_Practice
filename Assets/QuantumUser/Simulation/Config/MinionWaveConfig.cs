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

        // Positions in world space (keep them near 0,0,0).
        public FPVector3 SpawnPos = new FPVector3(0, 0, 0);
        public FPVector3 TargetPos = new FPVector3(12, 0, 0);

        // Name of the baked Quantum navmesh (default is usually "Navmesh")
        public string NavmeshName = "Navmesh";

        // Steering/motion
        public AssetRef<NavMeshAgentConfig> AgentConfig;
        public FP DefaultStoppingDistance = FP._1 / 2; // 0.5 units
    }

    // Extend your RuntimeConfig (Quantum auto-generates partial class).
    public partial class RuntimeConfig
    {
        public MinionWaveConfig MinionWave;
            public AssetRef<EntityView> MinionView; // pick your Entity View asset here
    }
}
