namespace Quantum
{
	public unsafe partial struct Weapons
	{
		public bool      IsBusy        => IsFiring || IsReloading || IsSwitching;
		public bool      IsFiring      => FireCooldown > 0;
		public bool      IsReloading   => ReloadCooldown > 0;
		public bool      IsSwitching   => SwitchCooldown > 0;
		public EntityRef CurrentWeapon => WeaponRefs[CurrentWeaponId];
	}
}
