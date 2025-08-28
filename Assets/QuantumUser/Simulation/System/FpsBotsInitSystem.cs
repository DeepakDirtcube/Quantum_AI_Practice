// Assets/QuantumUser/Simulation/System/FpsBotsInitSystem.cs
using UnityEngine.Scripting;

namespace Quantum
{
    /// Connects bot PlayerRefs (those with RuntimeConfig avatar overrides) as real players.
    [Preserve]
    public unsafe class FpsBotsInitSystem : SystemMainThread
    {
        private bool _done;

        public override void Update(Frame frame)
        {
            if (_done) return;

            var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
            if (gameplay == null) return;

            var cfg = frame.RuntimeConfig.CollectorsSampleConfig;
            var overrides = cfg.Bots;
            if (overrides == null || overrides.Length == 0) return;

            // Resolve Gameplay’s player dictionary once
            var players = frame.ResolveDictionary(gameplay->PlayerData);

            // Iterate player slots (PlayerRef is 1-based) and connect those
            // whose override prefab is set (valid) in RuntimeConfig.
            int maxSlots = frame.PlayerCount;
            int max = overrides.Length < maxSlots ? overrides.Length : maxSlots;

            for (int i = 0; i < max; i++)
            {
                if (overrides[i].IsValid == false)
                    continue; // this slot is a human (no override)

                PlayerRef slot = (PlayerRef)(i + 1);

                // If already connected, skip
                if (players.TryGetValue(slot, out PlayerData pd) && pd.IsConnected)
                    continue;

                // Connect → Gameplay.ConnectPlayer() will spawn avatar via RespawnPlayer
                gameplay->ConnectPlayer(frame, slot);
                Log.Info($"[BotsInit] Connected bot PlayerRef={slot}");
            }

            // One-shot: after all connect attempts, stop running
            _done = true;
        }
    }
}
