// namespace Quantum
// {
//   using Photon.Deterministic;

//   public unsafe class MinionGoReturnSystem : SystemMainThreadFilter<MinionGoReturnSystem.Filter>
//   {
//     public struct Filter
//     {
//       public EntityRef Entity;
//       public Minion* Minion;
//       public Transform3D* Transform;
//       public NavMeshPathfinder* Pathfinder;
//     }

//     public override void Update(Frame frame, ref Filter f)
//     {
//       var cfg = frame.RuntimeConfig.MinionWave;
//       if (cfg == null) return;

//       // Handle waiting states first
//       if (f.Minion->State == MinionState.WaitingAtTarget || f.Minion->State == MinionState.WaitingAtSpawn)
//       {
//         if (f.Minion->WaitTimer > FP._0)
//         {
//           f.Minion->WaitTimer -= frame.DeltaTime;
//           return;
//         }

//         // Wait finished -> flip to moving state and retarget
//         var toTarget = (f.Minion->State == MinionState.WaitingAtSpawn);
//         f.Minion->State = toTarget ? MinionState.GoingToTarget : MinionState.ReturningToSpawn;

//         var navmesh = frame.Map.NavMeshes[cfg.NavmeshName];
//         var newGoal = toTarget ? f.Minion->TargetPos : f.Minion->SpawnPos;
//         f.Pathfinder->SetTarget(frame, newGoal, navmesh);
//         return;
//       }

//       // Moving states: check arrival
//       FPVector3 goal = (f.Minion->State == MinionState.GoingToTarget) ? f.Minion->TargetPos : f.Minion->SpawnPos;
//       FP stop = f.Minion->StoppingDistance > FP._0 ? f.Minion->StoppingDistance : cfg.DefaultStoppingDistance;
//       FP dist = FPVector3.Distance(f.Transform->Position, goal);

//       if (dist > stop) return;

//       // Arrived -> either wait or immediately flip
//       var wait = cfg.WaitAtEndpointsSeconds;
//       if (f.Minion->State == MinionState.GoingToTarget)
//       {
//         f.Minion->State = (wait > FP._0) ? MinionState.WaitingAtTarget : MinionState.ReturningToSpawn;
//       }
//       else
//       { // ReturningToSpawn
//         f.Minion->State = (wait > FP._0) ? MinionState.WaitingAtSpawn : MinionState.GoingToTarget;
//       }

//       if (f.Minion->State == MinionState.WaitingAtTarget || f.Minion->State == MinionState.WaitingAtSpawn)
//       {
//         f.Minion->WaitTimer = wait;
//       }
//       else
//       {
//         // immediate flip — set the next leg
//         var navmesh = frame.Map.NavMeshes[cfg.NavmeshName];
//         var newGoal = (f.Minion->State == MinionState.GoingToTarget) ? f.Minion->TargetPos : f.Minion->SpawnPos;
//         f.Pathfinder->SetTarget(frame, newGoal, navmesh);
//       }
//     }
//   }
// }
