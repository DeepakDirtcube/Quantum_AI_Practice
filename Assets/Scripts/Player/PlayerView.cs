using UnityEngine;
using Photon.Deterministic;
using Cinemachine;
using Quantum;

namespace SimpleFPS
{
	/// <summary>
	/// Main player script which handles visuals and camera.
	/// </summary>
	public class PlayerView : QuantumEntityViewComponent<SceneContext>
	{
		[Header("Components")]
		public Animator      Animator;
		public WeaponsView   Weapons;

		[Header("Setup")]
		public AudioSource   JumpSound;
		public AudioClip[]   JumpClips;
		public Transform     CameraHandle;
		public GameObject    FirstPersonRoot;
		public GameObject    ThirdPersonRoot;

		private float _smoothPitch;
		private float _smoothPitchVelocity;

		public override void OnActivate(Frame frame)
		{
			if (frame.TryGet(EntityRef, out Player player) == false)
				return;

			bool isLocal = Game.PlayerIsLocal(player.PlayerRef);
			if (isLocal)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;

				ViewContext.LocalPlayerView = this;
				ViewContext.LocalPlayer = player.PlayerRef;
				ViewContext.LocalPlayerEntity = EntityRef;

				// Local player is always predicted.
				EntityView.InterpolationMode = QuantumEntityViewInterpolationMode.Prediction;
			}
			else
			{
				// Virtual cameras are enabled only for local player.
				var virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
				for (int i = 0; i < virtualCameras.Length; i++)
				{
					virtualCameras[i].enabled = false;
				}

				// Other player views are snapshot interpolated.
				EntityView.InterpolationMode = QuantumEntityViewInterpolationMode.SnapshotInterpolation;
			}

			SetFirstPersonVisuals(isLocal);
		}

		public override void OnDeactivate()
		{
			if (ViewContext.LocalPlayerView == this)
			{
				ViewContext.LocalPlayerView = null;
				ViewContext.LocalPlayerEntity = EntityRef.None;
			}
		}

		public override void OnLateUpdateView()
		{
			Frame verifiedFrame = VerifiedFrame;
			if (verifiedFrame.TryGet(EntityRef, out Player player) == false)
				return;

			Frame predictedFrame = PredictedFrame;
			if (predictedFrame.Exists(EntityRef) == false)
				return;

			KCC      kcc      = predictedFrame.Get<KCC>(EntityRef);
			Health   health   = predictedFrame.Get<Health>(EntityRef);
			Weapons  weapons  = predictedFrame.Get<Weapons>(EntityRef);
			Gameplay gameplay = predictedFrame.GetSingleton<Gameplay>();

			Weapon currentWeapon = default;
			if (predictedFrame.Exists(weapons.CurrentWeapon))
			{
				currentWeapon = predictedFrame.Get<Weapon>(weapons.CurrentWeapon);
			}

			float lookYaw   = kcc.Data.LookYaw.AsFloat;
			float lookPitch = kcc.Data.LookPitch.AsFloat;

			bool isLocal = Game.PlayerIsLocal(player.PlayerRef);
			if (isLocal)
			{
				if (health.IsAlive && gameplay.State != EGameplayState.Finished)
				{
					// Local player view is interpolated between previous predicted frame and predicted frame to get smooth look rotation.
					if (PredictedPreviousFrame.TryGet<KCC>(EntityRef, out KCC previousKCC))
					{
						lookYaw   = Mathf.LerpAngle(previousKCC.Data.LookYaw.AsFloat, kcc.Data.LookYaw.AsFloat, EntityView.Game.InterpolationFactor);
						lookPitch = Mathf.LerpAngle(previousKCC.Data.LookPitch.AsFloat, kcc.Data.LookPitch.AsFloat, EntityView.Game.InterpolationFactor);
					}
				}

				transform.rotation = Quaternion.Euler(0.0f, lookYaw, 0.0f);
			}
			else
			{
				// Remote players must have interpolated pitch otherwise the visual is glitching.
				// Yaw is handled out of the box by transform snapshot interpolation.
				if (verifiedFrame.TryGet<KCC>(EntityRef, out KCC verifiedKCC))
				{
					_smoothPitch = Mathf.SmoothDamp(_smoothPitch, verifiedKCC.Data.LookPitch.AsFloat, ref _smoothPitchVelocity, 0.1f);
					lookPitch = _smoothPitch;
				}
			}

			lookPitch = Mathf.Clamp(lookPitch, -89.0f, 89.0f);

			CameraHandle.localRotation = Quaternion.Euler(lookPitch, 0.0f, 0.0f);

			var moveVelocity = GetAnimationMoveVelocity(kcc);

			// Set animation parameters.
			Animator.SetFloat("LocomotionTime", Time.time * 2f);
			Animator.SetBool("IsAlive", health.IsAlive);
			Animator.SetBool("IsGrounded", kcc.IsGrounded);
			Animator.SetBool("IsReloading", currentWeapon.IsReloading);
			Animator.SetFloat("MoveX", moveVelocity.x, 0.05f, Time.deltaTime);
			Animator.SetFloat("MoveZ", moveVelocity.z, 0.05f, Time.deltaTime);
			Animator.SetFloat("MoveSpeed", moveVelocity.magnitude);
			Animator.SetFloat("Look", -lookPitch / 90f);

			if (health.IsAlive == false)
			{
				// Disable UpperBody (override) and Look (additive) layers. Death animation is full-body.

				int upperBodyLayerIndex = Animator.GetLayerIndex("UpperBody");
				Animator.SetLayerWeight(upperBodyLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(upperBodyLayerIndex) - Time.deltaTime));

				int lookLayerIndex = Animator.GetLayerIndex("Look");
				Animator.SetLayerWeight(lookLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(lookLayerIndex) - Time.deltaTime));

				// Force enable third person visual for local player.
				SetFirstPersonVisuals(false);
			}

			if (kcc.HasJumped)
			{
				Animator.SetTrigger("Jump");

				JumpSound.clip = JumpClips[Random.Range(0, JumpClips.Length)];
				JumpSound.Play();
			}
		}

		private void SetFirstPersonVisuals(bool firstPerson)
		{
			FirstPersonRoot.SetActive(firstPerson);
			ThirdPersonRoot.SetActive(firstPerson == false);

			Weapons.SetFirstPersonVisuals(firstPerson);
		}

		private Vector3 GetAnimationMoveVelocity(KCC kcc)
		{
			if (kcc.RealSpeed < FP._0_01)
				return default;

			var velocity = kcc.RealVelocity;

			// We only care about X an Z directions.
			velocity.Y = 0;

			if (velocity.SqrMagnitude > 1)
			{
				velocity = velocity.Normalized;
			}

			// Transform velocity vector to local space.
			return transform.InverseTransformVector(velocity.ToUnityVector3());
		}
	}
}
