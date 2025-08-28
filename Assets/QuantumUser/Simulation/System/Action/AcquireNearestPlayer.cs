// Assets/QuantumUser/Simulation/System/Action/AcquireNearestPlayer.cs
using System;
using Photon.Deterministic;
using Quantum.BotSDK;

namespace Quantum
{
    [Serializable]
    public unsafe partial class AcquireNearestPlayer : AIAction
    {
        public AIBlackboardValueKey TargetEntityKey; // EntityRef
        public AIBlackboardValueKey TargetPosKey;    // Vector3

        public override void Execute(Frame f, EntityRef self, ref AIContext ctx)
        {
            AIBlackboardComponent* bb = f.Unsafe.GetPointer<AIBlackboardComponent>(self);

            FP best = FP.FromFloat_UNSAFE(99999f);
            EntityRef bestEnt = default;
            FPVector3 bestPos = FPVector3.Zero;

            FPVector3 myPos = f.Get<Transform3D>(self).Position;

            var it = f.Filter<Player, Health, Transform3D>();
            while (it.Next(out var e, out Player p, out Health h, out Transform3D tr))
            {
                if (e == self) continue;
                if (p.PlayerRef.IsValid == false) continue;
                if (h.IsAlive == false) continue;

                FP d = FPVector3.Distance(myPos, tr.Position);
                if (d < best) { best = d; bestEnt = e; bestPos = tr.Position; }
            }

            if (bestEnt.IsValid)
            {
                bb->Set(f, TargetEntityKey.Key, bestEnt);
                bb->Set(f, TargetPosKey.Key, bestPos);
            }
            else
            {
                bb->Set(f, TargetEntityKey.Key, EntityRef.None);
            }
        }
    }
}
