namespace Quantum
{
    using Photon.Deterministic;
    using Quantum.BotSDK;
    using System.Collections.Generic;
    using System.Diagnostics;

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
            HarvesterHelper.SetupAsBot(frame, e);
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var cfg = frame.RuntimeConfig.MinionWave;
            if (cfg == null) return;

            var spawner = filter.Spawner;

            // Tick timer
            spawner->Timer += frame.DeltaTime;

            // First wave uses initial delay (0 => instant)
            if (spawner->CurrentWave == 0)
            {
                FP firstDelay = cfg.InitialSpawnWaitSeconds; // or spawner->InitialSpawnWaitSeconds
                if (spawner->Timer >= firstDelay)
                {
                    SpawnWave(frame, cfg, 0);
                    spawner->CurrentWave = 1;
                    spawner->Timer = FP._0;

                    if (cfg.MaxWaves == 1)
                    {
                        frame.Destroy(filter.Entity);
                        return;
                    }
                }
            }
            else
            {
                // Subsequent waves use the normal interval
                if (spawner->Timer >= cfg.WaveIntervalSeconds)
                {
                    SpawnWave(frame, cfg, spawner->CurrentWave);

                    spawner->CurrentWave += 1;
                    spawner->Timer = FP._0;

                    if (cfg.MaxWaves > 0 && spawner->CurrentWave >= cfg.MaxWaves)
                    {
                        frame.Destroy(filter.Entity);
                        return;
                    }
                }
            }

            // ✅ Tick HFSM for all spawned minions
            for (int i = _spawnedMinions.Count - 1; i >= 0; i--)
            {
                var (entity, _) = _spawnedMinions[i];

                if (!frame.Exists(entity))
                {
                    _spawnedMinions.RemoveAt(i);
                    continue;
                }

                if (frame.Has<HFSMAgent>(entity))
                {
                    HFSMManager.Update(frame, frame.DeltaTime, entity);
                }
            }

            // ✅ Optional: check despawn
            CheckDespawn(frame);
        }


        /// <summary>
        /// CHANGED: Now uses Entity Prototype instead of manual entity creation
        /// </summary>
        private void SpawnWave(Frame frame, MinionWaveConfig cfg, int waveIndex)
        {
            // ✅ NEW: Validate that we have a prototype reference
            if (!cfg.MinionPrototype.Id.IsValid)
            {
                Log.Error("MinionWaveConfig.MinionPrototype is not set! Please assign a QuantumEntityPrototype asset.");
                return;
            }

            // // ✅ NEW: Get the Entity Prototype asset
            // var prototypeAsset = frame.FindAsset<EntityPrototype>(cfg.MinionPrototype);
            // if (prototypeAsset == null)
            // {
            //     Log.Error($"Could not find Entity Prototype asset with ID: {cfg.MinionPrototype.Id}");
            //     return;
            // }

            for (int i = 0; i < cfg.Count; i++)
            {
                // ✅ NEW: Spawn from prototype instead of manual creation
                var spawnOffset = new FPVector3((i % 4) * FP._1, 0, (i / 4) * FP._1);
                var position = cfg.SpawnPos + spawnOffset;
                var rotation = FPQuaternion.Identity;


                // This creates the entity with ALL components from the prototype
                var entity = frame.Create(cfg.MinionPrototype);
                if (frame.TryGet<HFSMAgent>(entity, out var hfsmAgent) == true)
                {
                    BotSDKDebuggerSystem.AddToDebugger(frame, entity, hfsmAgent);

                }
                if (frame.Has<AIBlackboardComponent>(entity))
                {
                    var blackboardAsset = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
                    if (blackboardAsset != null)
                    {
                        // SetBlackboardValues(frame, entity, blackboardAsset);
                        Log.Info("22222222222222___");

                        blackboardAsset->Set(frame, "Destination", cfg.TargetPos);
                        blackboardAsset->Set(frame, "HarvestEnabled", false);
                        blackboardAsset->Set(frame, "HarvestRate", cfg.HarvestRate);
                        // blackboardAsset.
                    }
                }

                // ✅ NEW: Override/customize specific component values after spawning
                if (frame.Has<Minion>(entity))
                {
                    var minion = frame.Unsafe.GetPointer<Minion>(entity);
                    minion->SpawnPos = cfg.SpawnPos;
                    minion->TargetPos = cfg.TargetPos;
                    minion->State = MinionState.GoingToTarget;
                    minion->StoppingDistance = cfg.DefaultStoppingDistance;
                    minion->WaitTimer = FP._0;
                    minion->LifeAfterReturnSeconds = cfg.DefaultLifeAfterReturnSeconds;
                }

                // ✅ NEW: Override transform position if needed (prototype might have different position)
                if (frame.Has<Transform3D>(entity))
                {
                    var transform = frame.Unsafe.GetPointer<Transform3D>(entity);
                    transform->Position = position;
                    transform->Rotation = rotation;
                }

                // ✅ Add MinionEnergy component
                frame.Set(entity, new HarvesterEnergy
                {
                    UseTimeMode = cfg.IsHarvestTimeBased, // default to time-based (seconds-to-full)
                    HarvestParam = cfg.HarvestRate, // reuse config field as param
                    CurrentEnergy = 0,
                    MaxEnergy = FPMath.RoundToInt(cfg.MaxHarvest) // FP -> int cap
                });


                // ✅ The View component is already set from the prototype!
                // No need to manually set it - the prototype handles this

                _spawnedMinions.Add((entity, FP._0));

                // Log.Debug($"Spawned minion {entity} at wave {waveIndex}, position {i}");
            }

            Log.Info($"Wave {waveIndex} spawned: {cfg.Count} minions");
        }



        // /// <summary>
        // /// ✅ FIX: Initialize blackboard if it's not properly set up
        // /// </summary>
        // private void InitializeBlackboardIfNeeded(Frame frame, AIBlackboardComponent* blackboard)
        // {
        //     try
        //     {
        //         // Try to check if the blackboard is initialized by attempting a safe operation
        //         blackboard->Has(frame, "TestKey");
        //     }
        //     catch (System.Exception)
        //     {
        //         // If it throws an exception, the blackboard needs initialization
        //         Log.Debug("Initializing AIBlackboardComponent");

        //         // Force initialize the internal collections
        //         // This is typically done automatically but may need manual trigger
        //         blackboard->Initialize(frame);
        //     }
        // }
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
