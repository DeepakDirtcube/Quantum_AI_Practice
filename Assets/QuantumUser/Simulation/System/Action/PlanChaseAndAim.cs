// Assets/QuantumUser/Simulation/System/Action/PlanChaseAndAim.cs
using System;
using Photon.Deterministic;
using Quantum.BotSDK;

namespace Quantum
{
    [Serializable]
    public unsafe partial class PlanChaseAndAim : AIAction
    {
        public AIBlackboardValueKey TargetEntityKey; // EntityRef
        public AIBlackboardValueKey TargetPosKey;    // Vector3 (updated with current pos or last known)
        public AIBlackboardValueKey MoveDirKey;      // Vector3 (world)
        public AIBlackboardValueKey WantFireKey;     // Int (0/1)

        public FP AttackRange = FP.FromFloat_UNSAFE(10f);
        public FP StopBand = FP.FromFloat_UNSAFE(1.25f);

        public override void Execute(Frame f, EntityRef self, ref AIContext ctx)
        {
            AIBlackboardComponent* bb = f.Unsafe.GetPointer<AIBlackboardComponent>(self);
            Transform3D tr = f.Get<Transform3D>(self);

            // read target from BB
            FPVector3 targetPos = bb->GetVector3(f, TargetPosKey.Key);
            EntityRef targetEnt = bb->GetEntityRef(f, TargetEntityKey.Key);

            // refresh target position from live entity if possible
            if (targetEnt.IsValid)
            {
                Transform3D* ttr;
                if (f.Unsafe.TryGetPointer<Transform3D>(targetEnt, out ttr))
                {
                    targetPos = ttr->Position;
                    bb->Set(f, TargetPosKey.Key, targetPos);
                }
            }

            // plan movement
            FPVector3 to = targetPos - tr.Position;
            to.Y = FP._0;
            FP dist = to.Magnitude;

            FPVector3 move = FPVector3.Zero;
            if (dist > StopBand)
            {
                if (dist > FP._0) move = to / dist;
            }

            bb->Set(f, MoveDirKey.Key, move);

            // simple fire gate: in range and valid target => 1, else 0
            int wantFire = (dist <= AttackRange && targetEnt.IsValid) ? 1 : 0;
            bb->Set(f, WantFireKey.Key, wantFire);

            Log.Info($"[Plan] self={self.Index} move=({move.X.AsFloat},{move.Y.AsFloat},{move.Z.AsFloat}) dist={(dist).AsFloat}");

        }
    }
}
