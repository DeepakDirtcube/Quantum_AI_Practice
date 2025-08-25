using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// EarlyLagCompensationSystem creates proxies which are destroyed later in LateLagCompensationSystem.
	/// This system must run before physics system.
	///
	/// How it works?
	/// =============
	/// As a local player, you see movement of all other player entity views being snapshot interpolated (not limited to players).
	/// But in simulation, other players are predicted.
	/// This system takes interpolation offset (in ticks) and interpolation alpha - both sent via input
	/// and creates a proxy entity (with LagCompensationProxy component) for each entity marked to be lag compensated LagCompensationTarget.
	/// These proxy entities represent state which player saw at the time of firing from a gun.
	///
	/// Because every player has different interpolation, colliders of proxies must have unique layer per player. Check LagCompensationUtility for more details.
	/// All proxies for PlayerRef:0 have layer 16,
	/// All proxies for PlayerRef:1 have layer 17,
	/// All proxies for PlayerRef:2 have layer 18, and so on
	/// When making lag compensation casts, you must use layer mask based on PlayerRef to hit correct proxies.
	/// Use methods from LagCompensationUtility.
	/// </summary>
	[Preserve]
	public unsafe class EarlyLagCompensationSystem : SystemMainThread
	{
		public override void Update(Frame frame)
		{
			// Create proxies for every PlayerRef.
			for (int i = 0; i < frame.PlayerCount; ++i)
			{
				PlayerRef playerRef = i;

				// Skip if the player is not connected.
				var inputFlags = frame.GetPlayerInputFlags(playerRef);
				if ((inputFlags & DeterministicInputFlags.PlayerNotPresent) == DeterministicInputFlags.PlayerNotPresent)
					continue;

				// Skip if the player is not shooting.
				var input = frame.GetPlayerInput(playerRef);
				if (input->Fire == false)
					continue;

				// Get proxy collider layer locked to specific PlayerRef.
				int colliderLayer = LagCompensationUtility.GetProxyColliderLayer(playerRef);

				// Iterate over all entities which should be part of lag compensated casts.
				var targets = frame.Filter<LagCompensationTarget>();
				targets.UseCulling = false;
				while (targets.NextUnsafe(out EntityRef targetEntity, out LagCompensationTarget* target))
				{
					// Don't create a proxy for self. The local player is never interpolated.
					if (frame.Unsafe.TryGetPointer(targetEntity, out Player* targetPlayer) && targetPlayer->PlayerRef == playerRef)
						continue;

					// Don't create a proxy for dead entities.
					if (frame.Unsafe.TryGetPointer(targetEntity, out Health* targetHealth) && targetHealth->IsAlive == false)
						continue;

					var proxyEntity = frame.Create(target->ProxyPrototype);
					frame.SetCullable(proxyEntity, false);

					// We are shooting against proxy, but we'll need a reference to origin entity.
					var proxy = frame.Unsafe.GetPointer<LagCompensationProxy>(proxyEntity);
					proxy->Target = targetEntity;

					var collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(proxyEntity);
					collider->Layer = colliderLayer;

					// Set proxy transform based on player interpolation data (passed via input).
					var transform = frame.Unsafe.GetPointer<Transform3D>(proxyEntity);
					*transform = target->GetInterpolatedTransform(input->InterpolationOffset, input->InterpolationAlpha);
				}
			}
		}
	}
}

