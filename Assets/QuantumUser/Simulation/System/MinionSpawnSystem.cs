namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    [Preserve]
    public unsafe class MinionSpawnSystem : SystemMainThread
    {
        public override void OnInit(Frame frame)
        {
            var cfg = frame.RuntimeConfig.MinionWave;
            if (cfg == null || cfg.Count <= 0) return;

            for (int i = 0; i < cfg.Count; i++)
            {
                var e = frame.Create();
                // Small offsets so they don’t all overlap exactly at spawn
                FP xOff = (FP)(i % 4) - FP._1;   // -1..2
                FP zOff = (FP)(i / 4);           // 0,1,2,...

                // Transform
                frame.Set(e, new Transform3D
                {
                    Position = cfg.SpawnPos + new FPVector3(xOff, 0, zOff),
                    Rotation = FPQuaternion.Identity
                });


                // Logic Component
                frame.Set(e, new Minion
                {
                    SpawnPos = cfg.SpawnPos,
                    TargetPos = cfg.TargetPos,
                    State = MinionState.GoingToTarget,
                    StoppingDistance = cfg.DefaultStoppingDistance,
                    WaitTimer = 0
                });

                // ✅ THIS is where you set the View component using View.Create(...)
                if (frame.RuntimeConfig.MinionView.Id.IsValid)
                {
                    var entityViewAsset = frame.FindAsset<EntityView>(frame.RuntimeConfig.MinionView);
                    if (entityViewAsset != null)
                    {
                        frame.Set(e, View.Create(entityViewAsset));
                    }
                }

                // // Optional: Setup navigation, etc.
                // var pf = NavMeshPathfinder.Create(frame, e, null);
                // if (cfg.AgentConfig.Id.IsValid)
                // {
                //     pf.SetConfig(frame, e, cfg.AgentConfig);
                // }

                // if (frame.Map.NavMeshes.ContainsKey(cfg.NavmeshName))
                // {
                //     pf.SetTarget(frame, cfg.TargetPos, frame.Map.NavMeshes[cfg.NavmeshName]);
                // }

                // frame.Set(e, pf);
                // frame.Set(e, new NavMeshSteeringAgent());
            }
        }

        public override void Update(Frame f)
        {
        }

    }
}
