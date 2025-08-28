// Assets/QuantumUser/Simulation/System/Decision/AlwaysTrue.cs
using System;
using Quantum.BotSDK;

namespace Quantum
{
    [Serializable]
    public unsafe partial class AlwaysTrue : HFSMDecision
    {
        public override bool Decide(Frame f, EntityRef self, ref AIContext ctx) => true;
    }
}
