// Assets/QuantumUser/Simulation/System/BotBrainSystem.cs
using Photon.Deterministic;
using Quantum.BotSDK;
using UnityEngine.Scripting;

namespace Quantum
{
    /// Moves any entity tagged with BotTag straight toward the nearest alive human.
    /// Step 1: no aiming/shooting; we drive KCC directly (bot PlayerRef can stay invalid).
    [Preserve]
    public unsafe class BotBrainSystem : SystemMainThreadFilter<BotBrainSystem.Filter>
    {

        // BotBrainSystem.cs (replace Update with this version)
        public override void Update(Frame frame, ref Filter bot)
        {
            if (bot.Health->IsAlive == false)
                return;

            // Find nearest alive human (valid PlayerRef)
            bool found = false;
            FP bestD = FP._0;
            FPVector3 bestPos = bot.Transform->Position;

            var it = frame.Filter<Player, Health, Transform3D>();
            while (it.Next(out EntityRef otherEnt, out Player otherPlayer, out Health otherHealth, out Transform3D otherTr))
            {
                if (otherEnt == bot.Entity) continue;
                if (otherPlayer.PlayerRef.IsValid == false) continue; // humans only
                if (otherHealth.IsAlive == false) continue;

                FPVector3 myPos = bot.Transform->Position;
                FPVector3 enPos = otherTr.Position;
                FP d = FPVector3.Distance(myPos, enPos);

                if (!found) { found = true; bestD = d; bestPos = enPos; }
                else if (d < bestD) { bestD = d; bestPos = enPos; }
            }

            // Planar steering
            FPVector3 dir = FPVector3.Zero;
            if (found)
            {
                dir = bestPos - bot.Transform->Position;
                dir.Y = FP._0;
                FP len = dir.Magnitude;
                if (len > FP._0) dir /= len;
            }

            // IMPORTANT: give the bot a speed (humans get this in PlayerSystem)
            bot.KCC->SetKinematicSpeed(bot.Player->MoveSpeed); // mirrors PlayerSystem’s behavior
            bot.KCC->SetInputDirection(dir);

            if (frame.Has<HFSMAgent>(bot.Entity) == true)
            {
                HFSMManager.Update(frame, frame.DeltaTime, bot.Entity);
            }
        }

        public struct Filter
        {
            public EntityRef Entity;
            public BotTag* BotTag;
            public Health* Health;
            public KCC* KCC;
            public Player* Player;
            public Transform3D* Transform;  // <-- add this line to the filter
        }

    }
}
