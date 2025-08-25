using System;
using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class WeaponsView : QuantumEntityViewComponent<SceneContext>
	{
		[Header("Setup")]
		public Setup FirstPersonSetup;
		public Setup ThirdPersonSetup;

		[Header("Sounds")]
		public AudioSource SwitchSound;

		public WeaponView  VisibleWeapon => _allWeapons[_visibleWeaponId];

		private int _visibleWeaponId = -1;
		private WeaponView[] _allWeapons;

		private bool _firstPersonActive;
		private Setup _activeSetup;

		public void SetFirstPersonVisuals(bool firstPerson)
		{
			if (firstPerson == _firstPersonActive)
				return;

			_firstPersonActive = firstPerson;
			_activeSetup = firstPerson ? FirstPersonSetup : ThirdPersonSetup;

			for (int i = 0; i < _allWeapons.Length; i++)
			{
				// First person weapons are rendered with a different (overlay) camera
				// to prevent clipping through geometry.
				_allWeapons[i].gameObject.SetLayer(_activeSetup.WeaponLayer, true);
			}
		}

		public override void OnInitialize()
		{
			// All weapons are already present inside Player prefab.
			// This is the simplest solution when only few weapons are available in the game.
			_allWeapons = GetComponentsInChildren<WeaponView>();

			for (int i = 0; i < _allWeapons.Length; i++)
			{
				_allWeapons[i].WeaponId = (byte)i;
			}

			_activeSetup = ThirdPersonSetup;
		}

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventWeaponSwitchStarted>(this, OnWeaponSwitchStarted);
			QuantumEvent.Subscribe<EventWeaponReloadStarted>(this, OnWeaponReloadStarted);
			QuantumEvent.Subscribe<EventWeaponFired>(this, OnWeaponFired);
		}

		public override void OnDeactivate()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		public override void OnUpdateView()
		{
			if (TryGetPredictedQuantumComponent(out Weapons weapons) == false)
				return;

			UpdateVisibleWeapon(weapons.CurrentWeaponId);

			if (_firstPersonActive)
			{
				FirstPersonSetup.Animator.SetBool("IsReloading", weapons.IsReloading);
			}
		}

		public override void OnLateUpdateView()
		{
			if (VisibleWeapon != null)
			{
				var weaponTransform = VisibleWeapon.transform;
				var weaponPivot = _firstPersonActive ? VisibleWeapon.FirstPersonPivot : VisibleWeapon.ThirdPersonPivot;

				// Snap visible weapon to weapon handle transform, use weapon pivot to adjust offset and rotation per weapon
				weaponTransform.rotation = _activeSetup.WeaponHandle.rotation * weaponPivot.localRotation;
				weaponTransform.position = _activeSetup.WeaponHandle.position + weaponTransform.rotation * weaponPivot.localPosition;
			}
		}

		private void UpdateVisibleWeapon(int weaponId)
		{
			if (_visibleWeaponId == weaponId)
				return;

			_visibleWeaponId = weaponId;

			// Update weapon visibility
			for (int i = 0; i < _allWeapons.Length; i++)
			{
				var weapon = _allWeapons[i];
				weapon.ToggleVisibility(weapon.WeaponId == weaponId);
			}

			FirstPersonSetup.LeftHandSnap.Handle = VisibleWeapon.LeftHandHandle;

			FirstPersonSetup.Animator.runtimeAnimatorController = VisibleWeapon.HandsAnimatorController;
			ThirdPersonSetup.Animator.SetFloat("WeaponID", weaponId);

			// Hide and show animations are played only for local player
			if (_firstPersonActive)
			{
				FirstPersonSetup.Animator.SetTrigger("Show");
			}
		}

		private void OnWeaponFired(EventWeaponFired callback)
		{
			if (callback.PlayerEntity != EntityRef)
				return;

			VisibleWeapon.FireTriggered(callback.JustPressed, callback.IsEmpty);

			if (callback.IsEmpty)
				return;

			// Player fire animation (hands) is not played when strafing in third person because we lack a proper
			// animation, and we do not want to make the animation controller more complex
			if (_firstPersonActive == false && Mathf.Abs(ThirdPersonSetup.Animator.GetFloat("MoveX")) > 0.2f)
				return;

			_activeSetup.Animator.SetTrigger("Fire");
		}

		private void OnWeaponSwitchStarted(EventWeaponSwitchStarted callback)
		{
			if (callback.PlayerEntity != EntityRef)
				return;

			SwitchSound.Play();

			// Hide and show animations are played only for local player
			if (_firstPersonActive)
			{
				FirstPersonSetup.Animator.SetTrigger("Hide");
			}
		}

		private void OnWeaponReloadStarted(EventWeaponReloadStarted callback)
		{
			if (callback.PlayerEntity != EntityRef)
				return;

			VisibleWeapon.ReloadStarted();
		}

		// DATA STRUCTURES

		[Serializable]
		public class Setup
		{
			public Transform WeaponHandle;
			[Layer]
			public int       WeaponLayer;
			public Animator  Animator;
			public HandSnap  LeftHandSnap;
		}
	}
}
