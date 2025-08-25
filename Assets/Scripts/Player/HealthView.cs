using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class HealthView : QuantumEntityViewComponent<SceneContext>
	{
		public GameObject HitEffectPrefab;
		public GameObject ImmortalityIndicator;

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventDamageReceived>(this, OnDamageReceived);
		}

		public override void OnDeactivate()
		{
			QuantumEvent.UnsubscribeListener<EventDamageReceived>(this);
		}

		public override void OnUpdateView()
		{
			if (TryGetPredictedQuantumComponent(out Health health) == false)
				return;

			ImmortalityIndicator.SetActive(health.IsImmortal);
		}

		private void OnDamageReceived(EventDamageReceived callback)
		{
			if (callback.Entity != EntityRef)
				return;
			if (HitEffectPrefab == null)
				return;

			var hitRotation = Quaternion.LookRotation(callback.HitNormal.ToUnityVector3());
			Instantiate(HitEffectPrefab, callback.HitPoint.ToUnityVector3(), hitRotation);
		}
	}
}
