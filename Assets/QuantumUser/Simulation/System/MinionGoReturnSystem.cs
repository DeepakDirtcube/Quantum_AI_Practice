namespace Quantum {
  using Photon.Deterministic;

  /// <summary>
  /// Flips target when minion is within stopping distance of goal.
  /// </summary>
  public unsafe class MinionGoReturnSystem : SystemMainThreadFilter<MinionGoReturnSystem.Filter> {
    public struct Filter {
      public EntityRef Entity;
      public Minion* Minion;
      public Transform3D* Transform;
      public NavMeshPathfinder* Pathfinder;
    }

    public override void Update(Frame frame, ref Filter f) {
      var cfg = frame.RuntimeConfig.MinionWave;
      if (cfg == null) return;

      // Determine current goal
      FPVector3 goal = (f.Minion->State == MinionState.GoingToTarget)
        ? f.Minion->TargetPos
        : f.Minion->SpawnPos;

      // Distance check (deterministic)
      FP dist = FPVector3.Distance(f.Transform->Position, goal);
      FP stop = f.Minion->StoppingDistance > FP._0 ? f.Minion->StoppingDistance : cfg.DefaultStoppingDistance;

      if (dist <= stop) {
        // Reached current goal → flip state and retarget
        f.Minion->State = (f.Minion->State == MinionState.GoingToTarget)
          ? MinionState.ReturningToSpawn
          : MinionState.GoingToTarget;

        var navmesh = frame.Map.NavMeshes[cfg.NavmeshName];
        var newGoal = (f.Minion->State == MinionState.GoingToTarget) ? f.Minion->TargetPos : f.Minion->SpawnPos;
        f.Pathfinder->SetTarget(frame, newGoal, navmesh);
      }
    }
  }
}
