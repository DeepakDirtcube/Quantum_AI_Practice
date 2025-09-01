// Assets/QuantumUser/Simulation/Components/Minion.cs
namespace Quantum {
  using Photon.Deterministic;

  public enum MinionState : byte {
    GoingToTarget = 0,
    ReturningToSpawn = 1,
  }

  [System.Serializable]
  public unsafe struct Minion : IComponent {
    public FPVector3 SpawnPos;
    public FPVector3 TargetPos;
    public MinionState State;
    public FP StoppingDistance;
  }
}
