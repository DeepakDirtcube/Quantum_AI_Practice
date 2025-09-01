// Assets/QuantumUser/Simulation/System/MinionSpawnSystem.cs
namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    /// <summary>
    /// Spawns N minions at SpawnPos, assigns navigation to TargetPos,
    /// and attaches an Entity View (if provided in RuntimeConfig).
    /// </summary>
    [Preserve]
    public unsafe class MinionSpawnSystem : SystemMainThread
    {
        public override void OnInit(Frame frame)
        {
            // Read inline (Option A) wave config from RuntimeConfig
            var cfg = frame.RuntimeConfig.MinionWave;
            if (cfg == null || cfg.Count <= 0)
            {
                return;
            }

            // Resolve the baked navmesh by key
            if (!frame.Map.NavMeshes.ContainsKey(cfg.NavmeshName))
            {
                // NavMesh not found under that name — ensure QuantumMapData Bake All and key matches
                return;
            }
            var navmesh = frame.Map.NavMeshes[cfg.NavmeshName];

            // Optional Entity View (visual prefab) for minions
            AssetRef<EntityView> minionViewRef = default;
            if (frame.RuntimeConfig.MinionView.Id.IsValid)
            {
                minionViewRef = frame.RuntimeConfig.MinionView;
            }

            for (int i = 0; i < cfg.Count; i++)
            {
                // Small offsets so they don’t all overlap exactly at spawn
                FP xOff = (FP)(i % 4) - FP._1;   // -1..2
                FP zOff = (FP)(i / 4);           // 0,1,2,...

                var e = frame.Create();

                // Transform
                frame.Set(e, new Transform3D
                {
                    Position = cfg.SpawnPos + new FPVector3(xOff, 0, zOff),
                    Rotation = FPQuaternion.Identity
                });

                // Simulation data
                frame.Set(e, new Minion
                {
                    SpawnPos = cfg.SpawnPos,
                    TargetPos = cfg.TargetPos,
                    State = MinionState.GoingToTarget,
                    StoppingDistance = (cfg.DefaultStoppingDistance > FP._0) ? cfg.DefaultStoppingDistance : FP._1 / 2
                });

                // Attach a View so EVU can spawn a visible prefab
                if (minionViewRef.Id.IsValid)
                {
                    frame.Set(e, new View
                    {
                        Current = minionViewRef // <-- correct field name in Quantum 3
                    });
                }

                // Create Pathfinder + Steering
                var pf = NavMeshPathfinder.Create(frame, e, null);

                // Apply NavMeshAgentConfig via SetConfig (AssetRef<NavMeshAgentConfig>)
                if (cfg.AgentConfig.Id.IsValid)
                {
                    pf.SetConfig(frame, e, cfg.AgentConfig);
                }

                // First leg: go to target
                pf.SetTarget(frame, cfg.TargetPos, navmesh);

                // Commit components
                frame.Set(e, pf);
                frame.Set(e, new NavMeshSteeringAgent());
            }
        }

        public override void Update(Frame f)
        {
        }

    }
}
