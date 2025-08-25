using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Health system controls player health, damage and immortality.
	/// </summary>
	[Preserve]
	public unsafe class HealthSystem : SystemMainThreadFilter<HealthSystem.Filter>, ISignalOnComponentAdded<Health>
	{
		public override void Update(Frame frame, ref Filter filter)
		{
			filter.Health->ImmortalityCooldown -= frame.DeltaTime;

			if (filter.Health->ImmortalityCooldown <= 0)
			{
				filter.Health->ImmortalityCooldown = 0;
			}
		}

		void ISignalOnComponentAdded<Health>.OnAdded(Frame frame, EntityRef entity, Health* health)
		{
			health->CurrentHealth = health->MaxHealth;
			health->ImmortalityCooldown = health->SpawnImmortalityTime;
		}

		public struct Filter
		{
			public EntityRef Entity;
			public Health*   Health;
		}
	}
}
