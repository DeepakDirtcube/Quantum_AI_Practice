using Photon.Deterministic;
using Quantum.Physics3D;
using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Weapons system maintains player weapons, switching, reloading, shooting.
	/// </summary>
	[Preserve]
	public unsafe class WeaponsSystem : SystemMainThreadFilter<WeaponsSystem.Filter>,
		ISignalOnComponentAdded<Weapons>, ISignalOnComponentRemoved<Weapons>,
		ISignalSwitchWeapon
	{
		private const ushort _headShapeUserTag = 1;
		private const ushort _limbShapeUserTag = 2;

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Health->IsAlive == false)
				return;
			if (filter.Player->PlayerRef.IsValid == false)
				return;

			var input = frame.GetPlayerInput(filter.Player->PlayerRef);
			var currentWeapon = frame.Unsafe.GetPointer<Weapon>(filter.Weapons->CurrentWeapon);

			UpdateWeaponSwitch(frame, ref filter);
			UpdateReload(frame, ref filter, currentWeapon);

			filter.Weapons->FireCooldown -= frame.DeltaTime;

			if (input->Weapon >= 1)
			{
				TryStartWeaponSwitch(frame, ref filter, (byte)(input->Weapon - 1));
			}

			if (input->Fire.IsDown)
			{
				TryFire(frame, ref filter, currentWeapon, input->Fire.WasPressed);

				// Cancel after-spawn immortality when player starts shooting.
				filter.Health->StopImmortality();
			}

			if (input->Reload.IsDown || currentWeapon->ClipAmmo <= 0)
			{
				TryStartReload(frame, ref filter, currentWeapon);
			}
		}

		private void TryStartWeaponSwitch(Frame frame, ref Filter filter, byte weaponId)
		{
			if (weaponId == filter.Weapons->PendingWeaponId)
				return;

			var weaponRef = filter.Weapons->WeaponRefs[weaponId];
			if (weaponRef.IsValid == false)
				return;

			var weapon = frame.Unsafe.GetPointer<Weapon>(weaponRef);
			if (weapon->IsCollected == false)
				return;

			filter.Weapons->PendingWeaponId = weaponId;
			filter.Weapons->SwitchCooldown = filter.Weapons->WeaponSwitchTime;

			// Stop reload.
			filter.Weapons->ReloadCooldown = 0;

			frame.Events.WeaponSwitchStarted(weaponId, filter.Entity);
		}

		private void UpdateWeaponSwitch(Frame frame, ref Filter filter)
		{
			filter.Weapons->SwitchCooldown -= frame.DeltaTime;

			// Switch already completed.
			if (filter.Weapons->PendingWeaponId == filter.Weapons->CurrentWeaponId)
				return;

			// Switching too quickly.
			if (filter.Weapons->SwitchCooldown > filter.Weapons->WeaponSwitchTime * FP._0_50)
				return;

			// In the middle of the switch we already switch the current weapon
			// but player won't be able to shoot until the switch cooldown expires.
			filter.Weapons->CurrentWeaponId = filter.Weapons->PendingWeaponId;
		}

		private void TryStartReload(Frame frame, ref Filter filter, Weapon* weapon)
		{
			if (filter.Weapons->IsBusy)
				return;
			if (weapon->CanReload() == false)
				return;

			filter.Weapons->ReloadCooldown = weapon->ReloadTime;

			frame.Events.WeaponReloadStarted(filter.Weapons->CurrentWeaponId, filter.Entity);
		}

		private void UpdateReload(Frame frame, ref Filter filter, Weapon* weapon)
		{
			if (filter.Weapons->IsReloading == false)
				return;

			filter.Weapons->ReloadCooldown -= frame.DeltaTime;

			if (filter.Weapons->IsReloading == false)
			{
				weapon->Reload();

				// Add small prepare time after reload.
				filter.Weapons->FireCooldown = FP._0_25;
			}
		}

		private void TryFire(Frame frame, ref Filter filter, Weapon* weapon, bool justPressed)
		{
			if (filter.Weapons->IsBusy)
				return;
			if (weapon->IsCollected == false)
				return;
			if (justPressed == false && weapon->IsAutomatic == false)
				return;

			filter.Weapons->FireCooldown = (FP)60 / weapon->FireRate;

			if (weapon->ClipAmmo <= 0)
			{
				frame.Events.WeaponFired(filter.Weapons->CurrentWeaponId, filter.Entity, justPressed, true);
				return;
			}

			frame.Events.WeaponFired(filter.Weapons->CurrentWeaponId, filter.Entity, justPressed, false);

			var firePosition = filter.KCC->Data.TargetPosition + filter.Player->CameraOffset * FPVector3.Up;
			var fireRotation = FPQuaternion.LookRotation(filter.KCC->Data.LookDirection);

			DamageData damageData = default;

			for (int i = 0; i < weapon->ProjectilesPerShot; i++)
			{
				var projectileRotation = fireRotation;

				if (weapon->Dispersion > 0)
				{
					// We use unit sphere on purpose -> non-uniform distribution (more projectiles in the center).
					var dispersionRotation = FPQuaternion.Euler(frame.RNG->InUnitCircle().XYO * weapon->Dispersion);
					projectileRotation = fireRotation * dispersionRotation;
				}

				FireProjectile(frame, ref filter, firePosition, projectileRotation * FPVector3.Forward, weapon->MaxHitDistance, weapon->Damage, ref damageData);
			}

			if (damageData.TotalDamage > 0)
			{
				frame.Events.DamageInflicted(filter.Player->PlayerRef, damageData.IsFatal, damageData.IsCritical);
			}

			weapon->ClipAmmo--;
		}

		private void FireProjectile(Frame frame, ref Filter filter, FPVector3 fromPosition, FPVector3 direction, FP maxDistance, FP damage, ref DamageData damageData)
		{
			// Use default layer mask + add lag compensation proxy layer mask based on PlayerRef.
			var hitMask = filter.Weapons->HitMask;
			hitMask.BitMask |= LagCompensationUtility.GetProxyCollisionLayerMask(filter.Player->PlayerRef);

			var options = QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo;
			var nullableHit = frame.Physics3D.Raycast(fromPosition, direction, maxDistance, hitMask, options);

			if (nullableHit.HasValue == false)
			{
				// No surface was hit, show projectile visual flying to dummy distant point
				var distantPoint = fromPosition + direction * maxDistance;
				frame.Events.FireProjectile(filter.Weapons->CurrentWeaponId, filter.Entity, distantPoint, FPVector3.Zero);
			}

			Hit3D hit = nullableHit.Value;

			if (frame.Unsafe.TryGetPointer(hit.Entity, out LagCompensationProxy* lagCompensationProxy))
			{
				// Lag compensation proxy was hit, switching hit entity to its origin entity.
				hit.SetHitEntity(lagCompensationProxy->Target);
			}

			// When hitting dynamic colliders (players), hit normal is set to zero and hit impact won't be shown
			var hitNormal = hit.IsDynamic ? FPVector3.Zero : hit.Normal;
			frame.Events.FireProjectile(filter.Weapons->CurrentWeaponId, filter.Entity, hit.Point, hitNormal);

			if (frame.Unsafe.TryGetPointer(hit.Entity, out Health* health) == false)
				return;

			// 16.8.2024
			// At this moment, using hit.GetShape(frame)->UserTag returns incorrect value.
			// For now we use obsolete hit.ShapeUserTag.
			#pragma warning disable 0618

			// Hitting different shapes on player body can result in different damage multipliers
			if (hit.ShapeUserTag == _headShapeUserTag)
			{
				damage *= FP._2;
				damageData.IsCritical = true;
			}
			else if (hit.ShapeUserTag == _limbShapeUserTag)
			{
				damage *= FP._0_50;
			}

			#pragma warning restore 0618

			// At the end of gameplay the damage is doubled
			if (frame.GetSingleton<Gameplay>().IsDoubleDamageActive)
			{
				damage *= 2;
			}

			FP damageDone = health->ApplyDamage(damage);
			if (damageDone > 0)
			{
				damageData.TotalDamage += damageDone;

				if (health->IsAlive == false && frame.Unsafe.TryGetPointer(hit.Entity, out Player* victim))
				{
					frame.Signals.PlayerKilled(filter.Player->PlayerRef, victim->PlayerRef, filter.Weapons->CurrentWeaponId, false);
					damageData.IsFatal = true;
				}

				frame.Events.DamageReceived(hit.Entity, hit.Point, hit.Normal);
			}
		}

		void ISignalOnComponentAdded<Weapons>.OnAdded(Frame frame, EntityRef entity, Weapons* component)
		{
			// Prepare player weapons.
			for (int i = 0; i < component->WeaponPrototypes.Length; i++)
			{
				var prototype = component->WeaponPrototypes[i];
				if (prototype.IsValid == false)
					continue;

				component->WeaponRefs[i] = frame.Create(prototype);
			}

			// First weapon is automatically collected.
			var currentWeapon = frame.Unsafe.GetPointer<Weapon>(component->CurrentWeapon);
			currentWeapon->IsCollected = true;
		}

		void ISignalOnComponentRemoved<Weapons>.OnRemoved(Frame frame, EntityRef entity, Weapons* component)
		{
			// Destroy player weapons.
			for (int i = 0; i < component->WeaponRefs.Length; i++)
			{
				frame.Destroy(component->WeaponRefs[i]);
			}
		}

		void ISignalSwitchWeapon.SwitchWeapon(Frame frame, EntityRef playerEntity, byte weaponId)
		{
			var filter = new Filter
			{
				Entity = playerEntity,
				Weapons = frame.Unsafe.GetPointer<Weapons>(playerEntity),
			};

			TryStartWeaponSwitch(frame, ref filter, weaponId);
		}

		public struct Filter
		{
			public EntityRef Entity;
			public Player*   Player;
			public Weapons*  Weapons;
			public Health*   Health;
			public KCC*      KCC;
		}

		private struct DamageData
		{
			public FP TotalDamage;
			public bool IsCritical;
			public bool IsFatal;
		}
	}
}
