// Assets/QuantumUser/Simulation/System/Decision/BBEntityValid.cs
using System;
using Quantum.BotSDK;

namespace Quantum
{

    [Serializable]
    public unsafe partial class BBEntityInvalid : HFSMDecision
    {
        public AIBlackboardValueKey EntityKey; // drag "TargetEntity" in the editor

        public override bool Decide(Frame f, EntityRef self, ref AIContext ctx)
        {
            var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(self);
            if (bb == null || string.IsNullOrEmpty(EntityKey.Key))
                return true;

            var e = bb->GetEntityRef(f, EntityKey.Key);
            return !e.IsValid;
        }
    }
}
