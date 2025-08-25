using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class UICrosshair : MonoBehaviour
	{
		[Header("Hit UI")]
		public GameObject  RegularHit;
		public GameObject  CriticalHit;
		public GameObject  FatalHit;

		[Header("Hit Sounds")]
		public AudioSource RegularHitSound;
		public AudioSource CriticalHitSound;
		public AudioSource FatalHitSound;

		private void OnEnable()
		{
			RegularHit.SetActive(false);
			CriticalHit.SetActive(false);
			FatalHit.SetActive(false);

			QuantumEvent.Subscribe<EventDamageInflicted>(this, OnDamageInflicted);
		}

		private void OnDisable()
		{
			QuantumEvent.UnsubscribeListener<EventDamageInflicted>(this);
		}

		private void OnDamageInflicted(EventDamageInflicted callback)
		{
			var hitObject = callback.IsFatal ? FatalHit : (callback.IsCritical ? CriticalHit : RegularHit);

			// Restart hit animation
			hitObject.SetActive(false);
			hitObject.SetActive(true);

			var hitSound = callback.IsFatal ? FatalHitSound : (callback.IsCritical ? CriticalHitSound : RegularHitSound);
			if (hitSound != null)
			{
				hitSound.PlayOneShot(hitSound.clip);
			}
		}
	}
}
