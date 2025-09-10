namespace Quantum
{
    using Photon.Deterministic;

    // Initializes team spawn-manager entities from RuntimeConfig at game start.
    public unsafe class SpawnManagerInitSystem : SystemMainThread
    {
        public override void OnInit(Frame frame)
        {
            var rc = frame.RuntimeConfig;

            void CreateSpawnerIfValid(AssetRef<EntityPrototype> protoRef)
            {
                if (!protoRef.Id.IsValid)
                    return;

                var e = frame.Create(protoRef);
                Log.Info($"[SpawnManagerInit] Created spawn-manager {e} from prototype {protoRef.Id.Value}");

                bool hasSettings = frame.Unsafe.TryGetPointer<MinionWaveSettings>(e, out var settings);
                bool hasSpawner = frame.Unsafe.TryGetPointer<MinionWaveSpawner>(e, out var spawner);

                if (!hasSpawner)
                {
                    frame.Set(e, new MinionWaveSpawner { CurrentWave = 0, Timer = FP._0 });
                    hasSpawner = frame.Unsafe.TryGetPointer<MinionWaveSpawner>(e, out spawner);
                }

                if (hasSpawner)
                {
                    // Ensure SpawnedMinions list is allocated so systems can use it immediately
                    frame.TryAllocateList(ref spawner->SpawnedMinions);
                    spawner->CurrentWave = 0;
                    spawner->Timer = hasSettings ? -settings->InitialDelaySeconds : FP._0;
                }

                if (!hasSettings)
                {
                    Log.Error($"[SpawnManagerInit] Spawn-manager {e} is missing MinionWaveSettings component.");
                }
            }

            CreateSpawnerIfValid(rc.Team1MinionSpawner);
            // CreateSpawnerIfValid(rc.TeamBMinionSpawner);
        }

        public override void Update(Frame f)
        {
        }

    }
}
