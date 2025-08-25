using Photon.Deterministic;

namespace Quantum
{
	public partial struct Health
	{
		public bool IsAlive => CurrentHealth > 0;
		public bool IsImmortal => ImmortalityCooldown > 0;

		public FP ApplyDamage(FP damage)
		{
			if (CurrentHealth <= 0)
				return 0;

			if (IsImmortal)
				return 0;

			if (damage > CurrentHealth)
			{
				damage = CurrentHealth;
			}

			CurrentHealth -= damage;

			return damage;
		}

		public bool AddHealth(FP health)
		{
			if (CurrentHealth <= 0)
				return false;
			if (CurrentHealth >= MaxHealth)
				return false;

			CurrentHealth = FPMath.Min(CurrentHealth + health, MaxHealth);

			return true;
		}

		public void StopImmortality()
		{
			ImmortalityCooldown = 0;
		}
	}
}
