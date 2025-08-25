using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Player system processes player input and propagates actions to character controller (Quantum KCC).
	/// </summary>
	[Preserve]
	public unsafe class PlayerSystem : SystemMainThreadFilter<PlayerSystem.Filter>
	{
		public override void Update(Frame frame, ref Filter filter)
		{
			var player = filter.Player;
			if (player->PlayerRef.IsValid == false)
				return;

			var kcc = filter.KCC;

			var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
			if (gameplay->State == EGameplayState.Finished)
			{
				kcc->SetInputDirection(FPVector3.Zero);
			}

			var input = frame.GetPlayerInput(player->PlayerRef);

			if (filter.Health->IsAlive)
			{
				kcc->AddLookRotation(input->LookRotationDelta.X, input->LookRotationDelta.Y);
				kcc->SetInputDirection(kcc->Data.TransformRotation * input->MoveDirection.XOY);
				kcc->SetKinematicSpeed(player->MoveSpeed);

				if (input->Jump.WasPressed && kcc->IsGrounded)
				{
					kcc->Jump(FPVector3.Up * player->JumpForce);
				}
			}
			else
			{
				kcc->SetInputDirection(FPVector3.Zero);
			}
		}

		public struct Filter
		{
			public EntityRef Entity;
			public Player*   Player;
			public Health*   Health;
			public KCC*      KCC;
		}
	}
}
