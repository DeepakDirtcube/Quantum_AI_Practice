using System;

namespace Quantum
{
	public partial struct Weapon
	{
		public bool HasAmmo => ClipAmmo > 0 || RemainingAmmo > 0;

		public bool CanReload()
		{
			if (IsCollected == false)
			 	return false;

			if (ClipAmmo >= MaxClipAmmo)
				return false;

			return RemainingAmmo > 0;
		}

		public void Reload()
		{
			int reloadAmmo = MaxClipAmmo - ClipAmmo;
			reloadAmmo = Math.Min(reloadAmmo, RemainingAmmo);

			ClipAmmo += reloadAmmo;
			RemainingAmmo -= reloadAmmo;
		}

		public bool CollectOrRefill(int refillAmmo)
		{
			if (IsCollected && RemainingAmmo >= MaxRemainingAmmo)
				return false;

			if (IsCollected)
			{
				// If the weapon is already collected at least refill the ammo.
				RemainingAmmo = Math.Min(RemainingAmmo + refillAmmo, MaxRemainingAmmo);
			}
			else
			{
				// Weapon is already present inside Player prefab,
				// marking it as IsCollected is all that is needed.
				IsCollected = true;
			}

			return true;
		}
	}
}
