using UnityEngine;

namespace Quantum
{
	public class PickupView : QuantumEntityViewComponent
	{
		public GameObject ActiveObject;
		public GameObject InactiveObject;

		public override void OnLateUpdateView()
		{
			var pickup = VerifiedFrame.Get<Pickup>(EntityRef);

			ActiveObject.SetActive(pickup.Cooldown <= 0);
			InactiveObject.SetActive(pickup.Cooldown > 0);
		}
	}
}
