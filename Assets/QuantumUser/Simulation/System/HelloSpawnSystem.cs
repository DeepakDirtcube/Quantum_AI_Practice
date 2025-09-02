// Assets/QuantumUser/Simulation/System/HelloSpawnSystem.cs
namespace Quantum
{
  using Photon.Deterministic;
  using UnityEngine.Scripting;

  [Preserve]
  public unsafe class HelloSpawnSystem : SystemMainThread
  {
    public override void OnInit(Frame f)
    {
      var e = f.Create();
      f.Set(e, new Transform3D { Position = FPVector3.Zero, Rotation = FPQuaternion.Identity });
      f.Set(e, new Minion
      {
        SpawnPos = FPVector3.Zero,
        TargetPos = new FPVector3(3, 0, 0),
        State = MinionState.GoingToTarget,
        StoppingDistance = FP._1 / 2,
        WaitTimer = FP._0
      });
      // If we get here without exceptions, Minion is registered & buffer allocated ✅
    }
    public override void Update(Frame f)
    {
    }
  }
}
