using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class WeaponView : QuantumEntityViewComponent<SceneContext>
	{
		public byte             WeaponId { get; set; }

		[Header("Visuals")]
		public Sprite           Icon;
		public string           Name;
		public Animator         WeaponAnimator;
		public RuntimeAnimatorController HandsAnimatorController;

		[Header("Holding")]
		public Transform        FirstPersonPivot;
		public Transform        ThirdPersonPivot;
		public Transform        LeftHandHandle;

		[Header("Fire Effect")]
		public Transform        MuzzleTransform;
		public GameObject       MuzzleEffectPrefab;
		public ProjectileVisual ProjectileVisualPrefab;

		[Header("Sounds")]
		public AudioSource      FireSound;
		public AudioSource      ReloadingSound;
		public AudioSource      EmptyClipSound;

		private GameObject _muzzleEffectInstance;
		private bool _canPlayEmptyClip;

		public void ToggleVisibility(bool isVisible)
		{
			gameObject.SetActive(isVisible);

			if (_muzzleEffectInstance != null)
			{
				_muzzleEffectInstance.SetActive(false);
			}
		}

		public void FireTriggered(bool justPressed, bool isEmpty)
		{
			// Reset empty clip play.
			_canPlayEmptyClip |= justPressed;

			if (isEmpty)
			{
				if (_canPlayEmptyClip)
				{
					EmptyClipSound.Play();
					_canPlayEmptyClip = false;
				}
				return;
			}

			if (FireSound != null)
			{
				FireSound.PlayOneShot(FireSound.clip);
			}

			// Reset muzzle effect visibility.
			_muzzleEffectInstance.SetActive(false);
			_muzzleEffectInstance.SetActive(true);

			if (WeaponAnimator != null)
			{
				WeaponAnimator.SetTrigger("Fire");
			}

			_canPlayEmptyClip = true;
		}

		public void ReloadStarted()
		{
			ReloadingSound.Play();
			WeaponAnimator.SetTrigger("Reload");
		}

		public override void OnInitialize()
		{
			_muzzleEffectInstance = Instantiate(MuzzleEffectPrefab, MuzzleTransform);
			_muzzleEffectInstance.SetActive(false);
		}

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventFireProjectile>(this, OnFireProjectile);
		}

		public override void OnDeactivate()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnFireProjectile(EventFireProjectile callback)
		{
			if (callback.WeaponId != WeaponId)
				return;
			if (callback.PlayerEntity != EntityRef)
				return;

			var projectileVisual = Instantiate(ProjectileVisualPrefab, MuzzleTransform.position, MuzzleTransform.rotation);
			projectileVisual.SetHit(callback.TargetPosition.ToUnityVector3(), callback.HitNormal.ToUnityVector3(), callback.HitNormal != FPVector3.Zero);
		}
	}
}
