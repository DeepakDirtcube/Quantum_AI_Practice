using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Pickup system maintains level pickups (health/weapons) - collectiong based on collision with players, their cooldowns.
	/// </summary>
	[Preserve]
	public unsafe class PickupSystem : SystemMainThreadFilter<PickupSystem.Filter>, ISignalOnTrigger3D
	{
		public override void Update(Frame frame, ref Filter filter)
		{
			var pickup = filter.Pickup;
			pickup->Cooldown -= frame.DeltaTime;

			if (pickup->Cooldown > 0)
				return;

			pickup->Cooldown = 0;
			filter.Collider->Enabled = true;
		}

		public void OnTrigger3D(Frame frame, TriggerInfo3D info)
		{
			if (frame.Unsafe.TryGetPointer<Pickup>(info.Entity, out var pickup) == false)
				return;

			if (TryCollectPickup(frame, pickup, info.Other) == false)
				return;

			pickup->Cooldown = pickup->PickupCooldown;

			// Disable trigger
			var collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(info.Entity);
			collider->Enabled = false;
		}

		private bool TryCollectPickup(Frame frame, Pickup* pickup, EntityRef otherEntity)
		{
			if (pickup->Settings.Field == PickupSettings.HEALTH)
			{
				frame.Unsafe.TryGetPointer<Health>(otherEntity, out var health);
				return health != null && health->AddHealth(pickup->Settings.Health->Heal);
			}
			else if (pickup->Settings.Field == PickupSettings.WEAPON)
			{
				if (frame.Unsafe.TryGetPointer<Weapons>(otherEntity, out var weapons))
				{
					byte weaponId = pickup->Settings.Weapon->WeaponID;
					var weapon = frame.Unsafe.GetPointer<Weapon>(weapons->WeaponRefs[weaponId]);

					if (weapon->CollectOrRefill(pickup->Settings.Weapon->RefillAmmo))
					{
						frame.Signals.SwitchWeapon(otherEntity, pickup->Settings.Weapon->WeaponID);
						return true;
					}
				}
			}

			return false;
		}

		public struct Filter
		{
			public EntityRef          Entity;
			public Pickup*            Pickup;
			public PhysicsCollider3D* Collider;
		}
	}
}
