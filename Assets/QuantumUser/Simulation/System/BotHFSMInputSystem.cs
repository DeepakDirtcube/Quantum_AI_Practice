// Assets/QuantumUser/Simulation/System/BotHFSMInputSystem.cs
using Photon.Deterministic;
using Quantum.BotSDK;  // AIBlackboardComponent, AIBlackboardValueKey
using UnityEngine.Scripting;

namespace Quantum
{
    /// Reads Bot SDK HFSM Blackboard outputs and writes Input for that PlayerRef.
    /// Place BEFORE PlayerSystem and WeaponsSystem in System Setup.
    [Preserve]
    public unsafe class BotHFSMInputSystem
      : SystemMainThreadFilter<BotHFSMInputSystem.Filter>
    {

        // Blackboard keys (assign in System Setup to match your HFSM/Blackboard)
        public AIBlackboardValueKey TargetPosKey;   // FPVector3
        public AIBlackboardValueKey MoveDirKey;     // FPVector3 (world dir)
                                                    // public AIBlackboardValueKey WantFireKey; // Int (0/1)  <-- temporarily unused to avoid button write
        public AIBlackboardValueKey WeaponSlotKey;  // Int (optional, 1-based)

        // Tuning
        public FP MaxYawPerTick = FP.FromFloat_UNSAFE(6f);
        public FP MaxPitchPerTick = FP.FromFloat_UNSAFE(4f);
        public FP StopBand = FP.FromFloat_UNSAFE(1.25f);

        public override void Update(Frame f, ref Filter b)
        {
            if (b.Player->PlayerRef.IsValid == false || b.Health->IsAlive == false)
                return;

            var bb = b.Blackboard;

            // --- Read from HFSM Blackboard (guard keys that might be empty) ---
            FPVector3 targetPos = FPVector3.Zero;
            if (!string.IsNullOrEmpty(TargetPosKey.Key))
            {
                targetPos = bb->GetVector3(f, TargetPosKey.Key);
            }

            FPVector3 moveWorld = FPVector3.Zero;
            if (!string.IsNullOrEmpty(MoveDirKey.Key))
            {
                moveWorld = bb->GetVector3(f, MoveDirKey.Key);
            }

            // --- Build player input (same path as humans) ---
            var input = f.GetPlayerInput(b.Player->PlayerRef);

            // 1) MOVE: world -> local (relative to current look), with stop band
            var to = targetPos - b.Transform->Position;
            var dist = to.Magnitude;
            if (dist <= StopBand) moveWorld = FPVector3.Zero;

            moveWorld.Y = FP._0;
            if (moveWorld != FPVector3.Zero) moveWorld = moveWorld.Normalized;

            FPVector3 forward = b.KCC->Data.LookDirection;
            FPVector3 right = FPVector3.Cross(new FPVector3(FP._0, FP._1, FP._0), forward).Normalized;

            FP moveX = FPVector3.Dot(moveWorld, right);
            FP moveY = FPVector3.Dot(moveWorld, forward);
            input->MoveDirection = new FPVector2(moveX, moveY);

            // 2) AIM: yaw/pitch delta toward target
            FPVector3 eye = b.KCC->Data.TargetPosition + new FPVector3(FP._0, FP._1, FP._0); // ~1m eye height
            FPVector3 dir = targetPos - eye;
            if (dir != FPVector3.Zero) dir = dir.Normalized;

            var desired = FPQuaternion.LookRotation(dir).AsEuler;
            var current = b.KCC->Data.LookRotation.AsEuler;

            FP dYaw = NormalizeAngle(desired.Y - current.Y);
            FP dPitch = NormalizeAngle(desired.X - current.X);

            dYaw = FPMath.Clamp(dYaw, -MaxYawPerTick, MaxYawPerTick);
            dPitch = FPMath.Clamp(dPitch, -MaxPitchPerTick, MaxPitchPerTick);

            input->LookRotationDelta = new FPVector2(dPitch, dYaw);

            // 3) WEAPON slot (optional)
            if (!string.IsNullOrEmpty(WeaponSlotKey.Key))
            {
                int slot = 0;
                try { slot = bb->GetInteger(f, WeaponSlotKey.Key); } catch { slot = 0; }
                if (slot > 0) input->Weapon = (byte)slot;
            }

            // 4) FIRE — skipped for now (Button has read-only props on this SDK).
            // Once you share your generated Button API or a sample of how you set buttons elsewhere,
            // I’ll wire this to set Fire correctly without compile errors.
        }

        static FP NormalizeAngle(FP a)
        {
            FP p180 = FP.FromFloat_UNSAFE(180f);
            FP p360 = FP.FromFloat_UNSAFE(360f);
            while (a > p180) a -= p360;
            while (a <= -p180) a += p360;
            return a;
        }

        public struct Filter
        {
            public EntityRef Entity;
            public Player* Player;
            public Health* Health;
            public KCC* KCC;
            public Transform3D* Transform;
            public AIBlackboardComponent* Blackboard;
        }
    }
}
