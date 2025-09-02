namespace Quantum
{
    using Photon.Deterministic;
    using System.Collections.Generic;

    public unsafe class MinionWaveSpawnerSystem : SystemMainThreadFilter<MinionWaveSpawnerSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public MinionWaveSpawner* Spawner;
        }

        private List<(EntityRef entity, FP timer)> _spawnedMinions = new();

        public override void OnInit(Frame frame)
        {
            var cfg = frame.RuntimeConfig.MinionWave;
            if (cfg == null) return;

            var e = frame.Create();
            frame.Set(e, new MinionWaveSpawner
            {
                CurrentWave = 0,
                Timer = -cfg.InitialDelaySeconds
            });
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var cfg = frame.RuntimeConfig.MinionWave;
            if (cfg == null) return;

            var spawner = filter.Spawner;

            spawner->Timer += frame.DeltaTime;

            if (spawner->Timer >= cfg.WaveIntervalSeconds)
            {
                SpawnWave(frame, cfg, spawner->CurrentWave);
                spawner->CurrentWave++;
                spawner->Timer = FP._0;

                if (cfg.MaxWaves > 0 && spawner->CurrentWave >= cfg.MaxWaves)
                {
                    frame.Destroy(filter.Entity);
                }
            }

            CheckDespawn(frame);
        }

        private void SpawnWave(Frame frame, MinionWaveConfig cfg, int waveIndex)
        {
            for (int i = 0; i < cfg.Count; i++)
            {
                var e = frame.Create();

                var spawnOffset = new FPVector3((i % 4) * FP._1, 0, (i / 4) * FP._1);
                var position = cfg.SpawnPos + spawnOffset;

                frame.Set(e, new Transform3D
                {
                    Position = position,
                    Rotation = FPQuaternion.Identity
                });

                frame.Set(e, new Minion
                {
                    SpawnPos = cfg.SpawnPos,
                    TargetPos = cfg.TargetPos,
                    State = MinionState.GoingToTarget,
                    StoppingDistance = cfg.DefaultStoppingDistance,
                    WaitTimer = FP._0,
                    LifeAfterReturnSeconds = cfg.DefaultLifeAfterReturnSeconds
                });

                if (cfg.View.Id.IsValid)
                {
                    var view = frame.FindAsset<EntityView>(cfg.View);
                    if (view != null)
                    {
                        frame.Set(e, View.Create(view));
                    }
                }

                _spawnedMinions.Add((e, FP._0));
            }
        }

        private void CheckDespawn(Frame frame)
        {
            for (int i = _spawnedMinions.Count - 1; i >= 0; i--)
            {
                var (entity, timer) = _spawnedMinions[i];

                if (!frame.Exists(entity))
                {
                    _spawnedMinions.RemoveAt(i);
                    continue;
                }

                var minion = frame.Unsafe.GetPointer<Minion>(entity);
                if (minion->State == MinionState.GoingToTarget)
                {
                    timer += frame.DeltaTime;

                    if (timer >= minion->LifeAfterReturnSeconds)
                    {
                        frame.Destroy(entity); // ✅ This auto-returns the view to pool
                        _spawnedMinions.RemoveAt(i);
                        continue;
                    }

                    _spawnedMinions[i] = (entity, timer);
                }
            }
        }

    }
}
